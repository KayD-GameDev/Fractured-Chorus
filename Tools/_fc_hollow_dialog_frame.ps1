Add-Type -AssemblyName System.Drawing
$code = @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static class FcDialogHollow
{
    public static string Run(string path, string previewPath)
    {
        Bitmap bmp;
        using (var fs = File.OpenRead(path))
        using (var src = new Bitmap(fs))
        {
            bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
                g.DrawImage(src, 0, 0);
        }

        int w = bmp.Width, h = bmp.Height;
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        int stride = data.Stride;
        byte[] px = new byte[stride * h];
        Marshal.Copy(data.Scan0, px, 0, px.Length);

        bool[] clear = new bool[w * h];
        var st = new Stack<int>();
        Action<int> push = (id) =>
        {
            if (clear[id]) return;
            if (!IsInteriorFill(px, stride, w, id)) return;
            clear[id] = true;
            st.Push(id);
        };

        for (int y = (int)(h * 0.26); y <= (int)(h * 0.80); y += 10)
        {
            for (int x = (int)(w * 0.20); x <= (int)(w * 0.80); x += 16)
            {
                push(y * w + x);
            }
        }

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

        int cleared = 0;
        for (int id = 0; id < w * h; id++)
        {
            if (!clear[id]) continue;
            int i = (id / w) * stride + (id % w) * 4;
            px[i] = 0;
            px[i + 1] = 0;
            px[i + 2] = 0;
            px[i + 3] = 0;
            cleared++;
        }

        for (int y = 1; y < h - 1; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                int id = y * w + x;
                if (clear[id]) continue;
                int i = y * stride + x * 4;
                if (px[i + 3] == 0) continue;
                bool adj = false;
                if (clear[id - 1] || clear[id + 1] || clear[id - w] || clear[id + w]) adj = true;
                if (!adj) continue;
                if (!IsNearInteriorFill(px, stride, w, id)) continue;
                px[i + 3] = (byte)(px[i + 3] * 50 / 100);
            }
        }

        for (int id = 0; id < w * h; id++)
        {
            int i = (id / w) * stride + (id % w) * 4;
            if (px[i + 3] == 0)
            {
                px[i] = 0;
                px[i + 1] = 0;
                px[i + 2] = 0;
            }
        }

        Marshal.Copy(px, 0, data.Scan0, px.Length);
        bmp.UnlockBits(data);
        bmp.Save(path, ImageFormat.Png);

        using (var crop = new Bitmap(920, 340))
        using (var g = Graphics.FromImage(crop))
        {
            g.Clear(Color.FromArgb(255, 48, 52, 64));
            g.DrawImage(bmp, new Rectangle(0, 0, 920, 340), new Rectangle(240, 70, 920, 340), GraphicsUnit.Pixel);
            crop.Save(previewPath, ImageFormat.Png);
        }

        bmp.Dispose();
        return string.Format("cleared={0} px ({1:F1}% of {2}x{3})", cleared, cleared * 100.0 / (w * h), w, h);
    }

    static bool IsInteriorFill(byte[] px, int stride, int w, int id)
    {
        int i = (id / w) * stride + (id % w) * 4;
        if (px[i + 3] < 16) return false;
        int b = px[i];
        int g = px[i + 1];
        int r = px[i + 2];
        int max = r > g ? r : g;
        if (b > max) max = b;
        int min = r < g ? r : g;
        if (b < min) min = b;
        int lum = (r * 30 + g * 59 + b * 11) / 100;
        int sat = max - min;
        if (lum < 222) return false;
        if (sat > 42) return false;
        if (b + 8 < r && r - b > 25) return false;
        return true;
    }

    static bool IsNearInteriorFill(byte[] px, int stride, int w, int id)
    {
        int i = (id / w) * stride + (id % w) * 4;
        if (px[i + 3] < 16) return false;
        int b = px[i];
        int g = px[i + 1];
        int r = px[i + 2];
        int lum = (r * 30 + g * 59 + b * 11) / 100;
        int sat = (r > g ? r : g) - (r < g ? r : g);
        if (b > (r > g ? r : g)) sat = (r > g ? r : g) - (r < g ? r : g);
        int max = r > g ? r : g;
        if (b > max) max = b;
        int min = r < g ? r : g;
        if (b < min) min = b;
        sat = max - min;
        return lum >= 200 && sat <= 55;
    }
}
"@
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $code
$path = "D:\Fractured-Chorus1\Assets\FracturedChorus\Art\UI\Narrative\DialogueBox_Frame_LightBlueHolo_v1.png"
$preview = "D:\Fractured-Chorus1\Tools\_dialog_frame_hollow_preview.png"
Write-Output ([FcDialogHollow]::Run($path, $preview))
