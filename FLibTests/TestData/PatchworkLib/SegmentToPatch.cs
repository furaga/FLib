using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using PatchworkLib.PatchMesh;
using PatchSection = System.Drawing.CharacterRange;

namespace Magic2D
{
    public class SegmentToPatch
    {
        /// <param name="pathPointInterval">パスの間隔。値が小さいほどメッシュが細かくなる</param>
        public static Dictionary<string, PatchSkeletalMesh> LoadPatches(string root, string segmentationDirName, Dictionary<string, Bitmap> bitmaps, int pathPointInterval)
        {
            var segmentDict = SegmentLoader.LoadSegments(root, segmentationDirName);
            Dictionary<string, PatchSkeletalMesh> patchDict = new Dictionary<string, PatchSkeletalMesh>();
            foreach (var kv in segmentDict)
            {
                patchDict[kv.Value.name] = ToPatchSkeletalMesh(kv.Value, bitmaps, pathPointInterval);
                kv.Value.Dispose();
            }
            return patchDict;
        }

        // seg.bmpはどこに格納する?patchmesh?pathcmeshrenderer?
        // => pathcMeshRendererResourcesに格納する
        static PatchSkeletalMesh ToPatchSkeletalMesh(Segment seg, Dictionary<string, Bitmap> bitmaps, int pathPointInterval)
        {
            PatchMesh patchMesh = ToPatchMesh(seg, pathPointInterval);
            PatchSkeleton patchSkeleton = ToPatchSkeleton(seg);
            List<PatchSection> patchSections = ToPatchSections(seg, patchMesh.pathIndices.Select(i => patchMesh.vertices[i].position).ToList());

            PatchSkeletalMesh skeletalMesh = new PatchSkeletalMesh(patchMesh, patchSkeleton, patchSections);

            return skeletalMesh;
        }

        static PatchMesh ToPatchMesh(Segment seg, int pathPointInterval)
        {
            List<PointF> rawPath = new List<PointF>(seg.path);

            // 輪郭からメッシュを生成
            var shiftPath = FLib.FMath.ShiftPath(seg.path, -seg.offset.X, -seg.offset.Y);
            var subdividedPath = PathSubdivision.Subdivide(shiftPath, pathPointInterval);
            subdividedPath.RemoveAt(subdividedPath.Count - 1); // 終点（始点と同じ）は消す

            List<PointF> vertices = new List<PointF>();
            List<int> triIndices = new List<int>();
            FLib.Triangle.Triangulate(subdividedPath, vertices, triIndices, FLib.Triangle.Parameters.Default);

            List<int> path = Enumerable.Range(0, subdividedPath.Count).ToList();

            //
            // seg.partingLineから各頂点が属すpartを計算
            //
            List<int> vert2part = new List<int>();
            var partingLine = new List<PointF>();
            if (seg.partingLine != null)
                partingLine = FLib.FMath.ShiftPath(seg.partingLine, -seg.offset.X, -seg.offset.Y);
            PartingMeshes(vertices, triIndices, partingLine, vert2part);


            string patchKey = seg.name;
            SizeF textureSize = seg.bmp.Size;

            var patch = new PatchMesh(vertices, triIndices, path, vert2part, patchKey, textureSize);

            return patch;
        }



        // FloodFillで領域分割
        static void PartingMeshes(List<PointF> pts, List<int> triIndices, List<PointF> partingLine, List<int> outPtToPart)
        {
            if (outPtToPart == null)
                return;
            if (pts == null)
                return;

            // デフォルトでは全部0にする
            for (int i = 0; i < pts.Count; i++)
                outPtToPart.Add(0);

            if (partingLine == null || partingLine.Count <= 1)
                return;

            Dictionary<int, List<int>> edges = GetEdges(pts, triIndices);
            HashSet<int> boarderEdgeCodes = GetEdgeCodeNearLine(pts, edges, partingLine);
            var labels = FLib.FMath.FloodFill(pts.Count, edges, cond, boarderEdgeCodes);
            FillPointsNearBoarder(pts, edges, boarderEdgeCodes, labels);

            outPtToPart.Clear();
            if (labels != null && labels.Count == pts.Count)
            {
                for (int i = 0; i < labels.Count; i++)
                    outPtToPart.Add(labels[i]);
            }
        }

