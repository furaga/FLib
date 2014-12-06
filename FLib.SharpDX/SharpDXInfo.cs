using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
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

namespace FLib.SharpDX
{
    public class SharpDXInfo : IDisposable
    {
        internal Device Device { get; private set; }
        internal SwapChain SwapChain { get; private set; }
        internal IntPtr Handle { get; private set; }
        internal System.Drawing.Size Size { get; private set; }
        internal DepthStencilView DepthView { get; private set; }
        internal RenderTargetView RenderView { get; private set; }

        internal Buffer VertexBuffer { get; private set; }
        internal Buffer IndexBuffer { get; private set; }
        internal Buffer CameraBuffer { get; private set; }
        internal Texture2D DepthBuffer { get; private set; }
        internal Texture2D Texture { get; set; }
        internal ShaderResourceView TextureView { get; set; }

        internal VertexPositionColorTexture[] rawVertices;
        internal int[] rawIndices;

        internal VertexShader VertexShader { get; private set; }
        internal PixelShader PixelShader { get; private set; }
        internal InputLayout InputLayout { get; private set; }
        internal Texture2D BackBuffer { get; private set; }
        internal Factory Factory { get; private set; }        

        internal SharpDXInfo(Device dev, SwapChain sc, IntPtr h, System.Drawing.Size sz, 
            DepthStencilView dv, RenderTargetView rt, 
            Buffer vb, Buffer ib, Buffer cb, 
            Texture2D db,
            IEnumerable<VertexPositionColorTexture> rawVertices, IEnumerable<int> rawIndices,
            Texture2D tex, ShaderResourceView texView, 
            VertexShader vertexShader, PixelShader pixelShader, InputLayout layout,
            Texture2D backBuffer,Factory factory)
        {
            Device = dev;
            SwapChain = sc;
            Handle = h;
            Size = sz;
            DepthView = dv;
            RenderView = rt;
            VertexBuffer = vb;
            IndexBuffer = ib;
            CameraBuffer = cb;
            DepthBuffer = db;
            this.rawVertices = rawVertices.ToArray();
            this.rawIndices = rawIndices.ToArray();

            Texture = tex;
            TextureView = texView;
            
            VertexShader = vertexShader;
            PixelShader = pixelShader;
            InputLayout = layout;
            BackBuffer = backBuffer;
            Factory = factory;
        }

        /// <summary>
        /// ここではbufferの破棄（Dispose()）は実行しない。SharpDXHelperなどでbufferをキャッシュする実装があり得るため。
        /// </summary>
        internal void UpdateBuffers(Buffer vertexBuffer = null, Buffer indexBuffer = null, Buffer cameraBuffer = null)
        {
            if (vertexBuffer != null && this.VertexBuffer != vertexBuffer)
            {
//                this.VertexBuffer.Dispose();
                this.VertexBuffer = vertexBuffer;
            }
            if (indexBuffer != null && this.IndexBuffer != indexBuffer)
            {
//                this.IndexBuffer.Dispose();
                this.IndexBuffer = indexBuffer;
            }
            if (cameraBuffer != null && this.CameraBuffer != cameraBuffer)
            {
 //               this.CameraBuffer.Dispose();
                this.CameraBuffer = cameraBuffer;
            }
        }

        internal void UpdateTexture(Texture2D texture, ShaderResourceView textureView)
        {
            this.Texture = texture;
            this.TextureView = textureView;
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            RenderView.Dispose();
            Device.ImmediateContext.ClearState();
            Device.ImmediateContext.Flush();
            Device.ImmediateContext.Dispose();
            Device.Dispose();
            SwapChain.Dispose();
            VertexShader.Dispose();
            PixelShader.Dispose();
            InputLayout.Dispose();
            BackBuffer.Dispose();
            Factory.Dispose();
        }

    }
}
