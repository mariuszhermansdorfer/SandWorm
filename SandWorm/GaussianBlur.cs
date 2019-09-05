using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SandWorm
{
    public class GaussianBlur
    {
        public ushort[] source;
        private int _width;
        private int _height;

        private readonly ParallelOptions _pOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 };

        public GaussianBlur(ushort [] pixels)
        {
    
            source = pixels;
        }

        public ushort[] Process(int radial, int width, int height)
        {

            _width = width;
            _height = height;


            var newPixels = new ushort[_width * _height];
            
            GaussBlur_4(source, newPixels, radial);

            return newPixels;
        }

        private void GaussBlur_4(ushort[] source, ushort[] dest, int r)
        {
            var bxs = BoxesForGauss(r, 3);
            BoxBlur_4(source, dest, _width, _height, (bxs[0] - 1) / 2);
            BoxBlur_4(dest, source, _width, _height, (bxs[1] - 1) / 2);
            BoxBlur_4(source, dest, _width, _height, (bxs[2] - 1) / 2);
        }

        private ushort[] BoxesForGauss(int sigma, int n)
        {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);
            var wl = (ushort)Math.Floor(wIdeal);
            if (wl % 2 == 0) wl--;
            var wu = (ushort)(wl + 2);

            var mIdeal = (double)(12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            var m = Math.Round(mIdeal);

            var sizes = new List<ushort>();
            for (var i = 0; i < n; i++) sizes.Add(i < m ? wl : wu);
            return sizes.ToArray();
        }

        private void BoxBlur_4(ushort[] source, ushort[] dest, int w, int h, int r)
        {
            for (var i = 0; i < source.Length; i++) dest[i] = source[i];
            BoxBlurH_4(dest, source, w, h, r);
            BoxBlurT_4(source, dest, w, h, r);
        }

        private void BoxBlurH_4(ushort[] source, ushort[] dest, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, h, _pOptions, i =>
            {
                var ti = i * w;
                var li = ti;
                var ri = ti + r;
                var fv = source[ti];
                var lv = source[ti + w - 1];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j];
                for (var j = 0; j <= r; j++)
                {
                    val += source[ri++] - fv;
                    dest[ti++] = (ushort)(val * iar);
                }
                for (var j = r + 1; j < w - r; j++)
                {
                    val += source[ri++] - dest[li++];
                    dest[ti++] = (ushort)(val * iar);
                }
                for (var j = w - r; j < w; j++)
                {
                    val += lv - source[li++];
                    dest[ti++] = (ushort)(val * iar);
                }
            });
        }

        private void BoxBlurT_4(ushort[] source, ushort[] dest, int w, int h, int r)
        {
            var iar = (double)1 / (r + r + 1);
            Parallel.For(0, w, _pOptions, i =>
            {
                var ti = i;
                var li = ti;
                var ri = ti + r * w;
                var fv = source[ti];
                var lv = source[ti + w * (h - 1)];
                var val = (r + 1) * fv;
                for (var j = 0; j < r; j++) val += source[ti + j * w];
                for (var j = 0; j <= r; j++)
                {
                    val += source[ri] - fv;
                    dest[ti] = (ushort)(val * iar);
                    ri += w;
                    ti += w;
                }
                for (var j = r + 1; j < h - r; j++)
                {
                    val += source[ri] - source[li];
                    dest[ti] = (ushort)(val * iar);
                    li += w;
                    ri += w;
                    ti += w;
                }
                for (var j = h - r; j < h; j++)
                {
                    val += lv - source[li];
                    dest[ti] = (ushort)(val * iar);
                    li += w;
                    ti += w;
                }
            });
        }
    }
}
