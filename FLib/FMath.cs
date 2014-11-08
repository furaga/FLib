using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace FLib
{
    public class FMath
    {
        public static float MahalanobisDistance(Color[] pixels1, Color[] pixels2)
        {
            float sqdist = 0;

            foreach (Func<Color, float> getter in new Func<Color, float>[] {
                col => col.R,
                col => col.G,
                col => col.B
            })
            {
                float avg1, vrc1;
                float avg2, vrc2;
                GetAverageVariance(pixels1, getter, out avg1, out vrc1);
                GetAverageVariance(pixels2, getter, out avg2, out vrc2);
                sqdist += (avg1 - avg2) * (avg1 - avg2);
            }

            return (float)Math.Sqrt(sqdist);
        }
        public static void Swap<T>(ref T x, ref T y)
        {
            T t = x;
            x = y;
            y = t;
        }
        public static float Clamp(float x, float min, float max)
        {
            if (min > max) Swap(ref min, ref max);
            return Math.Min(max, Math.Max(min, x));
        }

        public static void GetAverageVariance(Color[] pixels, Func<Color, float> getter, out float avg, out float vrc)
        {
            float sum = 0;
            float sqsum = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                float val = getter(pixels[i]);
                sum += val;
                sqsum += val * val;
            }
            avg = sum / pixels.Length;
            vrc = sqsum / pixels.Length - avg * avg;
        }

        public static bool IsCrossed(PointF p1, PointF p2, PointF p3, PointF p4)
        {
            float ksi = (p4.Y - p3.Y) * (p4.X - p1.X) - (p4.X - p3.X) * (p4.Y - p1.Y);
            float eta = -(p2.Y - p1.Y) * (p4.X - p1.X) + (p2.X - p1.X) * (p4.Y - p1.Y);
            float delta = (p4.Y - p3.Y) * (p2.X - p1.X) - (p4.X - p3.X) * (p2.Y - p1.Y);
            if (Math.Abs(delta) <= 1e-4)
                return false;
            float lambda = ksi / delta;
            float mu = eta / delta;
            return -1e-4 <= lambda && lambda <= 1 + 1e-4 && -1e-4 <= mu && mu <= 1 + 1e-4;
        }

        public static bool IsCrossed(PointF p1, PointF p2, List<PointF> path)
        {
            if (path == null)
                return false;

            for (int i = 0; i < path.Count - 1; i++)
            {
                if (IsCrossed(p1, p2, path[i], path[i + 1]))
                    return true;
            }
            return false;
        }

        public static float SqDistance(PointF start, PointF end)
        {
            float dx = start.X - end.X;
            float dy = start.Y - end.Y;
            return dx * dx + dy * dy;
        }

        public static float GetDistanceToLine(PointF pt, List<PointF> line)
        {
            float min = float.MaxValue;
            for (int i = 0; i < line.Count - 1; i++)
                min = Math.Min(min, GetDistanceToLine(pt, line[i], line[i + 1]));
            return min;
        }

        public static float GetDistanceToLine(PointF pt, PointF lineStart, PointF lineEnd, bool ignoreIfOut = true)
        {
            float v0x = pt.X - lineStart.X;
            float v0y = pt.Y - lineStart.Y;
            float v1x = lineEnd.X - lineStart.X;
            float v1y = lineEnd.Y - lineStart.Y;

            float len0 = (float)Math.Sqrt(v0x * v0x + v0y * v0y);
            float len1 = (float)Math.Sqrt(v1x * v1x + v1y * v1y);

            if (len0 <= 1e-4f || len1 <= 1e-4f)
                return float.MaxValue;

            v0x /= len0;
            v0y /= len0;
            v1x /= len1;
            v1y /= len1;

            float cos = v0x * v1x + v0y * v1y;
            // 点が線分からはみ出していたら無効
            if (cos < 0 || len1 < len0 * cos)
            {
                if (ignoreIfOut)
                    return float.MaxValue;
            }

            float sin = (float)Math.Sqrt(1 - cos * cos);
            float dist = len0 * sin;

            return dist;
        }

        public static bool IsPointInPolygon(PointF pointTarget, List<PointF> path)
        {
            if (path == null || path.Count <= 2)
                return false;

            int iCountCrossing = 0;

            PointF point0 = path[0];
            bool bFlag0x = (pointTarget.X <= point0.X);
            bool bFlag0y = (pointTarget.Y <= point0.Y);

            for (int i = 1; i < path.Count + 1; i++)
            {
                PointF point1 = path[i % path.Count];
                bool bFlag1x = (pointTarget.X <= point1.X);
                bool bFlag1y = (pointTarget.Y <= point1.Y);
                if (bFlag0y != bFlag1y)
                {
                    if (bFlag0x == bFlag1x)
                    {
                        if (bFlag0x)
                            iCountCrossing += (bFlag0y ? -1 : 1);
                    }
                    else
                    {
                        if (pointTarget.X <= (point0.X + (point1.X - point0.X) * (pointTarget.Y - point0.Y) / (point1.Y - point0.Y)))
                            iCountCrossing += (bFlag0y ? -1 : 1);
                    }
                }
                point0 = point1;
                bFlag0x = bFlag1x;
                bFlag0y = bFlag1y;
            }

            return (0 != iCountCrossing);
        }

        public static float Distance(PointF p0, PointF p1)
        {
            return (float)Math.Sqrt(SqDistance(p0, p1));
        }

        public static float ToDegree(float rad)
        {
            return (float)(rad / Math.PI * 180);
        }
        public static float ToRadian(float deg)
        {
            return (float)(deg / 180 * Math.PI);
        }

        public static PointF Interpolate(PointF p0, PointF p1, float t)
        {
            return new PointF(p0.X * (1 - t) + p1.X * t, p0.Y * (1 - t) + p1.Y * t);
        }

        public static PointF Transform(PointF pt, Matrix transform)
        {
            var _pt = new[] { pt };
            transform.TransformPoints(_pt);
            return _pt[0];
        }

        // pathはrefPath上の点集合。ひとつづきの点集合ごとに分割する
        // たとえば
        // path: p1, p2, p3, p4
        // refPath: p1, p3, p0, p2, p4
        // のとき{ { { p1, p3 }, { p2, p4 } }を返す
        public static List<List<PointF>> SplitPath(List<PointF> path, List<PointF> refPath, bool looping)
        {
            var ranges = SplitPathRange(path, refPath, looping);
            List<List<PointF>> ans = new List<List<PointF>>();
            foreach (var range in ranges)
            {
                var ls = new List<PointF>();
                for (int i = range.First; i < range.First + range.Length; i++)
                    ls.Add(refPath[i % refPath.Count]);
                if (ls.Count >= 1)
                    ans.Add(ls);
            }

            return ans;
        }

        public static List<CharacterRange> SplitPathRange(List<PointF> path, List<PointF> refPath, bool looping)
        {
            List<CharacterRange> splited = new List<CharacterRange>();

            if (path == null || refPath == null)
                return splited;

            int start = -1;
            for (int i = 0; i < refPath.Count; i++)
            {
                var pt = refPath[i];
                if (path.Contains(pt))
                {
                    if (start < 0)
                        start = i;
                }
                else
                {
                    if (start >= 0)
                        splited.Add(new CharacterRange(start, i - start));
                    start = -1;
                }
            }

            if (start >= 0)
                splited.Add(new CharacterRange(start, refPath.Count - start));

            if (looping)
            {
                // pathの始点と終点をまたいでいる場合は統合
                if (splited.Count >= 2 && splited[0].First == 0 && splited.Last().First + splited.Last().Length == refPath.Count)
                {
                    var newRange = new CharacterRange(splited.Last().First, splited.Last().Length + splited[0].Length);
                    splited.RemoveAt(splited.Count - 1);
                    splited.RemoveAt(0);
                    splited.Add(newRange);
                }
            }

            return splited;
        }

        // 曲率計算
        public static float GetCurvature(List<PointF> curve)
        {
            if (curve == null || curve.Count <= 2)
                return 0;

            float[] dxs = new float[curve.Count - 1];
            float[] dys = new float[curve.Count - 1];
            float[] ddxs = new float[curve.Count - 2];
            float[] ddys = new float[curve.Count - 2];
            float[] lens = new float[curve.Count - 1];

            for (int i = 0; i < lens.Length; i++)
            {
                float dx = curve[i + 1].X - curve[i].X;
                float dy = curve[i + 1].Y - curve[i].Y;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (Math.Abs(len) < 1e-4)
                    len = 1e-4f;
                lens[i] = len;
            }

            for (int i = 0; i < dxs.Length; i++)
            {
                dxs[i] = (curve[i + 1].X - curve[i].X) / lens[i];
                dys[i] = (curve[i + 1].Y - curve[i].Y) / lens[i];
            }

            for (int i = 0; i < ddxs.Length; i++)
            {
                ddxs[i] = (dxs[i + 1] - dxs[i]) / lens[i];
                ddys[i] = (dys[i + 1] - dys[i]) / lens[i];
            }

            float k = 0;
            for (int j = 0; j < ddxs.Length; j++)
                k += (float)Math.Sqrt(ddxs[j] * ddxs[j] + ddys[j] * ddys[j]);

            k /= ddxs.Length;

            return k;
        }

        public static float GetAngleCos(List<PointF> curve)
        {
            if (curve == null || curve.Count < 3)
                return 0;
            float l0 = Distance(curve[1], curve[0]);
            float l1 = Distance(curve[1], curve[2]);
            if (l0 <= 1e-4 || l1 < 1e-4)
                return 0;
            float dx0 = (curve[1].X - curve[0].X) / l0;
            float dy0 = (curve[1].Y - curve[0].Y) / l0;
            float dx1 = (curve[2].X - curve[1].X) / l1;
            float dy1 = (curve[2].Y - curve[1].Y) / l1;
            return dx0 * dx1 + dy0 * dy1;
        }

        public static float GetSide(PointF p, PointF src, PointF dst)
        {
            float dx1 = p.X - src.X;
            float dy1 = p.Y - src.Y;
            float dx2 = dst.X - src.X;
            float dy2 = dst.Y - src.Y;
            return dx1 * dy2 - dx2 * dy1;
        }

        public static PointF HelmitteInterporate(PointF p1, PointF v1, PointF p2, PointF v2, float t)
        {
            PointF[] p = new[] { p1, v1, p2, v2 };

            float k = t;
            float[] s = new[] { k * k * k, k * k, k };
            float[] mx = new[] { p[0].X, p[1].X, p[2].X, p[3].X };
            float[] my = new[] { p[0].Y, p[1].Y, p[2].Y, p[3].Y };

            float x = ((2 * mx[0]) + (mx[1]) - (2 * mx[2]) + (mx[3])) * s[0] +
              ((-3 * mx[0]) - (2 * mx[1]) + (3 * mx[2]) - (mx[3])) * s[1] +
              (mx[1]) * s[2] + (mx[0]);
            float y = ((2 * my[0]) + (my[1]) - (2 * my[2]) + (my[3])) * s[0] +
              ((-3 * my[0]) - (2 * my[1]) + (3 * my[2]) - (my[3])) * s[1] +
              (my[1]) * s[2] + (my[0]);

            return new PointF(x, y);
        }

        public static List<int> FloodFill<T>(int n, Dictionary<int, List<int>> edges, Func<int, int, int, T, bool> cond, T tag)
        {
            HashSet<int> uncheck = new HashSet<int>();
            List<int> labels = new List<int>();
            int c = 0;

            for (int i = 0; i < n; i++)
            {
                uncheck.Add(i);
                labels.Add(-1);
            }

            while (uncheck.Count >= 1)
            {
                HashSet<int> check = new HashSet<int>() { uncheck.First() };
                while (check.Count >= 1)
                {
                    int idx = check.First();

                    labels[idx] = c;
                    check.Remove(idx);
                    uncheck.Remove(idx);

                    if (edges == null)
                        continue;

                    if (!edges.ContainsKey(idx))
                        continue;

                    List<int> nexts = edges[idx];
                    for (int i = 0; i < nexts.Count; i++)
                    {
                        if (!uncheck.Contains(nexts[i]))
                            continue;
                        if (cond == null || cond(idx, nexts[i], n, tag))
                            check.Add(nexts[i]);
                    }
                }
                c++;
            }

            return labels;
        }

        public static float GetMinElement<T>(IEnumerable<T> list, Func<T, float> f, out int minIdx)
        {
            minIdx = -1;
            if (list == null)
                return float.MaxValue;
            
            float min = float.MaxValue;
            int idx = -1;
            foreach (var x in list)
            {
                idx++;
                float val = f(x);
                if (min > val)
                {
                    min = val;
                    minIdx = idx;
                }
            }

            return min;
        }

        public static int Rem(int idx, int n)
        {
            if (n <= 0)
                return idx;
            while (idx < 0)
                idx += n;
            return idx % n;
        }

        public static List<PointF> ShiftPath(List<PointF> path, float offsetx, float offsety)
        {
            if (path == null)
                return new List<PointF>();
            List<PointF> _path = new List<PointF>();
            for (int i = 0; i < path.Count; i++)
                _path.Add(new PointF(path[i].X + offsetx, path[i].Y + offsety));
            return _path;
        }
    }
}