        static Dictionary<int, List<int>> GetEdges(List<PointF> pts, List<int> triIndices)
        {
            Dictionary<int, List<int>> edges = new Dictionary<int, List<int>>();

            for (int i = 0; i < pts.Count; i++)
                edges[i] = new List<int>();

            if (pts == null || triIndices == null || pts.Count <= 0 || triIndices.Count <= 0)
                return edges;

            for (int i = 0; i < triIndices.Count / 3; i++)
            {
                int idx0 = triIndices[3 * i + 0];
                int idx1 = triIndices[3 * i + 1];
                int idx2 = triIndices[3 * i + 2];
                if (!edges.ContainsKey(idx0) || !edges.ContainsKey(idx1) || !edges.ContainsKey(idx2))
                    continue;
                edges[idx0].AddRange(new[] { idx1 });
                edges[idx1].AddRange(new[] { idx2 });
                edges[idx2].AddRange(new[] { idx0 });
            }

            return edges;
        }

        static HashSet<int> GetEdgeCodeNearLine(List<PointF> pts, Dictionary<int, List<int>> es, List<PointF> line)
        {
            HashSet<int> edgeCodes = new HashSet<int>(); // 分割線と交差しているエッジ

            if (pts == null || es == null || line == null)
                return edgeCodes;

            foreach (var ee in es)
            {
                foreach (int _j in ee.Value)
                {
                    int i = ee.Key;
                    int j = _j;
                    if (i > j)
                        FLib.FMath.Swap(ref i, ref j);
                    if (FLib.FMath.IsCrossed(pts[i], pts[j], line))
                        edgeCodes.Add(i * pts.Count + j);
                }
            }
            return edgeCodes;
        }

        static void FillPointsNearBoarder(List<PointF> pts, Dictionary<int, List<int>> edges, HashSet<int> _edgeCodes, List<int> outLabels)
        {
            var edgeCodes = new HashSet<int>(_edgeCodes);

            var targets = new HashSet<int>();
            foreach (var e in edgeCodes)
            {
                targets.Add(e / pts.Count);
                targets.Add(e % pts.Count);
            }

            while (targets.Count > 0)
            {
                HashSet<int> remove = new HashSet<int>();

                foreach (int i in targets.ToArray())
                {
                    var ls = edges[i];
                    bool found = false;

                    // 境界付近の点は近傍の境界外の点の色で塗り替える
                    foreach (int j in ls)
                    {
                        if (targets.Contains(j))
                            continue;
                        outLabels[i] = outLabels[j];
                        found = true;
                    }

                    // 塗り替えられてない場合は次回に持ち越し
                    if (!found)
                        continue;

                    remove.Add(i);
                }

                foreach (int i in remove)
                {
                    var ls = edges[i];
                    foreach (int j in ls)
                        edgeCodes.Remove(Math.Min(j, i) * pts.Count + Math.Max(j, i));
                    targets.Remove(i);
                }
            }
        }

        static bool cond(int i, int j, int n, HashSet<int> set)
        {
            if (i > j)
                FLib.FMath.Swap(ref i, ref j);

            if (!set.Contains(i * n + j))
                return true;

            return false;
        }

        //--------------------------------------------------------
        







        private static PatchSkeleton ToPatchSkeleton(Segment seg)
        {
            Dictionary<string, PatchSkeletonJoint> jointDict = new Dictionary<string, PatchSkeletonJoint>();
            PatchSkeleton skl = new PatchSkeleton();
            foreach (var j in seg.an.joints)
            {
                PointF pos = new PointF(j.position.X - seg.offset.X, j.position.Y - seg.offset.Y);
                jointDict[j.name] = new PatchSkeletonJoint(j.name, pos);
                skl.joints.Add(jointDict[j.name]);
            }
            foreach (var b in seg.an.bones)
                skl.bones.Add(new PatchSkeletonBone(jointDict[b.src.name], jointDict[b.dst.name]));
            return skl;
        }


        private static List<PatchSection> ToPatchSections(Segment seg, List<PointF> subdividedPath)
        {
            var shiftPath = FLib.FMath.ShiftPath(seg.path, -seg.offset.X, -seg.offset.Y);
            var _sections = FLib.FMath.SplitPathRange(FLib.FMath.ShiftPath(seg.section, -seg.offset.X, -seg.offset.Y), shiftPath, true);
            var sections = new List<CharacterRange>();
            foreach (var r in _sections)
            {
                int i1 = r.First;
                int i2 = r.First + r.Length - 1;
                try
                {
                    int j1 = subdividedPath.IndexOf(shiftPath[i1]);
                    int j2 = subdividedPath.IndexOf(shiftPath[i2]);
                    sections.Add(new CharacterRange(j1, j2 - j1 + 1));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e + e.StackTrace);
                }
            }
            return sections;
        }
    }
}