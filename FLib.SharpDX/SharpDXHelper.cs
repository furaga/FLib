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
    /// <summary>
    /// SharpDX MiniCubeTexture Direct3D 11 Sample
    /// </summary>
    public class SharpDXHelper
    {
        const string DefaultShaderPath = "defaultShader.fx";

        public static SharpDXInfo Initialize(Form form, VertexPositionColorTexture[] rawVertices, string texturePath)
        {
            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(form.ClientSize.Width, form.ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            // Create Device and SwapChain
            Device device;
            SwapChain swapChain;
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, desc, out device, out swapChain);
            var context = device.ImmediateContext;

            // Ignore all windows events
            var factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            // New RenderTargetView from the backbuffer
            var backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            var renderView = new RenderTargetView(device, backBuffer);

            // Compile Vertex and Pixel shaders
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile(DefaultShaderPath, "VS", "vs_4_0"))
            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile(DefaultShaderPath, "PS", "ps_4_0"))
            {
                var vertexShader = new VertexShader(device, vertexShaderByteCode);
                var pixelShader = new PixelShader(device, pixelShaderByteCode);

                // Layout from VertexShader input signature
                var layout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                        new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 32, 0),
                    });

                // Instantiate Vertex buiffer from vertex data
                var vertexBuffer = Buffer.Create(
                    device,
                    BindFlags.VertexBuffer,
                    rawVertices,
                    usage: ResourceUsage.Dynamic,
                    accessFlags: CpuAccessFlags.Write,
                    optionFlags: ResourceOptionFlags.None
                );

                // Create Constant Buffer
                var cameraBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

                // Create Depth Buffer & View
                var depthBuffer = new Texture2D(device, new Texture2DDescription()
                {
                    Format = Format.D32_Float_S8X24_UInt,
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = form.ClientSize.Width,
                    Height = form.ClientSize.Height,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                });

                var depthView = new DepthStencilView(device, depthBuffer);

                // Load texture and create sampler
                var texture = Texture2D.FromFile<Texture2D>(device, texturePath);
                var textureView = new ShaderResourceView(device, texture);

                var sampler = new SamplerState(device, new SamplerStateDescription()
                {
                    Filter = Filter.MinMagMipLinear,
                    AddressU = TextureAddressMode.Wrap,
                    AddressV = TextureAddressMode.Wrap,
                    AddressW = TextureAddressMode.Wrap,
                    BorderColor = Color.Black,
                    ComparisonFunction = Comparison.Never,
                    MaximumAnisotropy = 16,
                    MipLodBias = 0,
                    MinimumLod = 0,
                    MaximumLod = 16,
                });

                // Prepare All the stages
                context.InputAssembler.InputLayout = layout;
                context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<VertexPositionColorTexture>(), 0));
                context.VertexShader.SetConstantBuffer(0, cameraBuffer);
                context.VertexShader.Set(vertexShader);
                context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
                context.PixelShader.Set(pixelShader);
                context.PixelShader.SetSampler(0, sampler);
                context.PixelShader.SetShaderResource(0, textureView);
                context.OutputMerger.SetTargets(depthView, renderView);

                return new SharpDXInfo(device, swapChain, form, depthView, renderView, vertexBuffer, cameraBuffer, depthBuffer, rawVertices, texture,
                    vertexShader, pixelShader, layout, backBuffer, factory);
            }
        }

        public static void Run(SharpDXInfo info, RenderLoop.RenderCallback mainLoop)
        {
            RenderLoop.Run(info.Form, mainLoop);
        }

        public static void BeginDraw(SharpDXInfo info)
        {
            info.Device.ImmediateContext.ClearDepthStencilView(info.DepthView, DepthStencilClearFlags.Depth, 1, 0);
            info.Device.ImmediateContext.ClearRenderTargetView(info.RenderView, Color.Black);
        }

        public static void UpdateVertices(SharpDXInfo info, VertexPositionColorTexture[] rawVertices)
        {
            if (rawVertices == null)
                return;
            var box = info.Device.ImmediateContext.MapSubresource(info.VertexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            for (int i = 0; i < info.rawVertices.Length; i++)
                System.Runtime.InteropServices.Marshal.StructureToPtr(rawVertices[i], box.DataPointer + Utilities.SizeOf<VertexPositionColorTexture>() * i, false);
            info.Device.ImmediateContext.UnmapSubresource(info.VertexBuffer, 0);
        }

        public static void DrawMesh(SharpDXInfo info)
        {
            info.Device.ImmediateContext.Draw(info.rawVertices.Length, 0);
        }

        public static void EndDraw(SharpDXInfo info)
        {
            info.SwapChain.Present(0, PresentFlags.None);
        }

        public static void UpdateCamera(SharpDXInfo info, Matrix worldViewProj)
        {
            info.Device.ImmediateContext.UpdateSubresource(ref worldViewProj, info.CameraBuffer);
        }
    }
}