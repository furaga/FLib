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
    /// <summary>
    /// SharpDX MiniCubeTexture Direct3D 11 Sample
    /// </summary>
    public class SharpDXHelper
    {
        enum BufferType
        {
            Vertex, Index, Camera
        }


        const string DefaultShaderPath = "defaultShader.fx";

        public static SharpDXInfo Initialize(Form form, IEnumerable<VertexPositionColorTexture> rawVertices, IEnumerable<int> rawIndices, Matrix viewWorldProj, string texturePath)
        {
            if (form == null)
                return null;
            return Initialize(form.Handle, form.ClientSize, rawVertices, rawIndices, viewWorldProj, texturePath);
        }

        public static SharpDXInfo Initialize(IntPtr handle, System.Drawing.Size size, IEnumerable<VertexPositionColorTexture> rawVertices, IEnumerable<int> rawIndices, Matrix viewWorldProj, string texturePath)
        {
            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(size.Width, size.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = handle,
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
            factory.MakeWindowAssociation(handle, WindowAssociationFlags.IgnoreAll);

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

                // Instantiate buffers
                var vertexBuffer = CreateBuffer<VertexPositionColorTexture>(device, BindFlags.VertexBuffer, rawVertices.Count());
                var indexBuffer = CreateBuffer<int>(device, BindFlags.IndexBuffer, rawIndices.Count());
                var cameraBuffer = CreateConstantBuffer<Matrix>(device);

                var depthBuffer = new Texture2D(device, new Texture2DDescription()
                {
                    Format = Format.D32_Float_S8X24_UInt,
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = size.Width,
                    Height = size.Height,
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

                // initialize sharpdxlib context
                var info = new SharpDXInfo(
                    device, swapChain, handle, size,
                    depthView, renderView,
                    vertexBuffer, indexBuffer, cameraBuffer, depthBuffer,
                    rawVertices, rawIndices,
                    texture, textureView,
                    vertexShader, pixelShader, layout,
                    backBuffer, factory);

                // Prepare All the stages
                context.InputAssembler.InputLayout = layout;
                context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<VertexPositionColorTexture>(), 0));
                context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
                context.VertexShader.SetConstantBuffer(0, cameraBuffer);
                context.VertexShader.Set(vertexShader); RasterizerStateDescription rasdesc = new RasterizerStateDescription()
                {
                    CullMode = CullMode.None,
                    FillMode = FillMode.Solid,
                    IsFrontCounterClockwise = true,
                    DepthBias = 0,
                    DepthBiasClamp = 0,
                    SlopeScaledDepthBias = 0,
                    IsDepthClipEnabled = true,
                    IsMultisampleEnabled = true,
                };
                context.Rasterizer.State = new RasterizerState(device, rasdesc);
                context.Rasterizer.SetViewport(new Viewport(0, 0, size.Width, size.Height, 0.0f, 1));
                context.PixelShader.Set(pixelShader);
                context.PixelShader.SetSampler(0, sampler);
                context.PixelShader.SetShaderResource(0, textureView);
                context.OutputMerger.SetTargets(depthView, renderView);

                UpdateVertexBuffer(info, rawVertices);
                UpdateIndexBuffer(info, rawIndices);
                UpdateCameraBuffer(info, viewWorldProj);

                return info;
            }
        }
        
        public static void Run(Form form, RenderLoop.RenderCallback mainLoop)
        {
            RenderLoop.Run(form, mainLoop);
        }

        public static void BeginDraw(SharpDXInfo info)
        {
            info.Device.ImmediateContext.ClearDepthStencilView(info.DepthView, DepthStencilClearFlags.Depth, 1, 0);
            info.Device.ImmediateContext.ClearRenderTargetView(info.RenderView, Color.Black);
        }

        public static void DrawMesh(SharpDXInfo info)
        {
            info.Device.ImmediateContext.DrawIndexed(info.rawIndices.Length, 0, 0);
        }

        public static void EndDraw(SharpDXInfo info)
        {
            info.SwapChain.Present(0, PresentFlags.None);
        }

        //----------------------------------------------------------------------------

        static Buffer CreateBuffer<T>(Device device, BindFlags bindFlag, int bufferLength)
            where T : struct
        {
            // bufferLength <= 0 ならBuffer.Create<>()でエラーが出るので、ダミーで、長さ1の空のバッファを作成する
            if (bufferLength <= 0)
                bufferLength = 1;

            Buffer buffer = Buffer.Create<T>(
                device,
                bindFlag,
                new T[bufferLength],
                usage: ResourceUsage.Dynamic,
                accessFlags: CpuAccessFlags.Write,
                optionFlags: ResourceOptionFlags.None
            );
            return buffer;
        }

        static Buffer CreateConstantBuffer<T>(Device device)
            where T : struct
        {
            Buffer buffer = new Buffer(
                     device,
                     Utilities.SizeOf<T>(),
                     ResourceUsage.Default,
                     BindFlags.ConstantBuffer,
                     CpuAccessFlags.None,
                     ResourceOptionFlags.None, 0);
            return buffer;
        }

        //----------------------------------------------------------------------------

        public static void UpdateVertexBuffer(SharpDXInfo info, IEnumerable<VertexPositionColorTexture> rawVertices)
        {
            if (rawVertices == null)
                return;

            // 頂点数が既存のデータと違ってたら頂点バッファを作り直す
            if (rawVertices.Count() != info.rawVertices.Length)
            {
                info.VertexBuffer.Dispose(); 
                var buffer = CreateBuffer<VertexPositionColorTexture>(info.Device, BindFlags.VertexBuffer, rawVertices.Count());
                info.UpdateBuffers(vertexBuffer : buffer);
                var binding = new VertexBufferBinding(info.VertexBuffer, Utilities.SizeOf<VertexPositionColorTexture>(), 0);
                info.Device.ImmediateContext.InputAssembler.SetVertexBuffers(0, binding);
                info.rawVertices = rawVertices.ToArray();
            }

            // 頂点バッファの値を更新
            var box = info.Device.ImmediateContext.MapSubresource(info.VertexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            for (int i = 0; i < rawVertices.Count(); i++)
            {
                System.Runtime.InteropServices.Marshal.StructureToPtr(rawVertices.ElementAt(i), box.DataPointer + Utilities.SizeOf<VertexPositionColorTexture>() * i, false);
                info.rawVertices[i] = rawVertices.ElementAt(i);
            }
            info.Device.ImmediateContext.UnmapSubresource(info.VertexBuffer, 0);
        }

        public static void UpdateIndexBuffer(SharpDXInfo info, IEnumerable<int> rawIndices)
        {
            if (rawIndices == null)
                return;

            // 頂点数が既存のデータと違ってたらインデックスバッファを作り直す
            if (rawIndices.Count() != info.rawIndices.Length)
            {
                info.IndexBuffer.Dispose(); 
                var buffer = CreateBuffer<int>(info.Device, BindFlags.IndexBuffer, rawIndices.Count());
                info.UpdateBuffers(indexBuffer: buffer);
                var binding = new VertexBufferBinding(info.IndexBuffer, Utilities.SizeOf<int>(), 0);
                info.Device.ImmediateContext.InputAssembler.SetIndexBuffer(buffer, Format.R32_UInt, 0);
                info.rawIndices = rawIndices.ToArray();
            }

            // インデックスバッファの値を更新
            var box = info.Device.ImmediateContext.MapSubresource(info.IndexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            for (int i = 0; i < rawIndices.Count(); i++)
            {
                System.Runtime.InteropServices.Marshal.StructureToPtr(rawIndices.ElementAt(i), box.DataPointer + Utilities.SizeOf<int>() * i, false);
                info.rawIndices[i] = rawIndices.ElementAt(i);
            }
            info.Device.ImmediateContext.UnmapSubresource(info.IndexBuffer, 0);
        }

        public static void UpdateCameraBuffer(SharpDXInfo info, Matrix worldViewProj)
        {
            info.Device.ImmediateContext.UpdateSubresource(ref worldViewProj, info.CameraBuffer);
        }

        //----------------------------------------------------------------------------

        public static Texture2D LoadTexture(SharpDXInfo info, string texturePath)
        {
            if (!System.IO.File.Exists(texturePath))
                return null;
            var texture = Texture2D.FromFile<Texture2D>(info.Device, texturePath);
            return texture;
        }

        /// <summary>
        /// bmpを一時ファイルに書き出してTexture2D.FromFile()で読み込みなおしている。非常に遅いので注意
        /// </summary>
        /// <param name="info"></param>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static Texture2D BitmapToTexture(SharpDXInfo info, System.Drawing.Bitmap bmp)
        {
            if (bmp == null)
                return null;
            const string tmpFile = "_tmp.png";
            bmp.Save(tmpFile);
            var texture = Texture2D.FromFile<Texture2D>(info.Device, tmpFile);
            return texture;
        }

        /// <summary>
        /// メッシュの描画に使うテクスチャを設定する。
        /// 新しいテクスチャが指定されたら、現在のテクスチャリソースを破棄して作りなおす
        /// 従って、テクスチャの切り替えが少ないほど描画が高速になる
        /// </summary>
        public static void SwitchTexture(SharpDXInfo info, Texture2D texture)
        {
            if (texture == info.Texture)
                return;
            info.TextureView.Dispose();
            ShaderResourceView textureView = null;
            if (texture != null)
                textureView = new ShaderResourceView(info.Device, texture);
            info.UpdateTexture(texture, textureView);
            info.Device.ImmediateContext.PixelShader.SetShaderResource(0, info.TextureView);
        }
    }
}