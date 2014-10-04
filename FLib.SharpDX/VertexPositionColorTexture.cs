using SharpDX;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace FLib.SharpDX
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionColorTexture
    {
        public Vector4 Position;
        public Vector4 Color;
        public Vector2 TextureCoordinate;
        public VertexPositionColorTexture(Vector3 position, Color color, Vector2 coordinate)
        {
            Color = color.ToVector4();
            Position = new Vector4(position, 1);
            TextureCoordinate = coordinate;
        }
    }
}