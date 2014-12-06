using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using FLib;

namespace Magic2D
{
    public enum SegmentOperation
    {
        Segment,
        SkeletonAnnotation,
        PartingLine,
        Section, // 断面
    }

    public class Segmentation : IDisposable
    {
        public Dictionary<string, SegmentRoot> segmentRootDict = new Dictionary<string, SegmentRoot>();
        public Matrix transform = new Matrix();
        float scale = 1;
        string editingSegmentRootKey = "";
        Segment editingSegment;

        public Segmentation()
        {
            editingSegment = new PathSegment("", null);
        }

        public void Clear()
        {
            foreach (var root in segmentRootDict.Values)
                root.Dispose();
            segmentRootDict.Clear();
            transform = new Matrix();
            scale = 1;
            editingSegmentRootKey = "";
            editingSegment = null;
        }

        public void Dispose()
        {
            foreach (var root in segmentRootDict.Values)
                root.Dispose();
        }

        PointF prevPoint = Point.Empty;

        public void AssignSegmentRootAs(string name, Bitmap bmp, SkeletonAnnotation an)
        {
            var root = new SegmentRoot(bmp, an);
            if (segmentRootDict.ContainsKey(name))
                segmentRootDict[name].Dispose();
            segmentRootDict[name] = root;
        }

        public void SetEditingSegmentRoot(string key)
        {
            editingSegmentRootKey = key;
            if (editingSegment != null)
                editingSegment.root = GetEditingSegmentRoot();
        }
        public SegmentRoot GetEditingSegmentRoot()
        {
            if (segmentRootDict.ContainsKey(editingSegmentRootKey))
                return segmentRootDict[editingSegmentRootKey];
            return null;
        }

        public void ResetEditingSegment()
        {
            editingSegment = new PathSegment("", null);
        }

        // セグメント追加
        public void AssignEditingSegmentAs(string name)
        {
            if (editingSegment == null)
                return;
            var root = GetEditingSegmentRoot();
            if (root == null)
                return;
            editingSegment.name = name;
            root.segments.Add(editingSegment);
            editingSegment = new PathSegment(name, null);
        }

        public void SetEditingSegment(string name)
        {
            var root = GetEditingSegmentRoot();
            if (root != null)
                editingSegment = root.GetSegment(name);
        }

        public Segment GetEditingSegment()
        {
            return editingSegment;
        }

        // 座標変換
        public void Pan(float dx, float dy)
        {
            transform.Translate(dx, dy, MatrixOrder.Append);
        }
        public void Zoom(float delta)
        {
            float prev = scale;
            if (prev <= 1e-4)
                return;

            scale += delta;
            scale = FMath.Clamp(scale, 0.1f, 15f);
            float ratio = scale / prev;
            if (Math.Abs(1 - ratio) <= 1e-4)
                return;

            transform.Scale(ratio, ratio, MatrixOrder.Append);
        }
        public void Zoom(float zoom, PointF pan)
        {
            Pan(-pan.X, -pan.Y);
            Zoom(zoom);
            Pan(pan.X, pan.Y);
        }

        public void CreateEditingSegment()
        {
            if (editingSegment == null || editingSegment.root != null)
                editingSegment = new PathSegment("", null);
        }
    }

    public class SegmentRoot : IDisposable
    {
        public Bitmap bmp;
        public List<Segment> segments = new List<Segment>();
        public SkeletonAnnotation an;

        public SegmentRoot(Bitmap bmp, SkeletonAnnotation an)
        {
            if (bmp != null)
                this.bmp = new Bitmap(bmp);
            this.an = an;
            segments.Add(new Segment("Full", this) { bmp = this.bmp, });
        }

        public void Dispose()
        {
            if (bmp != null)
                bmp.Dispose();
            foreach (var seg in segments)
                seg.Dispose();
        }

        public Segment GetSegment(string name)
        {
            if (segments.Any(s => s.name == name))
                return segments.First(s => s.name == name);
            return null;
        }

    }

    public class Segment : IDisposable
    {
        public virtual void Dispose()
        {
            if (bmp != null)
                bmp.Dispose();
        }

        public bool Closed { get; protected set; }
        public string name = "";
        public Bitmap bmp;
        public Point offset;
        public SegmentRoot root;
        public List<PointF> path = new List<PointF>();

        // アノテーション
        public SkeletonAnnotation an = new SkeletonAnnotation(null);
        public List<PointF> section = new List<PointF>();
        public List<PointF> partingLine = new List<PointF>();

        public PointF nearestPoint { get; private set; }
        public BoneAnnotation nearestBone { get; private set; }

        public Segment(string key, SegmentRoot root)
        {
            this.name = key;
            this.root = root;
        }

        public Segment(Segment seg, string name = "")
        {
            if (seg == null)
                return;

            this.name = name == "" ? seg.name : name;
            this.bmp = seg.bmp == null ? null : new Bitmap(seg.bmp);
            this.offset = seg.offset;
            this.root = null;
            this.an = new SkeletonAnnotation(seg.an, false);
            this.section = new List<PointF>(seg.section);
            this.partingLine = new List<PointF>(seg.partingLine);
            this.path = new List<PointF>(seg.path);
        }

