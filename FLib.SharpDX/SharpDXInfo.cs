using System;
using System.Linq;
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
        internal Form Form { get; private set; }
        internal DepthStencilView DepthView { get; private set; }
        internal RenderTargetView RenderView { get; private set; }

        internal Buffer VertexBuffer { get; private set; }
        internal Buffer CameraBuffer { get; private set; }
        internal Texture2D DepthBuffer { get; private set; }
        internal Texture2D Texture { get; set; }

        internal VertexPositionColorTexture[] rawVertices;

        internal VertexShader VertexShader { get; private set; }
        internal PixelShader PixelShader { get; private set; }
        internal InputLayout InputLayout { get; private set; }
        internal Texture2D BackBuffer { get; private set; }
        internal Factory Factory { get; private set; }        

        internal SharpDXInfo(Device dev, SwapChain sc, Form f, DepthStencilView dv, RenderTargetView rt, Buffer vb, Buffer cb, Texture2D db, VertexPositionColorTexture[] rawVertices, Texture2D tex,VertexShader vertexShader,PixelShader pixelShader,InputLayout layout,Texture2D backBuffer,Factory factory)
        {
            Device = dev;
            SwapChain = sc;
            Form  = f;
            DepthView = dv;
            RenderView  = rt;
            VertexBuffer = vb;
            CameraBuffer = cb;
            DepthBuffer = db;
            this.rawVertices = rawVertices;

            Texture = tex;            
            
            VertexShader = vertexShader;
            PixelShader = pixelShader;
            InputLayout = layout;
            BackBuffer = backBuffer;
            Factory = factory;
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
