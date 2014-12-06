using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using FLib;
namespace Magic2D
{
    // セグメントの輪郭パスを細かく分ける。
    // これをすることでセグメントの結合時に形が崩れるのを防ぐ。
    public static class PathSubdivision
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="course">0以上．値が大きいほど荒くなる</param>
        /// <returns></returns>
        public static List<PointF> Subdivide(List<PointF> path, float course)
        {
            if (course <= 0)
                return null;

            float x = float.MaxValue, y = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;

            foreach (var p in path)
            {
                x = Math.Min(p.X, x);
                y = Math.Min(p.Y, y);
                x1 = Math.Max(p.X, x1);
                y1 = Math.Max(p.Y, y1);
            }
            int w = (int)(x1 - x), h = (int)(y1 - y);
            x = x - w;
            y = y - h;
            w *= 3;
            h *= 3;

            Rectangle bounds = new Rectangle((int)x, (int)y, w, h);

            Pen pen = new Pen(Brushes.Red, 2);

            List<PointF> divPath = new List<PointF>();

            using (Bitmap line = new Bitmap((int)x + w, (int)y + h))
            {
                using (var g = Graphics.FromImage(line))
                {
                    g.Clear(Color.Transparent);
                    g.DrawCurve(pen, path.ToArray());
                }

                using (BitmapIterator iter = new BitmapIterator(line, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        divPath.Add(path[i]);
                        List<PointF> seg = SubdivideSegment(path[i], path[i + 1], course);
                        if (seg == null)
                            continue;
                        float len = FMath.Distance(path[i], path[i + 1]);
                        if (len <= 1e-4)
                            continue;
                        float dy = (path[i].X - path[i + 1].X) / len;
                        float dx = (path[i + 1].Y - path[i].Y) / len;
                        PointF dir = new PointF(dx, dy);
                        foreach (var p in seg)
                        {
                            PointF pt = GetNearestPixel(iter, p, dir, bounds, null);
                            if (pt.X >= 0 && pt.Y >= 0)
                                divPath.Add(pt);
                        }
                    }
                }
            }

            divPath.Add(path.Last());

            return divPath;
        }

        static List<PointF> SubdivideSegment(PointF p1, PointF p2, float course)
        {
            if (course <= 0)
                return null;
            float dist = FLib.FMath.Distance(p1, p2);
            int n = (int)(dist / course) - 1;

            List<PointF> ls = new List<PointF>();
            for (int i = 0; i < n; i++)
                ls.Add(FLib.FMath.Interpolate(p1, p2, (i + 1f) / (n + 1)));

            return ls;
        }

        // ptとdirが表す直線状を、ptから双方向に操作したときに最初に見つかるピクセル
        // iterは32ビット長を仮定
        unsafe static PointF GetNearestPixel(BitmapIterator iter, PointF pt, PointF dir, Rectangle bounds, Predicate<int> isFound = null)
        {
            if (iter.PixelSize != 4)
                return new PointF(-1, -1);

            float len = FMath.Distance(dir, PointF.Empty);
            if (len <= 1e-4)
                return new PointF(-1, -1);
            float dx = dir.X / len;
            float dy = dir.Y / len;

            int cnt = 0;
            while (true)
            {
                float x1 = pt.X + dx * cnt;
                float y1 = pt.Y + dy * cnt;
                float x2 = pt.X - dx * cnt;
                float y2 = pt.Y - dy * cnt;

                bool out1 = !bounds.Contains((int)x1, (int)y1);
                bool out2 = !bounds.Contains((int)x2, (int)y2);

                if (out1 && out2)
                    return new PointF(-1, -1);

                if (!out1 && CheckNearestPixel(x1, y1, iter, isFound))
                    return new PointF(x1, y1);

                if (!out2 && CheckNearestPixel(x2, y2, iter, isFound))
                    return new PointF(x2, y2);

                cnt++;
            }
        }

        unsafe static bool CheckNearestPixel(float x, float y, BitmapIterator iter, Predicate<int> isFound)
        {
            if (x < 0 || y < 0)
                return false;
            if (x >= iter.Bmp.Width || y >= iter.Bmp.Height)
                return false;
            int val = ((int*)iter.PixelData)[(int)x + (int)y * iter.Bmp.Width];
            if (isFound == null)
            {
                if (val != 0)
                    return true;
            }
            else
            {
                if (isFound(val))
                    return true;
            }
            return false;
        }
    }
}