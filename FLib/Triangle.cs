using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FLib
{
    // Triangle.NET(http://triangle.codeplex.com/)のラッパ
    public class Triangle
    {
        public struct Parameters
        {
            public float minAngle;
            public float maxAngle;
            public bool conformingDelaunay;
            public bool quality;
            public bool convex;

            public static Parameters Default
            {
                get
                {
                    return new Parameters(false);
                }
            }

            public Parameters(bool convex, float minAngle = 20, float maxAngle = 180, bool conformingDelaunay = true, bool quality = true)
            {
                this.minAngle = minAngle;
                this.maxAngle = maxAngle;
                this.conformingDelaunay = conformingDelaunay;
                this.quality = quality;
                this.convex = convex;
            }
        }

        public static void Triangulate(List<PointF> path, List<PointF> outVertices, List<int> outIndices, Parameters parameters)
        {
            if (outVertices == null || outIndices == null)
                return;

            var mesh = TriangulateMesh(path, parameters.minAngle, parameters.maxAngle, parameters.conformingDelaunay, parameters.quality, parameters.convex);
            if (mesh == null)
                return;

            outVertices.Clear();
            outIndices.Clear();

            for (int i = 0; i < mesh.Vertices.Count; i++)
                outVertices.Add(VertexToPoint(mesh.Vertices.ElementAt(i)));

            Dictionary<int, int> v2i = new Dictionary<int, int>();
            for (int i = 0; i < mesh.Vertices.Count; i++)
                v2i[mesh.Vertices.ElementAt(i).ID] = i;

            foreach (var t in mesh.Triangles)
            {
                outIndices.Add(v2i[t.GetVertex(0).ID]);
                outIndices.Add(v2i[t.GetVertex(1).ID]);
                outIndices.Add(v2i[t.GetVertex(2).ID]);
            }
        }

        static TriangleNet.Mesh TriangulateMesh(List<PointF> path, float minAngle, float maxAngle, bool conformingDelaunay, bool quality, bool convex)
        {
            if (path == null)
                return null;
            if (path.Count <= 2)
                return null;

            MeshRenderer.Core.RenderData renderData = new MeshRenderer.Core.RenderData();
            MeshRenderer.Core.RenderManager renderManager = new MeshRenderer.Core.RenderManager();
            TriangleNet.Geometry.InputGeometry input = new TriangleNet.Geometry.InputGeometry(path.Count);
            TriangleNet.Mesh mesh = new TriangleNet.Mesh();

            input.AddPoint(path[0].X, path[0].Y);
            for (int i = 1; i < path.Count; i++)
            {
                input.AddPoint(path[i].X, path[i].Y);
                input.AddSegment(i - 1, i);
            }
            input.AddSegment(path.Count - 1, 0);

            renderData.SetInputGeometry(input);

            renderManager.CreateDefaultControl();
            renderManager.SetData(renderData);

            mesh.Behavior.MinAngle = FMath.Clamp(0, 40, minAngle);
            mesh.Behavior.MaxAngle = FMath.Clamp(80, 180, maxAngle);
            mesh.Behavior.ConformingDelaunay = conformingDelaunay;
            mesh.Behavior.Quality = quality;
            mesh.Behavior.Convex = convex;

            mesh.Triangulate(input);

            return mesh;
        }

        static PointF VertexToPoint(TriangleNet.Data.Vertex v)
        {
            return new PointF((float)v.X, (float)v.Y);
        }
    }

    public struct TriMesh
    {
        public int idx0;
        public int idx1;
        public int idx2;
        public TriMesh(int idx0, int idx1, int idx2)
        {
            this.idx0 = idx0;
            this.idx1 = idx1;
            this.idx2 = idx2;
        }
    }
}
