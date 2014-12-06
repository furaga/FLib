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
    public class PatchSkeletonFitting
    {
        public static void Fitting(PatchSkeletalMesh mesh, PatchSkeleton skl)
        {
            // スケルトンに合わせて制御点を移動
            foreach (var kv in mesh.skeletalControlPointDict)
            {
                PatchSkeletonBone b = kv.Key;
                List<PointF> orgPts = kv.Value;
                PatchSkeletonBone br = CorrespondingBone(skl, b);

                if (br == null)
                    continue;
                if (orgPts.Count <= 1)
                    continue;

                for (int i = 0; i < orgPts.Count; i++)
                {
                    float t = (float)i / (orgPts.Count - 1);
                    float x = br.src.position.X * (1 - t) + br.dst.position.X * t;
                    float y = br.src.position.Y * (1 - t) + br.dst.position.Y * t;
                    PatchControlPoint c = mesh.mesh.FindControlPoint(orgPts[i]);
                    if (c == null)
                        continue;
                    mesh.mesh.TranslateControlPoint(c.position, new PointF(x, y), false);
                }

                // スケルトン(mesh.skl)も動かす
                b.src.position = br.src.position;
                b.dst.position = br.dst.position;
            
            }
            // 制御点の移動を反映してメッシュ変形
            mesh.mesh.FlushDefomation();


        }

        // スケルトン内のbと同じ（jointの名前が同じ）ボーンを探す
        static PatchSkeletonBone CorrespondingBone(PatchSkeleton skl, PatchSkeletonBone b)
        {
            if (skl == null || b == null || skl.bones == null)
                return null;
            foreach (var bb in skl.bones)
                // オーバーライドしてるjointの名前が同じならtrue
                if (b == bb)
                    return bb;
            return null;
        }
    }
}
