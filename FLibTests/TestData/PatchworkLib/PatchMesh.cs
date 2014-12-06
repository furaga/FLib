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
using ARAPDeform = PatchworkLib.ARAPDeformation.ARAPDeformation;

namespace PatchworkLib.PatchMesh
{
    public class PatchMesh
    {
        Guid _guid = Guid.NewGuid();
        internal string Id { get { return _guid.ToString(); } }

        internal List<PatchVertex> vertices = new List<PatchVertex>();
        List<PatchControlPoint> controlPoints = new List<PatchControlPoint>();
        internal List<PatchTriangle> triangles = new List<PatchTriangle>();

        // verticesのうち、メッシュの外周をなすもののインデックスのリスト
        internal List<int> pathIndices = new List<int>();

        ARAPDeform arap;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawVertices"></param>
        /// <param name="rawPathIndices"></param>
        /// <param name="vert2part">nullならすべての頂点が"-1"に所属する</param>
        /// <param name="patchKey"></param>
        /// <param name="textureSize"></param>
        internal PatchMesh(List<PointF> rawVertices, List<int> rawTriangleIndices, List<int> rawPathIndices, List<int> vert2part, string patchKey, SizeF textureSize)
        {
            if (rawVertices == null || rawPathIndices == null)
                return;

            vertices.Clear();
            
            for (int i = 0; i < rawVertices.Count; i++)
            {
                int part = -1;
                if (vert2part != null && vert2part.Count > i)
                    part = vert2part[i];
                PointF coord = new PointF(rawVertices[i].X / textureSize.Width, rawVertices[i].Y / textureSize.Height);
                vertices.Add(new PatchVertex(rawVertices[i], part, patchKey, coord));
            }
            pathIndices = new List<int>(rawPathIndices);

            for (int i = 0; i + 2 < rawTriangleIndices.Count; i += 3)
                triangles.Add(new PatchTriangle(rawTriangleIndices[i], rawTriangleIndices[i + 1], rawTriangleIndices[i + 2], patchKey));

            arap = new ARAPDeform(rawVertices, vert2part);

        }

        /// <summary>
        /// 引数をコピーして新しいインスタンスを作る。参照は保持しない
        /// </summary>
        internal PatchMesh(List<PatchVertex> vertices, List<PatchControlPoint> controls, List<PatchTriangle> mesh, List<int> path)
        {
            foreach (var v in vertices)
                this.vertices.Add(new PatchVertex(v));
//            foreach (var c in controls)
//                this.controlPoints.Add(new PatchControlPoint(c));
            foreach (var t in mesh)
                this.triangles.Add(new PatchTriangle(t));
            foreach (int p in path)
                this.pathIndices.Add(p);

            var rawVertices = this.vertices.Select(v => v.orgPosition).ToList();
            var vert2part = this.vertices.Select(v => v.part).ToList();

            arap = new ARAPDeform(rawVertices, vert2part);
            foreach (var c in controls)
                AddControlPoint(c.position, c.orgPosition);

        }

        internal static PatchMesh Copy(PatchMesh mesh)
        {
            return new PatchMesh(mesh.vertices, mesh.controlPoints, mesh.triangles, mesh.pathIndices);
        }

        //-------------------------------------------------------------------------
        // 外周の取得
        //-------------------------------------------------------------------------

        List<PatchVertex> GetPath()
        {
            if (pathIndices == null)
                return new List<PatchVertex>();
            List<PatchVertex> path = new List<PatchVertex>();
            foreach (var idx in pathIndices)
            {
                if (vertices.Count <= idx)
                    return new List<PatchVertex>();
                path.Add(vertices[idx]);
            }
            return path;
        }

        //-------------------------------------------------------------------------
        // メッシュの変形
        //-------------------------------------------------------------------------

        public void ClearControlPoints()
        {
            controlPoints.Clear();
        }

        public void AddControlPoint(PointF pt, PointF orgPt)
        {
            if (controlPoints.Any(c => c.position == pt))
                return;

            int part = -1;
            int minIdx;

            // 一番近い頂点を探して、それと同じpartを割り当てる
            FMath.GetMinElement(vertices, v => FMath.SqDistance(v.position, pt), out minIdx);
            if (minIdx >= 0)
                part = vertices[minIdx].part;
            
            var cp = new PatchControlPoint(orgPt, part);
            cp.position = pt;

            controlPoints.Add(cp);
        }


        public void RemoveControlPoint(PointF pt)
        {
            if (controlPoints.Any(c => c.position == pt))
                controlPoints.Remove(controlPoints.First(c => c.position == pt));
        }

        public void TranslateControlPoint(PointF pt, PointF to, bool flush)
        {
            if (controlPoints.Any(c => c.position == pt))
            {
                controlPoints.First(c => c.position == pt).position = to;
                if (flush)
                    FlushDefomation();
            }
        }

        /*        public void AddControlPoint(PointF controlPoint, PointF orgControlPoint)
                {
                    int part;
                    bool succeed = arap.AddControlPoint(controlPoint, orgControlPoint, out part);
                    if (succeed)
                    {
                        var c = new PatchControlPoint(orgControlPoint, part);
                        c.position = controlPoint;
                        controlPoints.Add(c);
                    }
                }

                public void RemoveControlPoint(PointF pt)
                {
                    int idx = arap.RemoveControlPoint(pt);
                    if (idx >= 0)
                       controlPoints.RemoveAt(idx);
                }

                public void TranslateControlPoint(PointF pt, PointF to, bool flush)
                {
                    int idx = arap.TranslateControlPoint(pt, to, flush);
                    if (idx >= 0)
                        controlPoints[idx].position = to;

                    if (flush)
                        FlushDefomation();
                }
                */

        internal PatchControlPoint FindControlPoint(PointF orgPosition)
        {
            foreach (var c in controlPoints)
                if (c.orgPosition == orgPosition)
                    return c;
            return null;
        }

        public void FlushDefomation()
        {
            // 制御点をPatchMesh, ARAPで同期させる
            arap.UpdateControlPoint(controlPoints.Select(c => c.position).ToList());

            // ARAP変形
            arap.FlushDefomation();

            // 結果を取得
            List<PointF> ptList = arap.CopyMeshPointList();
            for (int i = 0; i < vertices.Count; i++)
                vertices[i].position = ptList[i];
        }

        public void BeginDeformation()
        {
            // 制御点をPatchMesh, ARAPで同期させる

            arap.ClearControlPoints();
            foreach (var c in controlPoints)
                arap.AddControlPoint(c.position, c.orgPosition, c.part);

            arap.BeginDeformation();
        }

        public void EndDeformation()
        {
            arap.EndDeformation();
        }

        /// <summary>
        /// PatchMesh.controlPointsをリストにコピーして返す。
        /// ARAPと同期をとっているデータなので、クライアントがPatchMesh.controlPointsを直接いじれると絶対バグるため
        /// </summary>
        /// <returns></returns>
        internal List<PatchControlPoint> CopyControlPoints()
        {
            var ls = new List<PatchControlPoint>();
            foreach (var c in controlPoints)
                ls.Add(new PatchControlPoint(c));
            return ls;
        }


        internal void ScaleByRatio(float rx, float ry)
        {
            for (int i = 0; i < vertices.Count; i++)
                vertices[i].ScaleByRatio(rx, ry);
            
            for (int i = 0; i < controlPoints.Count; i++)
                controlPoints[i].ScaleByRatio(rx, ry);

            arap.ScaleByRatio(rx, ry);
        }
    }
}
