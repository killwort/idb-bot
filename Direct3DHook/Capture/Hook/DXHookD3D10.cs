﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Capture.Interface;
using SharpDX;
using SharpDX.Direct3D10;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D10.Device; //using SlimDX.DXGI;
using Rectangle = System.Drawing.Rectangle;
using Resource = SharpDX.Direct3D10.Resource;
//using SlimDX.Direct3D10;
//using SlimDX;
//using Device = SlimDX.Direct3D10.Device;

namespace Capture.Hook {
    internal enum D3D10DeviceVTbl : short {
        // IUnknown
        QueryInterface = 0,
        AddRef = 1,
        Release = 2,

        // ID3D10Device
        VSSetConstantBuffers = 3,
        PSSetShaderResources = 4,
        PSSetShader = 5,
        PSSetSamplers = 6,
        VSSetShader = 7,
        DrawIndexed = 8,
        Draw = 9,
        PSSetConstantBuffers = 10,
        IASetInputLayout = 11,
        IASetVertexBuffers = 12,
        IASetIndexBuffer = 13,
        DrawIndexedInstanced = 14,
        DrawInstanced = 15,
        GSSetConstantBuffers = 16,
        GSSetShader = 17,
        IASetPrimitiveTopology = 18,
        VSSetShaderResources = 19,
        VSSetSamplers = 20,
        SetPredication = 21,
        GSSetShaderResources = 22,
        GSSetSamplers = 23,
        OMSetRenderTargets = 24,
        OMSetBlendState = 25,
        OMSetDepthStencilState = 26,
        SOSetTargets = 27,
        DrawAuto = 28,
        RSSetState = 29,
        RSSetViewports = 30,
        RSSetScissorRects = 31,
        CopySubresourceRegion = 32,
        CopyResource = 33,
        UpdateSubresource = 34,
        ClearRenderTargetView = 35,
        ClearDepthStencilView = 36,
        GenerateMips = 37,
        ResolveSubresource = 38,
        VSGetConstantBuffers = 39,
        PSGetShaderResources = 40,
        PSGetShader = 41,
        PSGetSamplers = 42,
        VSGetShader = 43,
        PSGetConstantBuffers = 44,
        IAGetInputLayout = 45,
        IAGetVertexBuffers = 46,
        IAGetIndexBuffer = 47,
        GSGetConstantBuffers = 48,
        GSGetShader = 49,
        IAGetPrimitiveTopology = 50,
        VSGetShaderResources = 51,
        VSGetSamplers = 52,
        GetPredication = 53,
        GSGetShaderResources = 54,
        GSGetSamplers = 55,
        OMGetRenderTargets = 56,
        OMGetBlendState = 57,
        OMGetDepthStencilState = 58,
        SOGetTargets = 59,
        RSGetState = 60,
        RSGetViewports = 61,
        RSGetScissorRects = 62,
        GetDeviceRemovedReason = 63,
        SetExceptionMode = 64,
        GetExceptionMode = 65,
        GetPrivateData = 66,
        SetPrivateData = 67,
        SetPrivateDataInterface = 68,
        ClearState = 69,
        Flush = 70,
        CreateBuffer = 71,
        CreateTexture1D = 72,
        CreateTexture2D = 73,
        CreateTexture3D = 74,
        CreateShaderResourceView = 75,
        CreateRenderTargetView = 76,
        CreateDepthStencilView = 77,
        CreateInputLayout = 78,
        CreateVertexShader = 79,
        CreateGeometryShader = 80,
        CreateGemoetryShaderWithStreamOutput = 81,
        CreatePixelShader = 82,
        CreateBlendState = 83,
        CreateDepthStencilState = 84,
        CreateRasterizerState = 85,
        CreateSamplerState = 86,
        CreateQuery = 87,
        CreatePredicate = 88,
        CreateCounter = 89,
        CheckFormatSupport = 90,
        CheckMultisampleQualityLevels = 91,
        CheckCounterInfo = 92,
        CheckCounter = 93,
        GetCreationFlags = 94,
        OpenSharedResource = 95,
        SetTextFilterSize = 96,
        GetTextFilterSize = 97
    }

    /// <summary>
    ///     Direct3D 10 Hook - this hooks the SwapChain.Present method to capture images
    /// </summary>
    internal class DXHookD3D10 : BaseDXHook {
        private const int D3D10_DEVICE_METHOD_COUNT = 98;

        private List<IntPtr> _d3d10VTblAddresses;
        private List<IntPtr> _dxgiSwapChainVTblAddresses;

        private Hook<DXGISwapChain_PresentDelegate> DXGISwapChain_PresentHook;
        private Hook<DXGISwapChain_ResizeTargetDelegate> DXGISwapChain_ResizeTargetHook;

