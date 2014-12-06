using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;
using FLib;

namespace Magic2D
{
    /// SkeletonFittingタブで作成する。骨格データおよび関節の分割線などの注釈情報
    public class SkeletonAnnotation
    {
        public Bitmap bmp;
        public List<JointAnnotation> joints = new List<JointAnnotation>();
        public List<BoneAnnotation> bones = new List<BoneAnnotation>();
        public SkeletonAnnotation(Bitmap bmp)
        {
            this.bmp = bmp;
        }
        public SkeletonAnnotation(SkeletonAnnotation an, bool copyBmp)
        {
            if (an == null)
                return;

            if (copyBmp && an.bmp != null)
                this.bmp = new Bitmap(an.bmp);

            this.joints = new List<JointAnnotation>();
            foreach (var j in an.joints)
                this.joints.Add(new JointAnnotation(j.name, j.position));

            this.bones = new List<BoneAnnotation>();
            foreach (var b in an.bones)
            {
                if (!an.joints.Contains(b.src) || !an.joints.Contains(b.dst))
                    continue;
                int idx0 = an.joints.IndexOf(b.src);
                int idx1 = an.joints.IndexOf(b.dst);
                this.bones.Add(new BoneAnnotation(this.joints[idx0], this.joints[idx1]));
            }
        }

        public JointAnnotation GetNearestJoint(PointF point, float threshold, Matrix transform)
        {
            JointAnnotation nearest = null;
            float minSqDist = threshold * threshold;
            foreach (var joint in joints)
            {

                PointF[] jointPt = new[] { joint.position };
                transform.TransformPoints(jointPt);
                float dx = point.X - jointPt[0].X;
                float dy = point.Y - jointPt[0].Y;
                float sqDist = dx * dx + dy * dy;
                if (minSqDist > sqDist)
                {
                    nearest = joint;
                    minSqDist = sqDist;
                }
            }
            return nearest;
        }

        public BoneAnnotation GetNearestBone(PointF point, int threshold, Matrix transform)
        {
            float minDist = threshold;
            BoneAnnotation bone = null;

            for (int i = 0; i < bones.Count; i++)
            {
                PointF[] bonePts = new[] { bones[i].src.position, bones[i].dst.position };
                transform.TransformPoints(bonePts);
                float dist = FMath.GetDistanceToLine(point, bonePts[0], bonePts[1]);
                if (dist < minDist)
                {
                    minDist = dist;
                    bone = bones[i];
                }
            }

            return bone;
        }

        public static SkeletonAnnotation Load(string filepath)
        {
            return Load(filepath, null);
        }

        public static SkeletonAnnotation Load(string filepath, Bitmap refSkeletonBmp)
        {
            if (!File.Exists(filepath))
                return null;

            filepath = Path.GetFullPath(filepath);

            SkeletonAnnotation an = new SkeletonAnnotation(refSkeletonBmp);

            an.joints = File.ReadAllLines(filepath)
                .Where(line => !string.IsNullOrWhiteSpace(line) && line.Contains(':'))
                .Select(line =>
                {
                    var tokens = line.Split(':');
                    if (tokens.Length != 2)
                        return null;
                    var xys = tokens[1].Split(',');
                    if (xys.Length != 2)
                        return null;
                    float x, y;
                    if (!float.TryParse(xys[0], out x) || !float.TryParse(xys[1], out y))
                        return null;
                    return new JointAnnotation(tokens[0], new PointF(x, y));
                })
                .Where(j => j != null)
                .ToList();

            an.bones = File.ReadAllLines(filepath)
                .Where(line => !string.IsNullOrWhiteSpace(line) && line.Contains('>'))
                .Select(line =>
                {
                    var tokens = line.Split('>');
                    if (tokens.Length != 2)
                        return null;
                    JointAnnotation src = null, dst = null;
                    foreach (var j in an.joints)
                    {
                        if (j.name == tokens[0])
                            src = j;
                        if (j.name == tokens[1])
                            dst = j;
                    }
                    if (src == null || dst == null)
                        return null;

                    return new BoneAnnotation(src, dst);
                })
                .Where(b => b != null)
                .ToList();

            return an;
        }

        /*
                public static SkeletonAnnotation CreateHumanSkeleton(float width, float height)
                {
                    if (height <= 0)
                        return null;

                    SkeletonAnnotation an = new SkeletonAnnotation(null);
                    an.joints = new List<JointAnnotation>()
                    {
                        new JointAnnotation("head", new PointF(0.5f, 0f)),
                        new JointAnnotation("neck", new PointF(0.5f, 0f)),
                        new JointAnnotation("", new PointF(0.5f, 0f)),
                        new JointAnnotation("head", new PointF(0.5f, 0f)),
                        new JointAnnotation("head", new PointF(0.5f, 0f)),
                        new JointAnnotation("head", new PointF(0.5f, 0f)),
                        new JointAnnotation("head", new PointF(0.5f, 0f)),
                        new JointAnnotation("head", new PointF(0.5f, 0f)),

                    return null;
                }
        */
    }

    public class JointAnnotation
    {
        public string name;
        public PointF position;
        public JointAnnotation(string name, PointF position)
        {
            this.name = name;
            this.position = position;
        }
    }

    public class BoneAnnotation
    {
        public JointAnnotation src;
        public JointAnnotation dst;

        public BoneAnnotation(JointAnnotation src, JointAnnotation dst)
        {
            this.src = src;
            this.dst = dst;
        }

        public override string ToString()
        {
            if (src != null && dst != null)
                return src.name + "->" + dst.name;
            return base.ToString();
        }

        public override int GetHashCode()
        {
            if (src == null || dst == null)
                return 0;
            return src.name.GetHashCode() ^ dst.name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as BoneAnnotation);
        }

        public bool Equals(BoneAnnotation t)
        {
            if (Object.ReferenceEquals(t, null))
                return false;
            if (Object.ReferenceEquals(this, t))
                return true;
            if (this.GetType() != t.GetType())
                return false;
            if (src == null || dst == null)
                return false;

            return src.name == t.src.name && dst.name == t.dst.name;
        }

        public static bool operator ==(BoneAnnotation lhs, BoneAnnotation rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(BoneAnnotation lhs, BoneAnnotation rhs)
        {
            return !(lhs == rhs);
        }

    }
}
