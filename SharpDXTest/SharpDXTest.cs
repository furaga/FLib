using System;
using System.Diagnostics;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using FLib.SharpDX;
namespace MiniCubeTexure
{
    internal static class Program
    {
        static VertexPositionColorTexture[] raw_vertices = new[]
        {
            new VertexPositionColorTexture(new Vector3(-1,-1,-1), Color.Red, new Vector2(0, 1)),
            new VertexPositionColorTexture(new Vector3(-1,1,-1), Color.Red, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(1,1,-1), Color.Red, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(-1,-1,-1), Color.Red, new Vector2(0, 1)),
            new VertexPositionColorTexture(new Vector3(1,1,-1), Color.Red, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1,-1,-1), Color.Red, new Vector2(1, 1)),

            new VertexPositionColorTexture(new Vector3(-1,-1,1), Color.Blue, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1,1,1), Color.Blue, new Vector2(0, 1)),
            new VertexPositionColorTexture(new Vector3(-1,1,1), Color.Blue, new Vector2(1, 1)),
            new VertexPositionColorTexture(new Vector3(-1,-1,1), Color.Blue, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1,-1,1), Color.Blue, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(1,1,1), Color.Blue, new Vector2(0, 1)),

            new VertexPositionColorTexture(new Vector3(-1,1,-1), Color.Yellow, new Vector2(0, 1)),
            new VertexPositionColorTexture(new Vector3(-1,1,1), Color.Yellow, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(1,1,1), Color.Yellow, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(-1,1,-1), Color.Yellow, new Vector2(0, 1)),
            new VertexPositionColorTexture(new Vector3(1,1,1), Color.Yellow, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1,1,-1), Color.Yellow, new Vector2(1, 1)),

            new VertexPositionColorTexture(new Vector3(-1,-1,-1), Color.Green, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1,-1,1), Color.Green, new Vector2(0, 1)),
            new VertexPositionColorTexture(new Vector3(-1,-1,1), Color.Green, new Vector2(1, 1)),
            new VertexPositionColorTexture(new Vector3(-1,-1,-1), Color.Green, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1,-1,-1), Color.Green, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(1,-1,1), Color.Green, new Vector2(0, 1)),

            new VertexPositionColorTexture(new Vector3(-1,-1,-1), Color.Red, new Vector2(0, 1)),
            new VertexPositionColorTexture(new Vector3(-1,-1,1), Color.Red, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(-1,1,1), Color.Red, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(-1,-1,-1), Color.Red, new Vector2(0, 1)),
            new VertexPositionColorTexture(new Vector3(-1,1,1), Color.Red, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(-1,1,-1), Color.Red, new Vector2(1, 1)),

            new VertexPositionColorTexture(new Vector3(1,-1,-1), Color.Red, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1,1,1), Color.Red, new Vector2(0, 1)),
            new VertexPositionColorTexture(new Vector3(1,-1,1), Color.Red, new Vector2(1, 1)),
            new VertexPositionColorTexture(new Vector3(1,-1,-1), Color.Red, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1,1,-1), Color.Red, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(1,1,1), Color.Red, new Vector2(0, 1)),
        };

        [STAThread]
        private static void Main()
        {
            var form = new RenderForm("SharpDX - MiniCubeTexture Direct3D11 Sample");
            var info = SharpDXHelper.Initialize(form, raw_vertices, "test.png");

            // Prepare matrices
            var view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, form.ClientSize.Width / (float)form.ClientSize.Height, 0.1f, 100.0f);
            var viewProj = Matrix.Multiply(view, proj);

            // Use clock
            var clock = new Stopwatch();
            clock.Start();

            SharpDXHelper.Run(info, () =>
            {
                var time = clock.ElapsedMilliseconds / 1000.0f;

                for (int i = 0; i < raw_vertices.Length; i++)
                    raw_vertices[i].Position.X += 0.0001f;

                var worldViewProj = Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f) * viewProj;
                worldViewProj.Transpose();

                SharpDXHelper.BeginDraw(info);
                SharpDXHelper.UpdateCamera(info, worldViewProj);
                SharpDXHelper.UpdateVertices(info, raw_vertices);
                SharpDXHelper.DrawMesh(info);
                SharpDXHelper.EndDraw(info);
            });

            info.Dispose();
        }
    }
}