        public DXHookD3D10(CaptureInterface ssInterface) : base(ssInterface) => DebugMessage("Create");

        protected override string HookName => "DXHookD3D10";

        public override void Hook() {
            DebugMessage("Hook: Begin");

            // Determine method addresses in Direct3D10.Device, and DXGI.SwapChain
            if (_d3d10VTblAddresses == null) {
                _d3d10VTblAddresses = new List<IntPtr>();
                _dxgiSwapChainVTblAddresses = new List<IntPtr>();
                DebugMessage("Hook: Before device creation");
                using (var factory = new Factory1()) {
                    using (var device = new Device(factory.GetAdapter(0), DeviceCreationFlags.None)) {
                        DebugMessage("Hook: Device created");
                        _d3d10VTblAddresses.AddRange(GetVTblAddresses(device.NativePointer, D3D10_DEVICE_METHOD_COUNT));

                        using (var renderForm = new Form()) {
                            using (var sc = new SwapChain(factory, device, DXGI.CreateSwapChainDescription(renderForm.Handle))) {
                                _dxgiSwapChainVTblAddresses.AddRange(GetVTblAddresses(sc.NativePointer, DXGI.DXGI_SWAPCHAIN_METHOD_COUNT));
                            }
                        }
                    }
                }
            }

            // We will capture the backbuffer here
            DXGISwapChain_PresentHook = new Hook<DXGISwapChain_PresentDelegate>(
                _dxgiSwapChainVTblAddresses[(int) DXGI.DXGISwapChainVTbl.Present],
                new DXGISwapChain_PresentDelegate(PresentHook),
                this
            );

            // We will capture target/window resizes here
            DXGISwapChain_ResizeTargetHook = new Hook<DXGISwapChain_ResizeTargetDelegate>(
                _dxgiSwapChainVTblAddresses[(int) DXGI.DXGISwapChainVTbl.ResizeTarget],
                new DXGISwapChain_ResizeTargetDelegate(ResizeTargetHook),
                this
            );

            /*
             * Don't forget that all hooks will start deactivated...
             * The following ensures that all threads are intercepted:
             * Note: you must do this for each hook.
             */
            DXGISwapChain_PresentHook.Activate();

            DXGISwapChain_ResizeTargetHook.Activate();

            Hooks.Add(DXGISwapChain_PresentHook);
            Hooks.Add(DXGISwapChain_ResizeTargetHook);
        }

        public override void Cleanup() {
            try {
            } catch {
            }
        }

        /// <summary>
        ///     Hooked to allow resizing a texture/surface that is reused. Currently not in use as we create the texture for each
        ///     request
        ///     to support different sizes each time (as we use DirectX to copy only the region we are after rather than the entire
        ///     backbuffer)
        /// </summary>
        /// <param name="swapChainPtr"></param>
        /// <param name="newTargetParameters"></param>
        /// <returns></returns>
        private int ResizeTargetHook(IntPtr swapChainPtr, ref ModeDescription newTargetParameters) {
            var swapChain = (SwapChain) swapChainPtr;

            // This version creates a new texture for each request so there is nothing to resize.
            // IF the size of the texture is known each time, we could create it once, and then possibly need to resize it here

            swapChain.ResizeTarget(ref newTargetParameters);
            return Result.Ok.Code;
        }

