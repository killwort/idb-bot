using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Capture.Hook.DX11;
using Capture.Interface;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using SharpDX.Windows;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Resource = SharpDX.DXGI.Resource;

namespace Capture.Hook {
    internal enum D3D11DeviceVTbl : short {
        // IUnknown
        QueryInterface = 0,
        AddRef = 1,
        Release = 2,

        // ID3D11Device
        CreateBuffer = 3,
        CreateTexture1D = 4,
        CreateTexture2D = 5,
        CreateTexture3D = 6,
        CreateShaderResourceView = 7,
        CreateUnorderedAccessView = 8,
        CreateRenderTargetView = 9,
        CreateDepthStencilView = 10,
        CreateInputLayout = 11,
        CreateVertexShader = 12,
        CreateGeometryShader = 13,
        CreateGeometryShaderWithStreamOutput = 14,
        CreatePixelShader = 15,
        CreateHullShader = 16,
        CreateDomainShader = 17,
        CreateComputeShader = 18,
        CreateClassLinkage = 19,
        CreateBlendState = 20,
        CreateDepthStencilState = 21,
        CreateRasterizerState = 22,
        CreateSamplerState = 23,
        CreateQuery = 24,
        CreatePredicate = 25,
        CreateCounter = 26,
        CreateDeferredContext = 27,
        OpenSharedResource = 28,
        CheckFormatSupport = 29,
        CheckMultisampleQualityLevels = 30,
        CheckCounterInfo = 31,
        CheckCounter = 32,
        CheckFeatureSupport = 33,
        GetPrivateData = 34,
        SetPrivateData = 35,
        SetPrivateDataInterface = 36,
        GetFeatureLevel = 37,
        GetCreationFlags = 38,
        GetDeviceRemovedReason = 39,
        GetImmediateContext = 40,
        SetExceptionMode = 41,
        GetExceptionMode = 42
    }

    /// <summary>
    ///     Direct3D 11 Hook - this hooks the SwapChain.Present to take screenshots
    /// </summary>
    internal class DXHookD3D11 : BaseDXHook {
        private const int D3D11_DEVICE_METHOD_COUNT = 43;

        private List<IntPtr> _d3d11VTblAddresses;
        private List<IntPtr> _dxgiSwapChainVTblAddresses;
        private bool _finalRTMapped;

        private readonly object _lock = new object();

        private Query _query;
        private bool _queryIssued;
        private ScreenshotRequest _requestCopy;

        private IntPtr _swapChainPointer = IntPtr.Zero;

        private Hook<DXGISwapChain_PresentDelegate> DXGISwapChain_PresentHook;
        private Hook<DXGISwapChain_ResizeTargetDelegate> DXGISwapChain_ResizeTargetHook;

        private ImagingFactory2 wicFactory;

        public DXHookD3D11(CaptureInterface ssInterface) : base(ssInterface) { }

        protected override string HookName => "DXHookD3D11";

