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
    public class PatchVertex
    {
        public PointF position;
        public PointF orgPosition;
        public int part;
        // 各頂点に割り当てるテクスチャ座標
        // 複数の画像を重ねて描く（ひとつの頂点に複数のテクスチャ座標を持つ）場合があるので、辞書として記録する
        Dictionary<string, PointF> texcoordDict = new Dictionary<string,PointF>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orgPoint"></param>
        /// <param name="part"></param>
        /// <param name="patchKey">texcoordに対応するキー。"patch1", "patch2"など？</param>
        /// <param name="texcoord">元画像におけるこの頂点の位置[0,1]。普通はorgPointと同じ値になる</param>
        public PatchVertex(PointF orgPoint, int part, string patchKey, PointF texcoord)
        {
            this.position = this.orgPosition = orgPoint;
            this.part = part;
            this.texcoordDict[patchKey] = texcoord;
        }

        public PatchVertex(PointF orgPoint, int part, Dictionary<string, PointF> texcoordDict)
        {
            this.position = this.orgPosition = orgPoint;
            this.part = part;
            this.texcoordDict= new Dictionary<string,PointF>(texcoordDict);
        }

        public PatchVertex(PatchVertex v)
        {
            this.position = v.position;
            this.orgPosition = v.orgPosition;
            this.part = v.part;
            this.texcoordDict = new Dictionary<string, PointF>(v.texcoordDict);
        }

        public PointF GetTexcoord(string patchKey)
        {
            if (texcoordDict.ContainsKey(patchKey))
                return texcoordDict[patchKey];
            return new PointF();
        }

        public Dictionary<string, PointF> CopyTexcoordDict()
        {
            return new Dictionary<string, PointF>(texcoordDict);
        }

        internal void ScaleByRatio(float rx, float ry)
        {
            position = new PointF(position.X * rx, position.Y * ry);
            orgPosition = new PointF(orgPosition.X * rx, orgPosition.Y * ry);
        }
    }
}
