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
    public class PatchControlPoint
    {
        public PointF position;
        public PointF orgPosition;
        public readonly int part;

        public PatchControlPoint(PointF orgPoint, int part)
        {
            this.position = this.orgPosition = orgPoint;
            this.part = part;
        }

        public PatchControlPoint(PatchControlPoint c)
        {
            this.position = c.position;
            this.orgPosition = c.orgPosition;
            this.part = c.part;
        }


        internal void ScaleByRatio(float rx, float ry)
        {
            position = new PointF(position.X * rx, position.Y * ry);
            orgPosition = new PointF(orgPosition.X * rx, orgPosition.Y * ry);
        }
    }
}