        public override void Hook() {
            DebugMessage("Hook: Begin");
            if (_d3d11VTblAddresses == null) {
                _d3d11VTblAddresses = new List<IntPtr>();
                _dxgiSwapChainVTblAddresses = new List<IntPtr>();

                #region Get Device and SwapChain method addresses

                // Create temporary device + swapchain and determine method addresses
                _renderForm = ToDispose(new RenderForm());
                DebugMessage("Hook: Before device creation");
                Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.BgraSupport, DXGI.CreateSwapChainDescription(_renderForm.Handle), out _device, out _swapChain);

                ToDispose(_device);
                ToDispose(_swapChain);

                if (_device != null && _swapChain != null) {
                    DebugMessage("Hook: Device created");
                    _d3d11VTblAddresses.AddRange(GetVTblAddresses(_device.NativePointer, D3D11_DEVICE_METHOD_COUNT));
                    _dxgiSwapChainVTblAddresses.AddRange(GetVTblAddresses(_swapChain.NativePointer, DXGI.DXGI_SWAPCHAIN_METHOD_COUNT));
                } else {
                    DebugMessage("Hook: Device creation failed");
                }

                #endregion
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
        private int ResizeTargetHook(IntPtr swapChainPtr, ref ModeDescription newTargetParameters) => DXGISwapChain_ResizeTargetHook.Original(swapChainPtr, ref newTargetParameters);

        private void EnsureResources(Device device, Texture2DDescription description, Rectangle captureRegion, ScreenshotRequest request, bool useSameDeviceForResize = false) {
            var resizeDevice = useSameDeviceForResize ? device : _device;

            // Check if _resolvedRT or _finalRT require creation
            if (_finalRT != null && (_finalRT.Device.NativePointer == device.NativePointer || _finalRT.Device.NativePointer == _device.NativePointer) &&
                _finalRT.Description.Height == captureRegion.Height && _finalRT.Description.Width == captureRegion.Width && _resolvedRT != null &&
                _resolvedRT.Description.Height == description.Height && _resolvedRT.Description.Width == description.Width &&
                (_resolvedRT.Device.NativePointer == device.NativePointer || _resolvedRT.Device.NativePointer == _device.NativePointer) && _resolvedRT.Description.Format == description.Format) {
            } else {
                RemoveAndDispose(ref _query);
                RemoveAndDispose(ref _resolvedRT);
                RemoveAndDispose(ref _resolvedSRV);
                RemoveAndDispose(ref _finalRT);
                RemoveAndDispose(ref _resolvedRTShared);
                RemoveAndDispose(ref _resolvedRTKeyedMutex);
                RemoveAndDispose(ref _resolvedRTKeyedMutex_Dev2);

                _query = new Query(
                    resizeDevice,
                    new QueryDescription {
                        Flags = QueryFlags.None,
                        Type = QueryType.Event
                    }
                );
                _queryIssued = false;

                try {
                    var resolvedRTOptionFlags = ResourceOptionFlags.None;

                    if (device != resizeDevice)
                        resolvedRTOptionFlags |= ResourceOptionFlags.SharedKeyedmutex;

                    _resolvedRT = ToDispose(
                        new Texture2D(
                            device,
                            new Texture2DDescription {
                                CpuAccessFlags = CpuAccessFlags.None,
                                Format = description.Format, // for multisampled backbuffer, this must be same format
                                Height = description.Height,
                                Usage = ResourceUsage.Default,
                                Width = description.Width,
                                ArraySize = 1,
                                SampleDescription = new SampleDescription(1, 0), // Ensure single sample
                                BindFlags = BindFlags.ShaderResource,
                                MipLevels = 1,
                                OptionFlags = resolvedRTOptionFlags
                            }
                        )
                    );
                } catch {
                    // Failed to create the shared resource, try again using the same device as game for resize
                    EnsureResources(device, description, captureRegion, request, true);
                    return;
                }

                // Retrieve reference to the keyed mutex
                _resolvedRTKeyedMutex = ToDispose(_resolvedRT.QueryInterfaceOrNull<KeyedMutex>());

                // If the resolvedRT is a shared resource _resolvedRTKeyedMutex will not be null
                if (_resolvedRTKeyedMutex != null) {
                    using (var resource = _resolvedRT.QueryInterface<Resource>()) {
                        _resolvedRTShared = ToDispose(resizeDevice.OpenSharedResource<Texture2D>(resource.SharedHandle));
                        _resolvedRTKeyedMutex_Dev2 = ToDispose(_resolvedRTShared.QueryInterfaceOrNull<KeyedMutex>());
                    }

                    // SRV for use if resizing
                    _resolvedSRV = ToDispose(new ShaderResourceView(resizeDevice, _resolvedRTShared));
                } else {
                    _resolvedSRV = ToDispose(new ShaderResourceView(resizeDevice, _resolvedRT));
                }

                _finalRT = ToDispose(
                    new Texture2D(
                        resizeDevice,
                        new Texture2DDescription {
                            CpuAccessFlags = CpuAccessFlags.Read,
                            Format = description.Format,
                            Height = captureRegion.Height,
                            Usage = ResourceUsage.Staging,
                            Width = captureRegion.Width,
                            ArraySize = 1,
                            SampleDescription = new SampleDescription(1, 0),
                            BindFlags = BindFlags.None,
                            MipLevels = 1,
                            OptionFlags = ResourceOptionFlags.None
                        }
                    )
                );
                _finalRTMapped = false;
            }

            if (_resolvedRT != null && _resolvedRTKeyedMutex_Dev2 == null && resizeDevice == _device)
                resizeDevice = device;

            if (resizeDevice != null && request.Resize != null && (_resizedRT == null || _resizedRT.Device.NativePointer != resizeDevice.NativePointer ||
                                                                   _resizedRT.Description.Width != request.Resize.Value.Width || _resizedRT.Description.Height != request.Resize.Value.Height)) {
                // Create/Recreate resources for resizing
                RemoveAndDispose(ref _resizedRT);
                RemoveAndDispose(ref _resizedRTV);
                RemoveAndDispose(ref _saQuad);

                _resizedRT = ToDispose(
                    new Texture2D(
                        resizeDevice,
                        new Texture2DDescription {
                            Format = Format.R8G8B8A8_UNorm, // Supports BMP/PNG/etc
                            Height = request.Resize.Value.Height,
                            Width = request.Resize.Value.Width,
                            ArraySize = 1,
                            SampleDescription = new SampleDescription(1, 0),
                            BindFlags = BindFlags.RenderTarget,
                            MipLevels = 1,
                            Usage = ResourceUsage.Default,
                            OptionFlags = ResourceOptionFlags.None
                        }
                    )
                );

                _resizedRTV = ToDispose(new RenderTargetView(resizeDevice, _resizedRT));

                _saQuad = ToDispose(new ScreenAlignedQuadRenderer());
                _saQuad.Initialize(new DeviceManager(resizeDevice));
            }
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

                if (Request != null) {
                    DebugMessage("PresentHook: Request Start");
                    var startTime = DateTime.Now;
                    using (var currentRT = SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(swapChain, 0)) {
                        #region Determine region to capture

                        var captureRegion = new Rectangle(0, 0, currentRT.Description.Width, currentRT.Description.Height);

                        if (Request.RegionToCapture.Width > 0)
                            captureRegion = new Rectangle(Request.RegionToCapture.Left, Request.RegionToCapture.Top, Request.RegionToCapture.Right, Request.RegionToCapture.Bottom);
                        else if (Request.Resize.HasValue) captureRegion = new Rectangle(0, 0, Request.Resize.Value.Width, Request.Resize.Value.Height);

                        #endregion

                        // Create / Recreate resources as necessary
                        EnsureResources(currentRT.Device, currentRT.Description, captureRegion, Request);

                        Texture2D sourceTexture = null;

                        // If texture is multisampled, then we can use ResolveSubresource to copy it into a non-multisampled texture
                        if (currentRT.Description.SampleDescription.Count > 1 || Request.Resize.HasValue) {
                            if (Request.Resize.HasValue)
                                DebugMessage("PresentHook: resizing texture");
                            else
                                DebugMessage("PresentHook: resolving multi-sampled texture");

                            // Resolve into _resolvedRT
                            if (_resolvedRTKeyedMutex != null)
                                _resolvedRTKeyedMutex.Acquire(0, int.MaxValue);
                            currentRT.Device.ImmediateContext.ResolveSubresource(currentRT, 0, _resolvedRT, 0, _resolvedRT.Description.Format);
                            if (_resolvedRTKeyedMutex != null)
                                _resolvedRTKeyedMutex.Release(1);

                            if (Request.Resize.HasValue) {
                                lock (_lock) {
                                    if (_resolvedRTKeyedMutex_Dev2 != null)
                                        _resolvedRTKeyedMutex_Dev2.Acquire(1, int.MaxValue);
                                    _saQuad.ShaderResource = _resolvedSRV;
                                    _saQuad.RenderTargetView = _resizedRTV;
                                    _saQuad.RenderTarget = _resizedRT;
                                    _saQuad.Render();
                                    if (_resolvedRTKeyedMutex_Dev2 != null)
                                        _resolvedRTKeyedMutex_Dev2.Release(0);
                                }

                                // set sourceTexture to the resized RT
                                sourceTexture = _resizedRT;
                            } else {
                                // Make sourceTexture be the resolved texture
                                if (_resolvedRTShared != null)
                                    sourceTexture = _resolvedRTShared;
                                else
                                    sourceTexture = _resolvedRT;
                            }
                        } else {
                            // Copy the resource into the shared texture
                            if (_resolvedRTKeyedMutex != null) _resolvedRTKeyedMutex.Acquire(0, int.MaxValue);
                            currentRT.Device.ImmediateContext.CopySubresourceRegion(currentRT, 0, null, _resolvedRT, 0);
                            if (_resolvedRTKeyedMutex != null) _resolvedRTKeyedMutex.Release(1);

                            if (_resolvedRTShared != null)
                                sourceTexture = _resolvedRTShared;
                            else
                                sourceTexture = _resolvedRT;
                        }

                        // Copy to memory and send back to host process on a background thread so that we do not cause any delay in the rendering pipeline
                        _requestCopy = Request.Clone(); // this.Request gets set to null, so copy the Request for use in the thread

                        // Prevent the request from being processed a second time
                        Request = null;

                        var acquireLock = sourceTexture == _resolvedRTShared;

                        ThreadPool.QueueUserWorkItem(
                            o => {
                                // Acquire lock on second device
                                if (acquireLock && _resolvedRTKeyedMutex_Dev2 != null)
                                    _resolvedRTKeyedMutex_Dev2.Acquire(1, int.MaxValue);

                                lock (_lock) {
                                    // Copy the subresource region, we are dealing with a flat 2D texture with no MipMapping, so 0 is the subresource index
                                    sourceTexture.Device.ImmediateContext.CopySubresourceRegion(
                                        sourceTexture,
                                        0,
                                        new ResourceRegion {
                                            Top = captureRegion.Top,
                                            Bottom = captureRegion.Bottom,
                                            Left = captureRegion.Left,
                                            Right = captureRegion.Right,
                                            Front = 0,
                                            Back = 1 // Must be 1 or only black will be copied
                                        },
                                        _finalRT,
                                        0
                                    );

                                    // Release lock upon shared surface on second device
                                    if (acquireLock && _resolvedRTKeyedMutex_Dev2 != null)
                                        _resolvedRTKeyedMutex_Dev2.Release(0);

                                    _finalRT.Device.ImmediateContext.End(_query);
                                    _queryIssued = true;
                                    while (_finalRT.Device.ImmediateContext.GetData(_query).ReadByte() != 1) {
                                        // Spin (usually only one cycle or no spin takes place)
                                    }

                                    var startCopyToSystemMemory = DateTime.Now;
                                    try {
                                        DataBox db = default;
                                        if (_requestCopy.Format == ImageFormat.PixelData) {
                                            db = _finalRT.Device.ImmediateContext.MapSubresource(_finalRT, 0, MapMode.Read, MapFlags.DoNotWait);
                                            _finalRTMapped = true;
                                        }

                                        _queryIssued = false;

                                        try {
                                            using (var ms = new MemoryStream()) {
                                                switch (_requestCopy.Format) {
                                                    case ImageFormat.Bitmap:
                                                    case ImageFormat.Jpeg:
                                                    case ImageFormat.Png:
                                                        ToStream(_finalRT.Device.ImmediateContext, _finalRT, _requestCopy.Format, ms);
                                                        break;
                                                    case ImageFormat.PixelData:
                                                        if (db.DataPointer != IntPtr.Zero)
                                                            ProcessCapture(
                                                                _finalRT.Description.Width,
                                                                _finalRT.Description.Height,
                                                                db.RowPitch,
                                                                PixelFormat.Format32bppArgb,
                                                                db.DataPointer,
                                                                _requestCopy
                                                            );
                                                        return;
                                                }

                                                ms.Position = 0;
                                                ProcessCapture(ms, _requestCopy);
                                            }
                                        } finally {
                                            DebugMessage("PresentHook: Copy to System Memory time: " + (DateTime.Now - startCopyToSystemMemory));

                                            if (_finalRTMapped)
                                                lock (_lock) {
                                                    _finalRT.Device.ImmediateContext.UnmapSubresource(_finalRT, 0);
                                                    _finalRTMapped = false;
                                                }
                                        }
                                    } catch (SharpDXException exc) {
                                        // Catch DXGI_ERROR_WAS_STILL_DRAWING and ignore - the data isn't available yet
                                    }
                                }
                            }
                        );

                        // Note: it would be possible to capture multiple frames and process them in a background thread
                    }

                    DebugMessage("PresentHook: Copy BackBuffer time: " + (DateTime.Now - startTime));
                    DebugMessage("PresentHook: Request End");
                }

                #endregion
            } catch (Exception e) {
                // If there is an error we do not want to crash the hooked application, so swallow the exception
                DebugMessage("PresentHook: Exeception: " + e.GetType().FullName + ": " + e);
                //return unchecked((int)0x8000FFFF); //E_UNEXPECTED
            }

            // As always we need to call the original method, note that EasyHook will automatically skip the hook and call the original method
            // i.e. calling it here will not cause a stack overflow into this function
            return DXGISwapChain_PresentHook.Original(swapChainPtr, syncInterval, flags);
        }

