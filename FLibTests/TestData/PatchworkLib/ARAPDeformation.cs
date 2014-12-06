using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using FLib;

namespace PatchworkLib.ARAPDeformation
{
    class ARAPDeformation
    {
        List<PointF> meshPointList = new List<PointF>();
        List<PointF> orgMeshPointList = new List<PointF>();
        List<int> meshPtToPart = new List<int>();

        List<PointF> controls = new List<PointF>();
        List<PointF> orgControls = new List<PointF>();
        List<int> controlsToPart = new List<int>();

        float[] weights;
        float[] A00;
        float[] A01;
        float[] A10;
        float[] A11;
        PointF[] D;

        public ARAPDeformation(List<PointF> orgPoints, List<int> pointToPart)
        {
            this.meshPointList = new List<PointF>(orgPoints);
            this.orgMeshPointList = new List<PointF>(orgPoints);
            this.meshPtToPart = new List<int>(pointToPart);
        }

        public List<PointF> CopyMeshPointList()
        {
            return new List<PointF>(meshPointList);
        }

        internal void ClearControlPoints()
        {
            controls.Clear();
            controlsToPart.Clear();
            orgControls.Clear();
            weights = A00 = A01 = A10 = A11 = null;
            D = null;
        }

        internal bool AddControlPoint(PointF pt, PointF orgPt, int part)
        {
            if (controls.Contains(pt))
                return false;

            controls.Add(pt);
            orgControls.Add(orgPt);
            controlsToPart.Add(part);

            return true;
        }

        internal void UpdateControlPoint(List<PointF> pts)
        {
            if (pts.Count < controls.Count)
                return;

            for (int i = 0; i < controls.Count; i++)
                controls[i] = pts[i];
        }

        public void FlushDefomation()
        {
            RigidMLS();
        }

        public void BeginDeformation()
        {
            Precompute();
        }

        public void EndDeformation()
        {
            weights = null;
            A00 = null;
            A01 = null;
            A10 = null;
            A11 = null;
            D = null;
        }

        void Precompute()
        {
            if (orgControls.Count < 3)
                return;

            weights = new float[meshPointList.Count * orgControls.Count];
            A00 = new float[meshPointList.Count * orgControls.Count];
            A01 = new float[meshPointList.Count * orgControls.Count];
            A10 = new float[meshPointList.Count * orgControls.Count];
            A11 = new float[meshPointList.Count * orgControls.Count];
            D = new PointF[meshPointList.Count];

            for (int vIdx = 0; vIdx < meshPointList.Count; vIdx++)
            {
                int offset = vIdx * orgControls.Count;
                for (int i = 0; i < orgControls.Count; i++)
                {
                    if (meshPtToPart[vIdx] == controlsToPart[i])
                    {
                        // ぴったり同じ位置だったら無限の重みを与える
                        if (FMath.Distance(orgControls[i], orgMeshPointList[vIdx]) <= 1e-4)
                            weights[i + offset] = float.PositiveInfinity;
                        else
                            weights[i + offset] = (float)(1 / (0.01 + Math.Pow(FMath.Distance(orgControls[i], orgMeshPointList[vIdx]), 2)));
                    }
                    else
                    {
                        weights[i + offset] = 0;
                    }
                }

                // 追加　2014/11/08
                int nonzeroCnt = 0;
                for (int i = 0; i < orgControls.Count; i++)
                    if (weights[i + offset] != 0)
                        nonzeroCnt++;
                if (nonzeroCnt <= 1)
                    continue;
//                    return;

                PointF? Pa = AverageWeight(orgControls, weights, vIdx);
                if (Pa == null || !Pa.HasValue)
                    // 変更　2014/11/08
                    continue;
//                    return;

                PointF[] Ph = new PointF[orgControls.Count];
                for (int i = 0; i < orgControls.Count; i++)
                {
                    if (!orgControls[i].IsEmpty)
                    {
                        Ph[i].X = orgControls[i].X - Pa.Value.X;
                        Ph[i].Y = orgControls[i].Y - Pa.Value.Y;
                    }
                }

                float mu = 0;
                for (int i = 0; i < orgControls.Count; i++)
                    mu += (float)(Ph[i].X * Ph[i].X + Ph[i].Y * Ph[i].Y) * weights[i + offset];


                D[vIdx].X = orgMeshPointList[vIdx].X - Pa.Value.X;
                D[vIdx].Y = orgMeshPointList[vIdx].Y - Pa.Value.Y;
                for (int i = 0; i < orgControls.Count; i++)
                {
                    int idx = i + offset;
                    A00[idx] = weights[idx] / mu * (Ph[i].X * D[vIdx].X + Ph[i].Y * D[vIdx].Y);
                    A01[idx] = -weights[idx] / mu * (Ph[i].X * (-D[vIdx].Y) + Ph[i].Y * D[vIdx].X);
                    A10[idx] = -weights[idx] / mu * (-Ph[i].Y * D[vIdx].X + Ph[i].X * D[vIdx].Y);
                    A11[idx] = weights[idx] / mu * (Ph[i].Y * D[vIdx].Y + Ph[i].X * D[vIdx].X);
                }
            }
        }

        float[] Ortho(float[] v, int i) { return new float[] { -v[3 * i + 1], v[3 * i], 0 }; }

        float LengthSquared(float[] vecs, int i)
        {
            float x = vecs[3 * i];
            float y = vecs[3 * i + 1];
            float z = vecs[3 * i + 2];
            return x * x + y * y + z * z;
        }

        float Dot(float[] v0, int i, float[] v1, int j)
        {
            return v0[3 * i + 0] * v1[3 * j + 0] +
                v0[3 * i + 1] * v1[3 * j + 1] +
                v0[3 * i + 2] * v1[3 * j + 2];
        }

