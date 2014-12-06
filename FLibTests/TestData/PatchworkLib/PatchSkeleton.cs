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

namespace PatchworkLib.PatchMesh
{
    /// <summary>
    /// 各パッチに対応付けられる骨格
    /// </summary>
    public class PatchSkeleton
    {
        public List<PatchSkeletonJoint> joints = new List<PatchSkeletonJoint>();
        public List<PatchSkeletonBone> bones = new List<PatchSkeletonBone>();

        Tree<PatchSkeletonJoint> jointTree;

        public PatchSkeleton()
        {
        }

        PatchSkeleton(PatchSkeleton skl)
        {
            if (skl == null)
                return;

            this.joints = new List<PatchSkeletonJoint>();
            foreach (var j in skl.joints)
                this.joints.Add(new PatchSkeletonJoint(j.name, j.position));

            this.bones = new List<PatchSkeletonBone>();
            foreach (var b in skl.bones)
            {
                this.bones.Add(new PatchSkeletonBone(
                    this.joints.First(j => j.name == b.src.name),  
                    this.joints.First(j => j.name == b.dst.name)));
            }
        }

        public static PatchSkeleton Copy(PatchSkeleton skl)
        {
            return new PatchSkeleton(skl);
        }

        public void BuildJointTree()
        {
            int rootIdx;
            FMath.GetMinElement<PatchSkeletonJoint>(joints, j => bones.Count(b => b.dst == j), out rootIdx);

            HashSet<PatchSkeletonJoint> jointSet = new HashSet<PatchSkeletonJoint>(joints);
            jointTree = new Tree<PatchSkeletonJoint>(joints[rootIdx]);

            // DFS
        }

        void BuildJointTree_rec(Tree<PatchSkeletonJoint> tree, PatchSkeletonJoint joint, List<PatchSkeletonBone> bones)
        {/*
            var subTree = 
            foreach (var b in bones.Where(b => b.src == joint))
            {
                BuildJointTree_rec(tree, b.dst, bones);
            }
            */
        }

        public PatchSkeletonJoint GetNearestJoint(PointF point, float threshold, Matrix transform)
        {
            PatchSkeletonJoint nearest = null;
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

        public PatchSkeletonBone GetNearestBone(PointF point, int threshold, Matrix transform)
        {
            float minDist = threshold;
            PatchSkeletonBone bone = null;

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

        public static PatchSkeleton Load(string filepath)
        {
            return Load(filepath, null);
        }

        public static PatchSkeleton Load(string filepath, Bitmap refSkeletonBmp)
        {
            if (!File.Exists(filepath))
                return null;

            filepath = Path.GetFullPath(filepath);

            var an = new PatchSkeleton();

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
                    return new PatchSkeletonJoint(tokens[0], new PointF(x, y));
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
                    PatchSkeletonJoint src = null, dst = null;
                    foreach (var j in an.joints)
                    {
                        if (j.name == tokens[0])
                            src = j;
                        if (j.name == tokens[1])
                            dst = j;
                    }
                    if (src == null || dst == null)
                        return null;

                    return new PatchSkeletonBone(src, dst);
                })
                .Where(b => b != null)
                .ToList();

            return an;
        }

        internal void ScaleByRatio(float rx, float ry)
        {
            for (int i = 0; i < joints.Count; i++)
                joints[i].position = new PointF(joints[i].position.X * rx, joints[i].position.Y * ry);
        }
    }

    /// <summary>
    /// 各パッチに対応付けられている関節位置
    /// </summary>
    public class PatchSkeletonJoint
    {
        public string name;
        public PointF position;
        public PatchSkeletonJoint(string name, PointF position)
        {
            this.name = name;
            this.position = position;
        }
    }

    /// <summary>
    /// 各パッチに対応付けられている骨
    /// </summary>
    public class PatchSkeletonBone
    {
        public PatchSkeletonJoint src;
        public PatchSkeletonJoint dst;

        public PatchSkeletonBone(PatchSkeletonJoint src, PatchSkeletonJoint dst)
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
            return this.Equals(obj as PatchSkeletonBone);
        }

        public bool Equals(PatchSkeletonBone t)
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

        public static bool operator ==(PatchSkeletonBone lhs, PatchSkeletonBone rhs)
        {
            if (Object.ReferenceEquals(lhs, null))
            {
                if (Object.ReferenceEquals(rhs, null))
                    return true;
                return false;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(PatchSkeletonBone lhs, PatchSkeletonBone rhs)
        {
            return !(lhs == rhs);
        }

    }
}