        /// <summary>
        ///     Copies to a stream using WIC. The format is converted if necessary.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="texture"></param>
        /// <param name="outputFormat"></param>
        /// <param name="stream"></param>
        public void ToStream(DeviceContext context, Texture2D texture, ImageFormat outputFormat, Stream stream) {
            if (wicFactory == null)
                wicFactory = ToDispose(new ImagingFactory2());

            DataStream dataStream;
            var dataBox = context.MapSubresource(texture, 0, 0, MapMode.Read, MapFlags.None, out dataStream);
            try {
                var dataRectangle = new DataRectangle {
                    DataPointer = dataStream.DataPointer,
                    Pitch = dataBox.RowPitch
                };

                var format = PixelFormatFromFormat(texture.Description.Format);

                if (format == Guid.Empty)
                    return;

                using (var bitmap = new Bitmap(wicFactory, texture.Description.Width, texture.Description.Height, format, dataRectangle)) {
                    stream.Position = 0;

                    BitmapEncoder bitmapEncoder = null;
                    switch (outputFormat) {
                        case ImageFormat.Bitmap:
                            bitmapEncoder = new BmpBitmapEncoder(wicFactory, stream);
                            break;
                        case ImageFormat.Jpeg:
                            bitmapEncoder = new JpegBitmapEncoder(wicFactory, stream);
                            break;
                        case ImageFormat.Png:
                            bitmapEncoder = new PngBitmapEncoder(wicFactory, stream);
                            break;
                        default:
                            return;
                    }

                    try {
                        using (var bitmapFrameEncode = new BitmapFrameEncode(bitmapEncoder)) {
                            bitmapFrameEncode.Initialize();
                            bitmapFrameEncode.SetSize(bitmap.Size.Width, bitmap.Size.Height);
                            var pixelFormat = format;
                            bitmapFrameEncode.SetPixelFormat(ref pixelFormat);

                            if (pixelFormat != format)
                                // IWICFormatConverter
                                using (var converter = new FormatConverter(wicFactory)) {
                                    if (converter.CanConvert(format, pixelFormat)) {
                                        converter.Initialize(bitmap, SharpDX.WIC.PixelFormat.Format24bppBGR, BitmapDitherType.None, null, 0, BitmapPaletteType.MedianCut);
                                        bitmapFrameEncode.SetPixelFormat(ref pixelFormat);
                                        bitmapFrameEncode.WriteSource(converter);
                                    } else {
                                        DebugMessage(string.Format("Unable to convert Direct3D texture format {0} to a suitable WIC format", texture.Description.Format.ToString()));
                                        return;
                                    }
                                }
                            else
                                bitmapFrameEncode.WriteSource(bitmap);

                            bitmapFrameEncode.Commit();
                            bitmapEncoder.Commit();
                        }
                    } finally {
                        bitmapEncoder.Dispose();
                    }
                }
            } finally {
                context.UnmapSubresource(texture, 0);
            }
        }

