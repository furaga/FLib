using System;
using System.Linq;
using System.Collections.Generic;
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
        static VertexPositionColorTexture[] rawVertices = new[]
                    {
            new VertexPositionColorTexture(new Vector3(-1,-1,-1), Color.Red, new Vector2(0, 0)),
            new VertexPositionColorTexture(new Vector3(1,-1,-1), Color.Blue, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(-1,1,-1), Color.Yellow, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(-1,-1,1), Color.Green, new Vector2(1, 1)),
            new VertexPositionColorTexture(new Vector3(1,1,-1), Color.Purple, new Vector2(1, 1)),
            new VertexPositionColorTexture(new Vector3(1,-1,1), Color.Cyan, new Vector2(0, 1)),
            new VertexPositionColorTexture(new Vector3(-1,1,1), Color.White, new Vector2(1, 0)),
            new VertexPositionColorTexture(new Vector3(1,1,1), Color.LightGreen, new Vector2(0, 0)),
        };

        static int[] rawIndices= new[]
        {
            0, 2, 4,
            0, 4, 1,

            3, 7, 6,
            3, 5, 7,
            
            2, 6, 7,
            2, 7, 4,

            0, 5, 3,
            0, 1, 5,

            0, 3, 6,
            0, 6, 2,

            1, 7, 5,
            1, 4, 7,
        };

        [STAThread]
        private static void Main()
        {
            var form = new RenderForm("SharpDX - MiniCubeTexture Direct3D11 Sample");

            // Prepare matrices
            var view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, form.ClientSize.Width / (float)form.ClientSize.Height, 0.1f, 100.0f);
            var viewProj = Matrix.Multiply(view, proj);

            var info = SharpDXHelper.Initialize(form, rawVertices, rawIndices, new Matrix(), "test.png");

            var tex = SharpDXHelper.LoadTexture(info, "images.jpg");

            // Use clock
            var clock = new Stopwatch();
            clock.Start();

            SharpDXHelper.Run(form, () =>
            {
                var time = clock.ElapsedMilliseconds / 1000.0f;
                
                var worldViewProj = Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f) * viewProj;
                worldViewProj.Transpose();

                SharpDXHelper.BeginDraw(info);
                SharpDXHelper.UpdateCameraBuffer(info, worldViewProj);
                SharpDXHelper.UpdateVertexBuffer(info, rawVertices);
                if (clock.ElapsedMilliseconds >= 1000)
                {
                    SharpDXHelper.SwitchTexture(info, tex);
                    int cnt = (3 * (int)(clock.ElapsedMilliseconds / 40)) % rawIndices.Length;
                    if (cnt == 0)
                        cnt = 3;
                    SharpDXHelper.UpdateIndexBuffer(info, rawIndices.Take(cnt));
                    //Math.Min(rawIndices.Length, 3 * (clock.ElapsedMilliseconds / 500))).ToList());
                }
                SharpDXHelper.Draw(info, PrimitiveTopology.TriangleList);
                SharpDXHelper.EndDraw(info);
            });

            info.Dispose();
        }
    }
}