        void RigidMLS()
        {
            if (controls.Count < 3)
                return;

            if (weights == null || A00 == null || A01 == null || A10 == null || A11 == null || D == null)
                return;

            for (int vIdx = 0; vIdx < meshPointList.Count; vIdx++)
            {
                int offset = vIdx * controls.Count;

                // 追加　2014/11/08
                int nonzeroCnt = 0;
                for (int i = 0; i < controls.Count; i++)
                    if (weights[i + offset] != 0)
                        nonzeroCnt++;
                if (nonzeroCnt <= 1)
                    continue;


                bool flg = false;
                for (int i = offset; i < offset + controls.Count; i++)
                {
                    if (float.IsInfinity(weights[i]))
                    {
                        // infに飛んでたらcontrols[i]自体を返す
                        meshPointList[vIdx] = controls[i - offset];
                        flg = true;
                        break;
                    }
                }
                if (flg)
                    continue;

                PointF? Qa = AverageWeight(controls, weights, vIdx);
                if (Qa == null || !Qa.HasValue)
                    continue;

                meshPointList[vIdx] = Qa.Value;
                float fx = 0;
                float fy = 0;
                for (int i = 0; i < controls.Count; i++)
                {
                    int idx = i + vIdx * controls.Count;
                    float qx = controls[i].X - Qa.Value.X;
                    float qy = controls[i].Y - Qa.Value.Y;
                    fx += qx * A00[idx] + qy * A10[idx];
                    fy += qx * A01[idx] + qy * A11[idx];
                }
                float lenD = (float)Math.Sqrt(D[vIdx].X * D[vIdx].X + D[vIdx].Y * D[vIdx].Y);
                float lenf = (float)Math.Sqrt(fx * fx + fy * fy);
                float k = lenD / (0.01f + lenf);
                PointF pt = meshPointList[vIdx];
                pt.X += fx * k;
                pt.Y += fy * k;
                meshPointList[vIdx] = pt;
            }
        }

        PointF? AverageWeight(List<PointF> controls, float[] weight, int vIdx)
        {
            PointF pos_sum = PointF.Empty;

            int offset = vIdx * controls.Count;

            float w_sum = 0;
            for (int i = offset; i < offset + controls.Count; i++)
                w_sum += weight[i];

            if (w_sum <= 0)
                return null;

            for (int i = offset; i < offset + controls.Count; i++)
            {
                var p = controls[i - offset];
                pos_sum.X += p.X * weight[i];
                pos_sum.Y += p.Y * weight[i];
            }

            pos_sum.X /= w_sum;
            pos_sum.Y /= w_sum;

            return pos_sum;
        }

        public PointF? OrgToCurControlPoint(PointF orgPt)
        {
            if (!orgControls.Contains(orgPt))
                return null;
            int idx = orgControls.IndexOf(orgPt);
            return controls[idx];
        }

        /// <summary>
        /// 独立したARAP変形用の関数。少ない頂点をいますぐ変形させたいときに使う(現状PatchConnector.ExpandPatches()内のみで利用)
        /// </summary>
        /// <param name="pts">動かしたい点</param>
        /// <param name="moves">orgControlPoint - currentControlPoint のリスト</param>
        /// <returns></returns>
        internal static List<PointF> Deform(List<PointF> pts, List<Tuple<PointF, PointF>> moves)
        {
            List<PointF> newPts = new List<PointF>();
            for (int i = 0; i < pts.Count; i++)
            {
                bool finish = false;
                List<float> ws = new List<float>();
                float w_sum = 0;

                foreach (var mv in moves)
                {
                    if (mv.Item1 == pts[i])
                    {
                        newPts.Add(mv.Item2);
                        break;
                    }
                    float w = (float)(1 / (0.01 + Math.Pow(FMath.Distance(mv.Item1, pts[i]), 2)));
                    ws.Add(w);
                    w_sum += w;
                }

                if (finish)
                    continue;

                if (w_sum <= 1e-4)
                {
                    newPts.Add(pts[i]);
                    continue;
                }

                float inv_w_sum = 1 / w_sum;
                for (int j = 0; j < moves.Count; j++)
                    ws[j] *= inv_w_sum;

                float x = pts[i].X;
                float y = pts[i].Y;
                for (int j = 0; j < moves.Count; j++)
                {
                    var mv = moves[j];
                    float dx = mv.Item2.X - mv.Item1.X;
                    float dy = mv.Item2.Y - mv.Item1.Y;
                    x += dx * ws[j];
                    y += dy * ws[j];
                }
                newPts.Add(new PointF(x, y));
            }
            return newPts;
        }

        /// <summary>
        /// メッシュ・制御点をrx, ry倍する。
        /// PatchMesh.csで１からarapを作りなおしても良いが、事前計算に時間がかかるのでしない
        /// </summary>
        /// <param name="rx"></param>
        /// <param name="ry"></param>
        internal void ScaleByRatio(float rx, float ry)
        {
            for (int i = 0; i < meshPointList.Count; i++)
                meshPointList[i] = new PointF(meshPointList[i].X * rx, meshPointList[i].Y * ry);

            for (int i = 0; i < orgMeshPointList.Count; i++)
                orgMeshPointList[i] = new PointF(orgMeshPointList[i].X * rx, orgMeshPointList[i].Y * ry);

            for (int i = 0; i < controls.Count; i++)
                controls[i] = new PointF(controls[i].X * rx, controls[i].Y * ry);

            for (int i = 0; i < orgControls.Count; i++)
                orgControls[i] = new PointF(orgControls[i].X * rx, orgControls[i].Y * ry);
        }
    }
}