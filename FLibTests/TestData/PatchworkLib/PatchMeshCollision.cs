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

namespace PatchworkLib.PatchMesh
{
    public static class PatchMeshCollision
    {
        public static bool IsHit(PatchMesh mesh, PointF p)
        {
            if (mesh == null)
                return false;

            List<PointF> path = new List<PointF>();
            for (int i = 0; i < 3; i++)
                path.Add(new PointF());

            foreach (var t in mesh.triangles)
            {
                path[0] = mesh.vertices[t.Idx0].position;
                path[1] = mesh.vertices[t.Idx1].position;
                path[2] = mesh.vertices[t.Idx2].position;
                if (FLib.FMath.IsPointInPolygon(p, path))
                    return true;
            }

            return false;
        }

    }
}