        /// <summary>
        ///     Our present hook that will grab a copy of the backbuffer when requested. Note: this supports multi-sampling
        ///     (anti-aliasing)
        /// </summary>
        /// <param name="swapChainPtr"></param>
        /// <param name="syncInterval"></param>
        /// <param name="flags"></param>
        /// <returns>The HRESULT of the original method</returns>
        private int PresentHook(IntPtr swapChainPtr, int syncInterval, PresentFlags flags) {
            var swapChain = (SwapChain) swapChainPtr;

            try {
                #region Screenshot Request

                if (Request != null)
                    try {
                        DebugMessage("PresentHook: Request Start");
                        var startTime = DateTime.Now;
                        using (var texture = Resource.FromSwapChain<Texture2D>(swapChain, 0)) {
                            #region Determine region to capture

                            var regionToCapture = new Rectangle(0, 0, texture.Description.Width, texture.Description.Height);

                            if (Request.RegionToCapture.Width > 0) regionToCapture = Request.RegionToCapture;

                            #endregion

                            var theTexture = texture;

                            // If texture is multisampled, then we can use ResolveSubresource to copy it into a non-multisampled texture
                            Texture2D textureResolved = null;
                            if (texture.Description.SampleDescription.Count > 1) {
                                DebugMessage("PresentHook: resolving multi-sampled texture");
                                // texture is multi-sampled, lets resolve it down to single sample
                                textureResolved = new Texture2D(
                                    texture.Device,
                                    new Texture2DDescription {
                                        CpuAccessFlags = CpuAccessFlags.None,
                                        Format = texture.Description.Format,
                                        Height = texture.Description.Height,
                                        Usage = ResourceUsage.Default,
                                        Width = texture.Description.Width,
                                        ArraySize = 1,
                                        SampleDescription = new SampleDescription(1, 0), // Ensure single sample
                                        BindFlags = BindFlags.None,
                                        MipLevels = 1,
                                        OptionFlags = texture.Description.OptionFlags
                                    }
                                );
                                // Resolve into textureResolved
                                texture.Device.ResolveSubresource(texture, 0, textureResolved, 0, texture.Description.Format);

                                // Make "theTexture" be the resolved texture
                                theTexture = textureResolved;
                            }

                            // Create destination texture
                            var textureDest = new Texture2D(
                                texture.Device,
                                new Texture2DDescription {
                                    CpuAccessFlags = CpuAccessFlags.None, // CpuAccessFlags.Write | CpuAccessFlags.Read,
                                    Format = Format.R8G8B8A8_UNorm, // Supports BMP/PNG
                                    Height = regionToCapture.Height,
                                    Usage = ResourceUsage.Default, // ResourceUsage.Staging,
                                    Width = regionToCapture.Width,
                                    ArraySize = 1, //texture.Description.ArraySize,
                                    SampleDescription = new SampleDescription(1, 0), // texture.Description.SampleDescription,
                                    BindFlags = BindFlags.None,
                                    MipLevels = 1, //texture.Description.MipLevels,
                                    OptionFlags = texture.Description.OptionFlags
                                }
                            );

                            // Copy the subresource region, we are dealing with a flat 2D texture with no MipMapping, so 0 is the subresource index
                            theTexture.Device.CopySubresourceRegion(
                                theTexture,
                                0,
                                new ResourceRegion {
                                    Top = regionToCapture.Top,
                                    Bottom = regionToCapture.Bottom,
                                    Left = regionToCapture.Left,
                                    Right = regionToCapture.Right,
                                    Front = 0,
                                    Back = 1 // Must be 1 or only black will be copied
                                },
                                textureDest,
                                0,
                                0,
                                0,
                                0
                            );

                            // Note: it would be possible to capture multiple frames and process them in a background thread

                            // Copy to memory and send back to host process on a background thread so that we do not cause any delay in the rendering pipeline
                            var request = Request.Clone(); // this.Request gets set to null, so copy the Request for use in the thread
                            ThreadPool.QueueUserWorkItem(
                                delegate {
                                    //FileStream fs = new FileStream(@"c:\temp\temp.bmp", FileMode.Create);
                                    //Texture2D.ToStream(testSubResourceCopy, ImageFileFormat.Bmp, fs);

                                    var startCopyToSystemMemory = DateTime.Now;
                                    using (var ms = new MemoryStream()) {
                                        Resource.ToStream(textureDest, ImageFileFormat.Bmp, ms);
                                        ms.Position = 0;
                                        DebugMessage("PresentHook: Copy to System Memory time: " + (DateTime.Now - startCopyToSystemMemory));

                                        var startSendResponse = DateTime.Now;
                                        ProcessCapture(ms, request);
                                        DebugMessage("PresentHook: Send response time: " + (DateTime.Now - startSendResponse));
                                    }

                                    // Free the textureDest as we no longer need it.
                                    textureDest.Dispose();
                                    textureDest = null;
                                    DebugMessage("PresentHook: Full Capture time: " + (DateTime.Now - startTime));
                                }
                            );

                            // Make sure we free up the resolved texture if it was created
                            if (textureResolved != null) {
                                textureResolved.Dispose();
                                textureResolved = null;
                            }
                        }

                        DebugMessage("PresentHook: Copy BackBuffer time: " + (DateTime.Now - startTime));
                        DebugMessage("PresentHook: Request End");
                    } finally {
                        // Prevent the request from being processed a second time
                        Request = null;
                    }

                #endregion
            } catch (Exception e) {
                // If there is an error we do not want to crash the hooked application, so swallow the exception
                DebugMessage("PresentHook: Exeception: " + e.GetType().FullName + ": " + e.Message);
            }

            // As always we need to call the original method, note that EasyHook has already repatched the original method
            // so calling it here will not cause an endless recursion to this function
            swapChain.Present(syncInterval, flags);
            return Result.Ok.Code;
        }

        /// <summary>
        ///     The IDXGISwapChain.Present function definition
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int DXGISwapChain_PresentDelegate(IntPtr swapChainPtr, int syncInterval, PresentFlags flags);

        /// <summary>
        ///     The IDXGISwapChain.ResizeTarget function definition
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int DXGISwapChain_ResizeTargetDelegate(IntPtr swapChainPtr, ref ModeDescription newTargetParameters);
    }
}