        public static Guid PixelFormatFromFormat(Format format) {
            switch (format) {
                case Format.R32G32B32A32_Typeless:
                case Format.R32G32B32A32_Float:
                    return SharpDX.WIC.PixelFormat.Format128bppRGBAFloat;
                case Format.R32G32B32A32_UInt:
                case Format.R32G32B32A32_SInt:
                    return SharpDX.WIC.PixelFormat.Format128bppRGBAFixedPoint;
                case Format.R32G32B32_Typeless:
                case Format.R32G32B32_Float:
                    return SharpDX.WIC.PixelFormat.Format96bppRGBFloat;
                case Format.R32G32B32_UInt:
                case Format.R32G32B32_SInt:
                    return SharpDX.WIC.PixelFormat.Format96bppRGBFixedPoint;
                case Format.R16G16B16A16_Typeless:
                case Format.R16G16B16A16_Float:
                case Format.R16G16B16A16_UNorm:
                case Format.R16G16B16A16_UInt:
                case Format.R16G16B16A16_SNorm:
                case Format.R16G16B16A16_SInt:
                    return SharpDX.WIC.PixelFormat.Format64bppRGBA;
                case Format.R32G32_Typeless:
                case Format.R32G32_Float:
                case Format.R32G32_UInt:
                case Format.R32G32_SInt:
                case Format.R32G8X24_Typeless:
                case Format.D32_Float_S8X24_UInt:
                case Format.R32_Float_X8X24_Typeless:
                case Format.X32_Typeless_G8X24_UInt:
                    return Guid.Empty;
                case Format.R10G10B10A2_Typeless:
                case Format.R10G10B10A2_UNorm:
                case Format.R10G10B10A2_UInt:
                    return SharpDX.WIC.PixelFormat.Format32bppRGBA1010102;
                case Format.R11G11B10_Float:
                    return Guid.Empty;
                case Format.R8G8B8A8_Typeless:
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRgb:
                case Format.R8G8B8A8_UInt:
                case Format.R8G8B8A8_SNorm:
                case Format.R8G8B8A8_SInt:
                    return SharpDX.WIC.PixelFormat.Format32bppRGBA;
                case Format.R16G16_Typeless:
                case Format.R16G16_Float:
                case Format.R16G16_UNorm:
                case Format.R16G16_UInt:
                case Format.R16G16_SNorm:
                case Format.R16G16_SInt:
                    return Guid.Empty;
                case Format.R32_Typeless:
                case Format.D32_Float:
                case Format.R32_Float:
                case Format.R32_UInt:
                case Format.R32_SInt:
                    return Guid.Empty;
                case Format.R24G8_Typeless:
                case Format.D24_UNorm_S8_UInt:
                case Format.R24_UNorm_X8_Typeless:
                    return SharpDX.WIC.PixelFormat.Format32bppGrayFloat;
                case Format.X24_Typeless_G8_UInt:
                case Format.R9G9B9E5_Sharedexp:
                case Format.R8G8_B8G8_UNorm:
                case Format.G8R8_G8B8_UNorm:
                    return Guid.Empty;
                case Format.B8G8R8A8_UNorm:
                case Format.B8G8R8X8_UNorm:
                    return SharpDX.WIC.PixelFormat.Format32bppBGRA;
                case Format.R10G10B10_Xr_Bias_A2_UNorm:
                    return SharpDX.WIC.PixelFormat.Format32bppBGR101010;
                case Format.B8G8R8A8_Typeless:
                case Format.B8G8R8A8_UNorm_SRgb:
                case Format.B8G8R8X8_Typeless:
                case Format.B8G8R8X8_UNorm_SRgb:
                    return SharpDX.WIC.PixelFormat.Format32bppBGRA;
                case Format.R8G8_Typeless:
                case Format.R8G8_UNorm:
                case Format.R8G8_UInt:
                case Format.R8G8_SNorm:
                case Format.R8G8_SInt:
                    return Guid.Empty;
                case Format.R16_Typeless:
                case Format.R16_Float:
                case Format.D16_UNorm:
                case Format.R16_UNorm:
                case Format.R16_SNorm:
                    return SharpDX.WIC.PixelFormat.Format16bppGrayHalf;
                case Format.R16_UInt:
                case Format.R16_SInt:
                    return SharpDX.WIC.PixelFormat.Format16bppGrayFixedPoint;
                case Format.B5G6R5_UNorm:
                    return SharpDX.WIC.PixelFormat.Format16bppBGR565;
                case Format.B5G5R5A1_UNorm:
                    return SharpDX.WIC.PixelFormat.Format16bppBGRA5551;
                case Format.B4G4R4A4_UNorm:
                    return Guid.Empty;

                case Format.R8_Typeless:
                case Format.R8_UNorm:
                case Format.R8_UInt:
                case Format.R8_SNorm:
                case Format.R8_SInt:
                    return SharpDX.WIC.PixelFormat.Format8bppGray;
                case Format.A8_UNorm:
                    return SharpDX.WIC.PixelFormat.Format8bppAlpha;
                case Format.R1_UNorm:
                    return SharpDX.WIC.PixelFormat.Format1bppIndexed;

                default:
                    return Guid.Empty;
            }
        }

        /// <summary>
        ///     The IDXGISwapChain.Present function definition
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int DXGISwapChain_PresentDelegate(IntPtr swapChainPtr, int syncInterval, /* int */ PresentFlags flags);

        /// <summary>
        ///     The IDXGISwapChain.ResizeTarget function definition
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate int DXGISwapChain_ResizeTargetDelegate(IntPtr swapChainPtr, ref ModeDescription newTargetParameters);

        #region Internal device resources

        private Device _device;
        private SwapChain _swapChain;
        private RenderForm _renderForm;
        private Texture2D _resolvedRTShared;
        private KeyedMutex _resolvedRTSharedKeyedMutex;
        private ShaderResourceView _resolvedSRV;
        private ScreenAlignedQuadRenderer _saQuad;
        private Texture2D _finalRT;
        private Texture2D _resizedRT;
        private RenderTargetView _resizedRTV;

        #endregion

        #region Main device resources

        private Texture2D _resolvedRT;
        private KeyedMutex _resolvedRTKeyedMutex;
        private KeyedMutex _resolvedRTKeyedMutex_Dev2;

        #endregion
    }
}