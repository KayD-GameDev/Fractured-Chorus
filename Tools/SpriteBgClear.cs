using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: SpriteBgClear <folder-or-file> [...]");
            return 1;
        }

        foreach (var arg in args)
        {
            if (Directory.Exists(arg))
            {
                foreach (var file in Directory.GetFiles(arg, "*.png"))
                {
                    Console.WriteLine(Path.GetFileName(file) + " cleared=" + ProcessFile(file));
                }
            }
            else if (File.Exists(arg))
            {
                Console.WriteLine(Path.GetFileName(arg) + " cleared=" + ProcessFile(arg));
            }
        }

        return 0;
    }

    private static int ProcessFile(string path)
    {
        Bitmap bmp;
        using (var src = new Bitmap(path))
        {
            bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.DrawImage(src, 0, 0, src.Width, src.Height);
            }
        }

        var w = bmp.Width;
        var h = bmp.Height;
        var rect = new Rectangle(0, 0, w, h);
        var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        var stride = data.Stride;
        var bytes = Math.Abs(stride) * h;
        var px = new byte[bytes];
        Marshal.Copy(data.Scan0, px, 0, bytes);

        var visited = new bool[w * h];
        var q = new Queue<int>();

        for (var x = 0; x < w; x++)
        {
            TryEnqueue(px, stride, visited, q, w, h, x, 0);
            TryEnqueue(px, stride, visited, q, w, h, x, h - 1);
        }

        for (var y = 0; y < h; y++)
        {
            TryEnqueue(px, stride, visited, q, w, h, 0, y);
            TryEnqueue(px, stride, visited, q, w, h, w - 1, y);
        }

        var cleared = 0;
        while (q.Count > 0)
        {
            var idx = q.Dequeue();
            var x = idx % w;
            var y = idx / w;
            var pi = y * stride + x * 4;
            px[pi] = 0;
            px[pi + 1] = 0;
            px[pi + 2] = 0;
            px[pi + 3] = 0;
            cleared++;
            TryEnqueue(px, stride, visited, q, w, h, x + 1, y);
            TryEnqueue(px, stride, visited, q, w, h, x - 1, y);
            TryEnqueue(px, stride, visited, q, w, h, x, y + 1);
            TryEnqueue(px, stride, visited, q, w, h, x, y - 1);
        }

        Marshal.Copy(px, 0, data.Scan0, bytes);
        bmp.UnlockBits(data);

        var temp = path + ".tmp.png";
        if (File.Exists(temp))
        {
            File.Delete(temp);
        }

        bmp.Save(temp, ImageFormat.Png);
        bmp.Dispose();
        File.Copy(temp, path, true);
        File.Delete(temp);
        return cleared;
    }

    private static void TryEnqueue(byte[] px, int stride, bool[] visited, Queue<int> q, int w, int h, int x, int y)
    {
        if (x < 0 || y < 0 || x >= w || y >= h)
        {
            return;
        }

        var idx = y * w + x;
        if (visited[idx])
        {
            return;
        }

        if (!IsBackground(px, stride, x, y))
        {
            return;
        }

        visited[idx] = true;
        q.Enqueue(idx);
    }

    private static bool IsBackground(byte[] px, int stride, int x, int y)
    {
        var i = y * stride + x * 4;
        var b = px[i];
        var g = px[i + 1];
        var r = px[i + 2];
        var a = px[i + 3];
        if (a < 8)
        {
            return true;
        }

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var avg = (r + g + b) / 3;
        if (r >= 245 && g >= 245 && b >= 245)
        {
            return true;
        }

        if ((max - min) <= 12 && avg >= 175)
        {
            return true;
        }

        if (r >= 235 && g >= 235 && b >= 235 && (max - min) <= 18)
        {
            return true;
        }

        return false;
    }
}
