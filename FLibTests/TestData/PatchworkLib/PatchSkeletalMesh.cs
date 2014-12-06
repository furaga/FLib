using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using PatchSection = System.Drawing.CharacterRange;

namespace PatchworkLib.PatchMesh
{
    /// <summary>
    /// パッチのポリゴン(PatchMesh)）と骨格(PatchSkeleton)を持つクラス
    /// パッチの接続はこれを使って行う
    /// </summary>
    public class PatchSkeletalMesh
    {
        public PatchMesh Mesh { get { return mesh;  } }

        internal PatchMesh mesh;
        internal PatchSkeleton skl;
        internal List<PatchSection> sections = new List<PatchSection>();
        internal Dictionary<PatchSkeletonBone, List<PointF>> skeletalControlPointDict = new Dictionary<PatchSkeletonBone, List<PointF>>();

        private PointF scale = new PointF(1,1);

        // コピーせずに参照をそのまま格納する
        public PatchSkeletalMesh(PatchMesh mesh, PatchSkeleton skl, List<PatchSection> sections, bool setSkeletalControlPoints = true)
        {
            this.mesh = mesh;
            this.skl = skl;
            this.sections = new List<PatchSection>(sections);
            this.skeletalControlPointDict = CreateSkeletalControlPoints(skl, 20);

            // 既存の制御点を消して、スケルトン周りの制御点を追加
            if (setSkeletalControlPoints)
            {
                this.mesh.ClearControlPoints();
                foreach (var ls in this.skeletalControlPointDict.Values)
                    foreach (var pt in ls)
                        this.mesh.AddControlPoint(pt, pt);
            }
        }

        public static PatchSkeletalMesh Copy(PatchSkeletalMesh org)
        {
            var m = PatchMesh.Copy(org.mesh);
            var s = PatchSkeleton.Copy(org.skl);
            PatchSkeletalMesh copy = new PatchSkeletalMesh(m, s, org.sections, false);
            return copy;
        }

        static Dictionary<PatchSkeletonBone, List<PointF>> CreateSkeletalControlPoints(PatchSkeleton skl, int linearSpan)
        {
            Dictionary<PatchSkeletonBone, List<PointF>> dict = new Dictionary<PatchSkeletonBone, List<PointF>>();
            // ボーン沿いに制御点を追加
            if (skl != null && linearSpan >= 1)
            {
                foreach (var b in skl.bones)
                {
                    dict[b] = new List<PointF>();
                    float dist =FLib. FMath.Distance(b.src.position, b.dst.position);
                    int ptNum = Math.Max(2, (int)(dist / linearSpan) + 1);
                    for (int i = 0; i < ptNum; i++)
                    {
                        float t = (float)i / (ptNum - 1);
                        // 誤差の問題があるので i == 0, ptNum - 1のときは明示的に端点を代入する
                        PointF p;
                        if (i == 0)
                            p = b.src.position;
                        else if (i == ptNum - 1)
                            p = b.dst.position;
                        else
                            p = FLib.FMath.Interpolate(b.src.position, b.dst.position, t);
                        dict[b].Add(p);
                    }
                }
            }
            return dict;
        }

        /// <summary>
        /// メッシュを拡大する。拡大後の拡大率で指定
        /// </summary>
        public void Scale(float sx, float sy)
        {
            if (Math.Abs(sx) <= 1e-4 || Math.Abs(sx) <= 1e-4)
                throw new Exception(string.Format("Scale: |sx| (={0}) and |sy| (={1}) cannot be less than 1e-4", Math.Abs(sx), Math.Abs(sy)));
            
            ScaleByRatio(sx / scale.X, sy / scale.Y);
        }

        /// <summary>
        /// メッシュを拡大する。現在のscale値の何倍するかで指定
        /// </summary>
        public void ScaleByRatio(float rx, float ry)
        {
            if (rx <= 0 || ry <= 0)
                throw new Exception(string.Format("ScaleByRatio: rx (={0}) and ry (={1}) cannot be less than 0", rx, ry));

            scale.X *= rx;
            scale.Y *= ry;

            // メッシュ・スケルトン・ボーン（と制御点）を(rx, ry)倍する
            mesh.ScaleByRatio(rx, ry);
            skl.ScaleByRatio(rx, ry);

            foreach (var kv in skeletalControlPointDict)
            {
                PatchSkeletonBone b = kv.Key;
                b.src.position = new PointF(b.src.position.X * rx, b.src.position.Y * ry);
                b.dst.position = new PointF(b.dst.position.X * rx, b.dst.position.Y * ry);

                List<PointF> ls = kv.Value;
                for (int i = 0; i < ls.Count; i++)
                    ls[i] = new PointF(ls[i].X * rx, ls[i].Y * ry);
            }

        }

    }
}
