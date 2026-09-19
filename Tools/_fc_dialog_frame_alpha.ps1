Add-Type -AssemblyName System.Drawing
$code = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static class FcDialogPipeline
{
    public static void MatteJpgToPng(string jpg, string png, bool hollowInterior)
    {
        Bitmap src;
        using (var fs = File.OpenRead(jpg))
        using (var img = new Bitmap(fs))
        {
            src = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(src))
                g.DrawImage(img, 0, 0, img.Width, img.Height);
        }

        int w = src.Width, h = src.Height;
        var data = src.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = data.Stride;
        byte[] px = new byte[stride * h];
        Marshal.Copy(data.Scan0, px, 0, px.Length);
        FloodChecker(px, stride, w, h);
        SeedLoose(px, stride, w, h, 55, 88);
        SeedLoose(px, stride, w, h, 960, 165);
        for (int p = 0; p < 8; p++) ExpandTransparent(px, stride, w, h);
        ZeroClearPx(px, stride, w, h);
        Marshal.Copy(px, 0, data.Scan0, px.Length);
        src.UnlockBits(data);

        Rectangle bbox = BoundsPx(px, stride, w, h, 10);
        int pad = 8;
        bbox = Inflate(bbox, pad, w, h);
        using (var cropped = src.Clone(bbox, PixelFormat.Format32bppArgb))
        {
            src.Dispose();
            int outW = 1920;
            int outH = (int)Math.Round(cropped.Height * (1920.0 / cropped.Width));
            using (var scaled = new Bitmap(outW, outH, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(scaled))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(cropped, 0, 0, outW, outH);
                ZeroClearBmp(scaled);
                if (hollowInterior)
                    HollowInterior(scaled);
                scaled.Save(png, ImageFormat.Png);
            }
        }
    }

    public static void HollowExisting(string png)
    {
        using (var src = new Bitmap(png))
        {
            var work = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(work))
                g.DrawImage(src, 0, 0);
            HollowInterior(work);
            work.Save(png, ImageFormat.Png);
            work.Dispose();
        }
    }

    static void HollowInterior(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        var data = bmp.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = data.Stride;
        byte[] px = new byte[stride * h];
        Marshal.Copy(data.Scan0, px, 0, px.Length);

        bool[] clear = new bool[w * h];
        var st = new Stack<int>();
        Action<int> push = (id) =>
        {
            if (clear[id] || IsProtectedZone(w, h, id)) return;
            if (!IsInteriorFill(px, stride, w, id)) return;
            clear[id] = true;
            st.Push(id);
        };

        int y0 = (int)(h * 0.32);
        int y1 = (int)(h * 0.84);
        int x0 = (int)(w * 0.14);
        int x1 = (int)(w * 0.82);
        for (int y = y0; y <= y1; y += 8)
        for (int x = x0; x <= x1; x += 12)
            push(y * w + x);

        while (st.Count > 0)
        {
            int id = st.Pop();
            int x = id % w;
            int y = id / w;
            if (x > 0) push(id - 1);
            if (x < w - 1) push(id + 1);
            if (y > 0) push(id - w);
            if (y < h - 1) push(id + w);
        }

        int rx0 = (int)(w * 0.17);
        int rx1 = (int)(w * 0.80);
        int ry0 = (int)(h * 0.36);
        int ry1 = (int)(h * 0.80);
        for (int y = ry0; y <= ry1; y++)
        {
            for (int x = rx0; x <= rx1; x++)
            {
                int id = y * w + x;
                if (IsProtectedZone(w, h, id)) continue;
                if (!IsInteriorFill(px, stride, w, id)) continue;
                clear[id] = true;
            }
        }

        for (int id = 0; id < w * h; id++)
        {
            if (!clear[id] || IsProtectedZone(w, h, id)) continue;
            int i = (id / w) * stride + (id % w) * 4;
            px[i] = px[i + 1] = px[i + 2] = px[i + 3] = 0;
        }
        ZeroClearPx(px, stride, w, h);
        Marshal.Copy(px, 0, data.Scan0, px.Length);
        bmp.UnlockBits(data);
    }

    static bool IsProtectedZone(int w, int h, int id)
    {
        int x = id % w;
        int y = id / w;
        if (y < h * 0.30 && x < w * 0.34) return true;
        return false;
    }

    static bool IsInteriorFill(byte[] px, int stride, int w, int id)
    {
        int i = (id / w) * stride + (id % w) * 4;
        if (px[i + 3] < 20) return false;
        int b = px[i], g = px[i + 1], r = px[i + 2];
        int max = Math.Max(r, Math.Max(g, b));
        int min = Math.Min(r, Math.Min(g, b));
        int lum = (r * 30 + g * 59 + b * 11) / 100;
        int sat = max - min;
        if (lum < 226) return false;
        if (sat > 40) return false;
        return true;
    }

    static bool StrictBg(int r, int g, int b)
    {
        int max = Math.Max(r, Math.Max(g, b));
        int min = Math.Min(r, Math.Min(g, b));
        int y = (r * 30 + g * 59 + b * 11) / 100;
        return (max - min) <= 18 && y >= 120 && y <= 222 && (b - r) <= 16;
    }

    static bool LooseBg(int r, int g, int b)
    {
        int max = Math.Max(r, Math.Max(g, b));
        int min = Math.Min(r, Math.Min(g, b));
        int y = (r * 30 + g * 59 + b * 11) / 100;
        if (y < 95 || y > 225) return false;
        if (r < 70 && g < 85 && b < 120) return false;
        return (max - min) <= 56 && (b - r) < 55;
    }

    static void FloodChecker(byte[] px, int stride, int w, int h)
    {
        bool[] vis = new bool[w * h];
        var st = new Stack<int>();
        Action<int,int> push = (x,y) => {
            if (x<0||y<0||x>=w||y>=h) return;
            int id=y*w+x; if(vis[id]) return; vis[id]=true; st.Push(id);
        };
        for (int x=0;x<w;x++){ push(x,0); push(x,h-1); }
        for (int y=0;y<h;y++){ push(0,y); push(w-1,y); }
        while (st.Count>0)
        {
            int id=st.Pop();
            int x=id%w,y=id/w,i=y*stride+x*4;
            if(!StrictBg(px[i+2],px[i+1],px[i])){ vis[id]=false; continue;}
            px[i]=px[i+1]=px[i+2]=px[i+3]=0;
            push(x-1,y); push(x+1,y); push(x,y-1); push(x,y+1);
        }
    }

    static void SeedLoose(byte[] px, int stride, int w, int h, int sx, int sy)
    {
        if (sx<0||sy<0||sx>=w||sy>=h) return;
        bool[] vis = new bool[w*h];
        var st = new Stack<int>();
        st.Push(sy*w+sx); vis[sy*w+sx]=true;
        while(st.Count>0)
        {
            int id=st.Pop();
            int x=id%w,y=id/w,i=y*stride+x*4;
            if(px[i+3]==0) continue;
            if(!LooseBg(px[i+2],px[i+1],px[i])) continue;
            px[i]=px[i+1]=px[i+2]=px[i+3]=0;
            for(int dy=-1;dy<=1;dy++)
            for(int dx=-1;dx<=1;dx++)
            {
                if(dx==0&&dy==0) continue;
                int nx=x+dx,ny=y+dy;
                if(nx<0||ny<0||nx>=w||ny>=h) continue;
                int nid=ny*w+nx;
                if(vis[nid]) continue;
                vis[nid]=true; st.Push(nid);
            }
        }
    }

    static void ExpandTransparent(byte[] px, int stride, int w, int h)
    {
        var kill = new List<int>();
        for (int y=1;y<h-1;y++)
        for (int x=1;x<w-1;x++)
        {
            int i=y*stride+x*4;
            if(px[i+3]==0) continue;
            if(!LooseBg(px[i+2],px[i+1],px[i])) continue;
            int t=0;
            for(int dy=-1;dy<=1;dy++)
            for(int dx=-1;dx<=1;dx++)
            {
                if(dx==0&&dy==0) continue;
                if(px[(y+dy)*stride+(x+dx)*4+3]==0) t++;
            }
            if(t>=3) kill.Add(i);
        }
        foreach(int i in kill){ px[i]=px[i+1]=px[i+2]=px[i+3]=0; }
    }

    static Rectangle BoundsPx(byte[] px, int stride, int w, int h, int amin)
    {
        int minX=w,minY=h,maxX=0,maxY=0;
        for(int y=0;y<h;y++)
        for(int x=0;x<w;x++)
        {
            if(px[y*stride+x*4+3]<amin) continue;
            if(x<minX)minX=x; if(y<minY)minY=y; if(x>maxX)maxX=x; if(y>maxY)maxY=y;
        }
        if(maxX<minX) return new Rectangle(0,0,w,h);
        return new Rectangle(minX,minY,maxX-minX+1,maxY-minY+1);
    }

    static Rectangle Inflate(Rectangle r, int pad, int w, int h)
    {
        int x=Math.Max(0,r.X-pad), y=Math.Max(0,r.Y-pad);
        int rw=Math.Min(w-x,r.Width+pad*2), rh=Math.Min(h-y,r.Height+pad*2);
        return new Rectangle(x,y,rw,rh);
    }

    static void ZeroClearPx(byte[] px, int stride, int w, int h)
    {
        for(int y=0;y<h;y++)
        for(int x=0;x<w;x++)
        {
            int i=y*stride+x*4;
            if(px[i+3]==0) px[i]=px[i+1]=px[i+2]=0;
        }
    }

    static void ZeroClearBmp(Bitmap bmp)
    {
        int w=bmp.Width,h=bmp.Height;
        var data=bmp.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride=data.Stride;
        byte[] px=new byte[stride*h];
        Marshal.Copy(data.Scan0,px,0,px.Length);
        ZeroClearPx(px,stride,w,h);
        Marshal.Copy(px,0,data.Scan0,px.Length);
        bmp.UnlockBits(data);
    }
}
"@
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $code
$jpg = "C:\Users\Asus\.cursor\projects\d-Fractured-Chorus1\assets\c__Users_Asus_AppData_Roaming_Cursor_User_workspaceStorage_8868388ef8a4e1b8bd84d6af4db53888_images_Dialog-78d231ca-8723-472b-95fd-04dcc3439a89.jpg"
$png = "D:\Fractured-Chorus1\Assets\FracturedChorus\Art\UI\Narrative\DialogueBox_Frame_LightBlueHolo_v1.png"
$preview = "D:\Fractured-Chorus1\Tools\_dialog_frame_hollow_preview.png"
$solid = $args -contains "-solid"
if ($args -contains "-hollowOnly") {
  [FcDialogPipeline]::HollowExisting($png)
} else {
  [FcDialogPipeline]::MatteJpgToPng($jpg, $png, -not $solid)
}
Add-Type -AssemblyName System.Drawing
$b = New-Object System.Drawing.Bitmap $png
$c = New-Object System.Drawing.Bitmap 920,340
$g = [System.Drawing.Graphics]::FromImage($c)
$g.Clear([System.Drawing.Color]::FromArgb(255,48,52,64))
$g.DrawImage($b, (New-Object System.Drawing.Rectangle 0,0,920,340), (New-Object System.Drawing.Rectangle 240,70,920,340), [System.Drawing.GraphicsUnit]::Pixel)
$c.Save($preview, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $c.Dispose(); $b.Dispose()
Write-Output done
