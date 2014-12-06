using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Magic2D
{
    /// <summary>
    /// Magic2Dのプロジェクトフォルダを読み込んで、セグメントのリストを取得する
    /// </summary>
    class SegmentLoader
    {
        internal static Dictionary<string, Segment> LoadSegments(string root, string dirName)
        {
            using (Segmentation segmentation = OpenSegmentation(root, dirName))
            {
                Dictionary<string, Segment> segmentDict = CopySegments(segmentation);
                // 元画像（画像名.Full）は削除
                while (true)
                {
                    if (!segmentDict.Any(kv => kv.Key.EndsWith(".Full")))
                        break;
                    var _kv = segmentDict.First(kv => kv.Key.EndsWith(".Full"));
                    segmentDict[_kv.Key].Dispose();
                    segmentDict.Remove(_kv.Key);
                }
                return segmentDict;
            }
        }

        // segmentationに含まれるsegmentをすべてコピーする
        static Dictionary<string, Segment> CopySegments(Segmentation segmentation)
        {
            Dictionary<string, Segment> segmentDict = new Dictionary<string, Segment>();
            foreach (var kv in segmentation.segmentRootDict)
                foreach (var seg in kv.Value.segments)
                    if (seg.bmp != null)
                    {
                        string newKey = kv.Key + "." + seg.name;
                        segmentDict[newKey] = new Segment(seg, newKey);
                    }
            return segmentDict;
        }

        // segmentationをロードする
        static Segmentation OpenSegmentation(string root, string dirName)
        {
            Segmentation segmentation = new Segmentation();
            if (segmentation == null)
            {
                segmentation.Dispose();
                return null;
            }

            segmentation.Clear();

            string dir = Path.Combine(root, dirName);
            if (!Directory.Exists(dir))
            {
                segmentation.Dispose();
                return null;
            }

            // パラメータの読み込み
            string f = Path.Combine(dir, "segmentation.seg");
            if (!File.Exists(f))
            {
                segmentation.Dispose();
                return null;
            }

            string[] lines = File.ReadAllLines(f);
            SegmentRoot sroot = null;
            Segment seg = null;
            foreach (var line in lines)
            {
                if (line.StartsWith("SegmentRoot:"))
                {
                    string key = line.Substring("SegmentRoot:".Length).Trim();
                    segmentation.segmentRootDict[key] = new SegmentRoot(null, null);
                    sroot = segmentation.segmentRootDict[key];
                    sroot.segments.Clear();
                }
                if (root == null)
                    continue;
                if (line.StartsWith("PathSegment:"))
                {
                    string name = line.Substring("PathSegment:".Length).Trim();
                    seg = new PathSegment(name, sroot);
                    sroot.segments.Add(seg);
                }
                if (seg == null)
                    continue;
                if (line.StartsWith("closed:"))
                {
                    string closedText = line.Substring("closed:".Length).Trim();
                    bool closed;
                    if (bool.TryParse(closedText, out closed))
                        seg._SetClosed(closed);
                }
                if (line.StartsWith("offset:"))
                {
                    string offsetText = line.Substring("offset:".Length).Trim();
                    string[] tokens = offsetText.Split(',');
                    if (tokens.Length != 2)
                        continue;
                    int x, y;
                    if (int.TryParse(tokens[0], out x) && int.TryParse(tokens[1], out y))
                        seg.offset = new Point(x, y);
                }
                if (line.StartsWith("path:"))
                {
                    string pathText = line.Substring("path:".Length).Trim();
                    seg.path = StringToPointList(pathText);
                }
                if (line.StartsWith("section:"))
                {
                    string sectionText = line.Substring("section:".Length).Trim();
                    seg.section = StringToPointList(sectionText);
                }
                if (line.StartsWith("partingLine:"))
                {
                    string partingText = line.Substring("partingLine:".Length).Trim();
                    seg.partingLine = StringToPointList(partingText);
                }
            }

            // 画像の読み込み
            foreach (var kv in segmentation.segmentRootDict)
            {
                var bmpDict = new Dictionary<string, Bitmap>();
                OpenImages(dir, kv.Key + "_bmp", bmpDict);

                if (bmpDict.ContainsKey("_root_"))
                    kv.Value.bmp = bmpDict["_root_"];

                foreach (var sg in kv.Value.segments)
                {
                    if (bmpDict.ContainsKey(sg.name))
                        sg.bmp = bmpDict[sg.name];
                }
            }

            // スケルトンの読み込み
            foreach (var kv in segmentation.segmentRootDict)
            {
                var anDict = new Dictionary<string, SkeletonAnnotation>();
                OpenAnnotations(dir, kv.Key + "_skeleton", anDict);

                if (anDict.ContainsKey("_root_"))
                    kv.Value.an = anDict["_root_"];

                foreach (var sg in kv.Value.segments)
                {
                    if (anDict.ContainsKey(sg.name))
                        sg.an = anDict[sg.name];
                }
            }

            return segmentation;
        }

        static void OpenImages(string root, string dirName, Dictionary<string, Bitmap> imageDict)
        {
            imageDict.Clear();
            string dir = Path.Combine(root, dirName);
            if (Directory.Exists(dir))
            {
                foreach (var f in Directory.GetFiles(dir))
                {
                    string key = Path.GetFileNameWithoutExtension(f);
                    using (var _bmp = new Bitmap(f))
                        AssignImage(imageDict, key, new Bitmap(_bmp));
                }
            }
        }

        static void AssignImage(Dictionary<string, Bitmap> imageDict, string key, Bitmap bmp)
        {
            DeleteImage(imageDict, key);
            imageDict[key] = bmp;
        }

        static void DeleteImage(Dictionary<string, Bitmap> imageDict, string key)
        {
            if (imageDict.ContainsKey(key))
            {
                imageDict[key].Dispose();
                imageDict.Remove(key);
            }
        }

        static void OpenAnnotations(string root, string dirName, Dictionary<string, SkeletonAnnotation> anDict)
        {
            foreach (var an in anDict)
                an.Value.bmp.Dispose();

            anDict.Clear();

            string dir = Path.Combine(root, dirName);
            if (!Directory.Exists(dir))
                return;

            string p = Path.Combine(dir, "skeletonAnnotation.ska");
            if (!File.Exists(p))
                return;

            var lines = File.ReadAllLines(p);

            SkeletonAnnotation ann = null;

            foreach (var line in lines)
            {
                if (line.StartsWith("SkeletonAnnotation"))
                {
                    string key = line.Split('[').Last().Trim('[', ']');
                    anDict[key] = ann = new SkeletonAnnotation(null);
                }
                if (line.StartsWith("JointAnnotation"))
                {
                    if (ann == null)
                        continue;
                    string[] tokens = line.Split(',');
                    if (tokens.Length != 4)
                        continue;
                    string name = tokens[1];
                    float x, y;
                    if (!float.TryParse(tokens[2], out x) || !float.TryParse(tokens[3], out y))
                        continue;
                    ann.joints.Add(new JointAnnotation(name, new PointF(x, y)));
                }
                if (line.StartsWith("BoneAnnotation"))
                {
                    if (ann == null)
                        continue;
                    string[] tokens = line.Split(',');
                    if (tokens.Length != 3)
                        continue;
                    int srcIdx, dstIdx;
                    if (!int.TryParse(tokens[1], out srcIdx) || !int.TryParse(tokens[2], out dstIdx))
                        continue;
                    if (srcIdx < 0 && ann.joints.Count <= srcIdx)
                        continue;
                    if (dstIdx < 0 && ann.joints.Count <= dstIdx)
                        continue;
                    ann.bones.Add(new BoneAnnotation(ann.joints[srcIdx], ann.joints[dstIdx]));
                }
            }

            foreach (var kv in anDict)
            {
                string f = Path.Combine(dir, kv.Key + ".png");
                if (!File.Exists(f))
                    continue;
                using (var _bmp = new Bitmap(f))
                    anDict[kv.Key].bmp = new Bitmap(_bmp);
            }
        }

        static List<PointF> StringToPointList(string text)
        {
            var ls = new List<PointF>();

            string[] tokens = text.Split(' ');
            for (int i = 0; i < tokens.Length; i++)
            {
                string[] ptTokens = tokens[i].Split(',');
                if (ptTokens.Length != 2)
                    return ls;
                float x, y;
                if (float.TryParse(ptTokens[0], out x) && float.TryParse(ptTokens[1], out y))
                    ls.Add(new PointF(x, y));
            }

            return ls;
        }
    }
}