        public void _SetClosed(bool closed)
        {
            this.Closed = closed;
        }

        PointF Transform(PointF pt, Matrix transform)
        {
            if (transform == null || !transform.IsInvertible)
                return pt;
            var _pt = new PointF[] { pt };
            transform.TransformPoints(_pt);
            return _pt[0];
        }

        PointF GetNearestPoint(PointF pt, List<PointF> path, int threshold, Matrix transform)
        {
            if (path == null || path.Count <= 0)
                return PointF.Empty;

            PointF nearest = PointF.Empty;
            float minSqDist = threshold * threshold;
            var pathPoints = path.ToArray();
            transform.TransformPoints(pathPoints);
            for (int i = 0; i < pathPoints.Length; i++)
            {
                var pathPoint = pathPoints[i];
                float dx = pathPoint.X - pt.X;
                float dy = pathPoint.Y - pt.Y;
                float sqDist = dx * dx + dy * dy;
                if (minSqDist > sqDist)
                {
                    nearest = path[i];
                    minSqDist = sqDist;
                }
            }
            return nearest;
        }

        public RectangleF GetBound(List<PointF> points)
        {
            float x = float.MaxValue, y = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var pt in path)
            {
                x = Math.Min(x, pt.X);
                y = Math.Min(y, pt.Y);
                x1 = Math.Max(x1, pt.X);
                y1 = Math.Max(y1, pt.Y);
            }
            float w = x1 - x;
            float h = y1 - y;
            if (w <= 1 || h <= 1)
                return RectangleF.Empty;
            return new RectangleF(x, y, w, h);
        }

        public Rectangle GetExpandedBound(List<PointF> path)
        {
            // pathを包括できるちょっと大きめの矩形領域を求める
            var bound = GetBound(path);
            int cx = (int)(bound.X + bound.Width * 0.5f);
            int cy = (int)(bound.Y + bound.Height * 0.5f);
            int cw = (int)(bound.Width * 1.4f);
            int ch = (int)(bound.Height * 1.4f);
            return new Rectangle(cx - cw / 2, cy - ch / 2, cw, ch);
        }

        public Bitmap DrawPath(Rectangle bound)
        {
            PixelFormat format = PixelFormat.Format32bppArgb;
            if (root != null && root.bmp != null)
                format = root.bmp.PixelFormat;
            var boarder = new Bitmap(bound.Width, bound.Height, format);
            using (var g = Graphics.FromImage(boarder))
            {
                var transform = new Matrix();
                transform.Translate(-bound.X, -bound.Y);
                g.Transform = transform;
                g.Clear(Color.Black);
                DrawPath(g, new Pen(Brushes.White, 2), new Pen(Brushes.White, 2));
            }
            return boarder;
        }

        unsafe public void UpdateBitmap(SegmentRoot root)
        {
            if (root != null)
                this.root = root;

            if (!Closed)
                return;

            if (root == null || root.bmp == null)
                return;

            if (bmp != null)
                bmp.Dispose();

            var expBound = GetExpandedBound(path);
            var pathBmp = DrawPath(expBound);

            Rectangle pixelBound = new Rectangle();
            bool[] pixels = GetPixelsInPath(pathBmp, out pixelBound);

            offset = new Point(expBound.X + pixelBound.X, expBound.Y + pixelBound.Y);

            bmp = new Bitmap((int)pixelBound.Width, (int)pixelBound.Height, root.bmp.PixelFormat);

            using (var initer = new BitmapIterator(root.bmp, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            using (var outiter = new BitmapIterator(bmp, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            {
                for (int y = 0; y < pixelBound.Height; y++)
                    for (int x = 0; x < pixelBound.Width; x++)
                    {
                        if (pixels[pixelBound.Width * y + x])
                        {
                            int xx = offset.X + x;
                            int yy = offset.Y + y;
                            if (xx < 0 || root.bmp.Width <= xx || yy < 0 || root.bmp.Height <= yy)
                                continue;
                            if (x < 0 || bmp.Width <= x || y < 0 || bmp.Height <= y)
                                continue;
                            int ii = initer.Stride * yy + 4 * xx;
                            int oi = outiter.Stride * y + 4 * x;
                            outiter.Data[oi + 0] = initer.Data[ii + 0];
                            outiter.Data[oi + 1] = initer.Data[ii + 1];
                            outiter.Data[oi + 2] = initer.Data[ii + 2];
                            outiter.Data[oi + 3] = initer.Data[ii + 3];
                        }
                    }
            }
        }

        unsafe public bool[] GetPixelsInPath(Bitmap pathBmp, out Rectangle pixelBound)
        {
            pixelBound = new Rectangle();

            bool[] isBoarder = new bool[pathBmp.Width * pathBmp.Height];

            using (var bbmp = new BitmapIterator(pathBmp, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            {
                for (int y = 0; y < pathBmp.Height; y++)
                    for (int x = 0; x < pathBmp.Width; x++)
                    {
                        bool onBoarder = bbmp.Data[bbmp.Stride * y + 4 * x + 1] >= 128;
                        if (onBoarder)
                            isBoarder[x + y * pathBmp.Width] = true;
                    }
            }

            // FloodFill
            HashSet<int> pixels = new HashSet<int>();
            HashSet<int> checks = new HashSet<int>(pixels);
            checks.Add(0);
            while (checks.Count >= 1)
            {
                int idx = checks.First();
                pixels.Add(idx);
                checks.Remove(idx);

                int x;
                int y = Math.DivRem(idx, pathBmp.Width, out x);
                if (x - 1 >= 0)
                {
                    int i = (x - 1) + y * pathBmp.Width;
                    if (!isBoarder[i] && !pixels.Contains(i))
                        checks.Add(i);
                }
                if (x + 1 < pathBmp.Width)
                {
                    int i = (x + 1) + y * pathBmp.Width;
                    if (!isBoarder[i] && !pixels.Contains(i))
                        checks.Add(i);
                }
                if (y - 1 >= 0)
                {
                    int i = x + (y - 1) * pathBmp.Width;
                    if (!isBoarder[i] && !pixels.Contains(i))
                        checks.Add(i);
                }
                if (y + 1 < pathBmp.Height)
                {
                    int i = x + (y + 1) * pathBmp.Width;
                    if (!isBoarder[i] && !pixels.Contains(i))
                        checks.Add(i);
                }
            }

            int xx = int.MaxValue;
            int yy = int.MaxValue;
            int x1 = int.MinValue;
            int y1 = int.MinValue;

            List<int> xs = new List<int>();
            List<int> ys = new List<int>();
            for (int y = 0; y < pathBmp.Height; y++)
                for (int x = 0; x < pathBmp.Width; x++)
                {
                    int i = x + y * pathBmp.Width;
                    if (pixels.Contains(i))
                        continue;
                    xx = Math.Min(xx, x);
                    yy = Math.Min(yy, y);
                    x1 = Math.Max(x1, x);
                    y1 = Math.Max(y1, y);
                    xs.Add(x);
                    ys.Add(y);
                }
            pixelBound = new Rectangle(xx, yy, x1 - xx + 1, y1 - yy + 1);
            bool[] trimmedPixels = new bool[pixelBound.Width * pixelBound.Height];
            for (int i = 0; i < xs.Count; i++)
                trimmedPixels[(xs[i] - pixelBound.X) + (ys[i] - pixelBound.Y) * pixelBound.Width] = true;

            return trimmedPixels;
        }

        protected PointF InvertPoint(PointF point, Matrix transform)
        {
            var inv = transform.Clone();
            if (!inv.IsInvertible)
                return point;
            inv.Invert();
            var pt = new PointF[] { point };
            inv.TransformPoints(pt);
            return pt[0];
        }

        internal void DrawPath(Graphics g, Pen pathPen, Pen closedPathPen)
        {
            if (Closed)
                g.DrawClosedCurve(closedPathPen, path.Take(path.Count - 1).ToArray());
            else if (path.Count >= 2)
                g.DrawCurve(pathPen, path.ToArray());
        }
    }

    public class PathSegment : Segment
    {
        const float distThreshold = 5;


        public PathSegment(string key, SegmentRoot root)
            : base(key, root)
        {

        }


        bool IsClosed(List<PointF> path, float threshold, Matrix transform)
        {
            if (path == null || path.Count <= 0)
                return false;
            PointF[] visualPath = path.ToArray();
            transform.TransformPoints(visualPath);
            return IsClosed(visualPath, distThreshold);
        }

        bool IsClosed(PointF[] path, float threshold)
        {
            if (path == null)
                return false;
            if (path.Length >= 3)
            {
                PointF start = path[0];
                PointF end = path[path.Length - 1];
                float sqDist = FMath.SqDistance(start, end);
                if (sqDist <= threshold * threshold)
                    return true;
            }
            return false;
        }

        bool IsCrossed(List<PointF> path, Matrix transform)
        {
            if (path == null || path.Count <= 0)
                return false;
            PointF[] visualPath = path.ToArray();
            transform.TransformPoints(visualPath);
            return IsCrossed(visualPath);
        }
        bool IsCrossed(PointF[] path)
        {
            if (path == null)
                return false;
            bool crossed = false;

            if (path.Length >= 3)
            {
                PointF src0 = path[path.Length - 2];
                PointF dst0 = path[path.Length - 1];
                for (int i = 0; i <= path.Length - 4; i++)
                {
                    PointF src1 = path[i];
                    PointF dst1 = path[i + 1];
                    if (FMath.IsCrossed(src0, dst0, src1, dst1))
                    {
                        crossed = true;
                        break;
                    }
                }
            }
            return crossed;
        }
    }

    //    public class LazyBrushSegment : Segment
    //  {
    //}
}
