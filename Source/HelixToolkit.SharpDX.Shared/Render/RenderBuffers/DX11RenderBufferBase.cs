﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using System.Linq;
using System;
#if NETFX_CORE
namespace HelixToolkit.UWP.Render
#else
namespace HelixToolkit.Wpf.SharpDX.Render
#endif
{
    using Core2D;
    /// <summary>
    /// 
    /// </summary>
    public abstract class DX11RenderBufferProxyBase : DisposeObject, IDX11RenderBufferProxy
    {
        /// <summary>
        /// Occurs when [on new buffer created].
        /// </summary>
        public event EventHandler<Texture2D> OnNewBufferCreated;
        /// <summary>
        /// Occurs when [on device lost].
        /// </summary>
        public event EventHandler<bool> OnDeviceLost;
        /// <summary>
        /// The color buffer
        /// </summary>
        protected Texture2D colorBuffer;
        /// <summary>
        /// The depth stencil buffer
        /// </summary>
        protected Texture2D depthStencilBuffer;
        /// <summary>
        /// The color buffer view
        /// </summary>
        protected RenderTargetView colorBufferView;
        /// <summary>
        /// The depth stencil buffer view
        /// </summary>
        protected DepthStencilView depthStencilBufferView;
        /// <summary>
        /// The D2D controls
        /// </summary>
        protected Device2DProxy d2dControls;
        /// <summary>
        /// Gets the d2 d controls.
        /// </summary>
        /// <value>
        /// The d2 d controls.
        /// </value>
        public IDevice2DProxy D2DControls
        {
            get { return d2dControls; }
        }
        /// <summary>
        /// Gets or sets the width of the target.
        /// </summary>
        /// <value>
        /// The width of the target.
        /// </value>
        public int TargetWidth { private set; get; }
        /// <summary>
        /// Gets or sets the height of the target.
        /// </summary>
        /// <value>
        /// The height of the target.
        /// </value>
        public int TargetHeight { private set; get; }
        /// <summary>
        /// Gets the color buffer view.
        /// </summary>
        /// <value>
        /// The color buffer view.
        /// </value>
        public RenderTargetView ColorBufferView { get { return colorBufferView; } }
        /// <summary>
        /// Gets the depth stencil buffer view.
        /// </summary>
        /// <value>
        /// The depth stencil buffer view.
        /// </value>
        public DepthStencilView DepthStencilBufferView { get { return depthStencilBufferView; } }
        /// <summary>
        /// Gets the color buffer.
        /// </summary>
        /// <value>
        /// The color buffer.
        /// </value>
        public Texture2D ColorBuffer { get { return colorBuffer; } }
        /// <summary>
        /// Gets the depth stencil buffer.
        /// </summary>
        /// <value>
        /// The depth stencil buffer.
        /// </value>
        public Texture2D DepthStencilBuffer { get { return depthStencilBuffer; } }

        private IDeviceContextPool deviceContextPool;
        /// <summary>
        /// Gets the device context pool.
        /// </summary>
        /// <value>
        /// The device context pool.
        /// </value>
        public IDeviceContextPool DeviceContextPool { get { return deviceContextPool; } }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DX11RenderBufferProxy"/> is initialized.
        /// </summary>
        /// <value>
        ///   <c>true</c> if initialized; otherwise, <c>false</c>.
        /// </value>
        public bool Initialized { private set; get; } = false;


#if MSAA
        /// <summary>
        /// Set MSAA level. If set to Two/Four/Eight, the actual level is set to minimum between Maximum and Two/Four/Eight
        /// </summary>
        public MSAALevel MSAA
        {
            private set; get;
        } = MSAALevel.Disable;
#endif

        /// <summary>
        /// The currently used Direct3D Device
        /// </summary>
        public Device Device
        {
            private set; get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DX11RenderBufferProxyBase"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        public DX11RenderBufferProxyBase(Device device)
        {
            Device = device;
            deviceContextPool = Collect(new DeviceContextPool(device));
        }

        private Texture2D CreateRenderTarget(int width, int height, MSAALevel msaa)
        {
#if MSAA
            MSAA = msaa;
#endif
            TargetWidth = width;
            TargetHeight = height;
            DisposeBuffers();
            var texture = OnCreateRenderTargetAndDepthBuffers(width, height);
            Initialized = true;
            OnNewBufferCreated?.Invoke(this, texture);
            return texture;
        }

        protected virtual void DisposeBuffers()
        {
            RemoveAndDispose(ref d2dControls);
            RemoveAndDispose(ref colorBufferView);
            RemoveAndDispose(ref depthStencilBufferView);
            RemoveAndDispose(ref colorBuffer);
            RemoveAndDispose(ref depthStencilBuffer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        protected abstract Texture2D OnCreateRenderTargetAndDepthBuffers(int width, int height);

        /// <summary>
        /// Sets the default render-targets
        /// </summary>
        public void SetDefaultRenderTargets(DeviceContext context)
        {
            context.OutputMerger.SetTargets(depthStencilBufferView, colorBufferView);
            context.Rasterizer.SetViewport(0, 0, TargetWidth, TargetWidth, 0.0f, 1.0f);
            context.Rasterizer.SetScissorRectangle(0, 0, TargetWidth, TargetHeight);
        }
        /// <summary>
        /// Clears the render target binding.
        /// </summary>
        /// <param name="context">The context.</param>
        public void ClearRenderTargetBinding(DeviceContext context)
        {
            context.OutputMerger.SetTargets(null, new RenderTargetView[0]);
        }
        /// <summary>
        /// Clears the render target.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="color">The color.</param>
        public void ClearRenderTarget(DeviceContext context, Color4 color)
        {
            ClearRenderTarget(context, color, true, true);
        }
        /// <summary>
        /// Clears the buffers with the clear-color
        /// </summary>
        /// <param name="context"></param>
        /// <param name="color"></param>
        /// <param name="clearBackBuffer"></param>
        /// <param name="clearDepthStencilBuffer"></param>
        public void ClearRenderTarget(DeviceContext context, Color4 color, bool clearBackBuffer, bool clearDepthStencilBuffer)
        {
            if (clearBackBuffer)
            {
                context.ClearRenderTargetView(colorBufferView, color);
            }

            if (clearDepthStencilBuffer)
            {
                context.ClearDepthStencilView(depthStencilBufferView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            }
        }
        /// <summary>
        /// Initializes.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="msaa">The msaa.</param>
        /// <returns></returns>
        public Texture2D Initialize(int width, int height, MSAALevel msaa)
        {
#if MSAA
            return CreateRenderTarget(width, height, msaa);
#else
            return CreateRenderTarget(width, height, MSAALevel.Disable);
#endif
        }
        /// <summary>
        /// Resize render target and depthbuffer resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public virtual Texture2D Resize(int width, int height)
        {
#if MSAA
            return CreateRenderTarget(width, height, MSAA);
#else
            return CreateRenderTarget(width, height, MSAALevel.Disable);
#endif
        }
        /// <summary>
        /// Begins the draw.
        /// </summary>
        /// <returns></returns>
        public virtual bool BeginDraw()
        {
            return Initialized;
        }
        /// <summary>
        /// Ends the draw.
        /// </summary>
        /// <returns></returns>
        public virtual bool EndDraw()
        {
            return true;
        }
        /// <summary>
        /// Presents this drawing..
        /// </summary>
        /// <returns></returns>
        public virtual bool Present()
        {
            return true;
        }
        /// <summary>
        /// Begins the 2d drawing.
        /// </summary>
        /// <returns></returns>
        public virtual bool BeginDraw2D()
        {
            d2dControls.D2DTarget.BeginDraw();
            return true;
        }
        /// <summary>
        /// Ends the 2D drawing.
        /// </summary>
        /// <returns></returns>
        public virtual bool EndDraw2D()
        {
            d2dControls.D2DTarget.EndDraw();
            return true;
        }
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposeManagedResources"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposeManagedResources)
        {
            OnNewBufferCreated = null;
            OnDeviceLost = null;
            Initialized = false;
            base.Dispose(disposeManagedResources);
        }

        #region ERROR HANDLING        
        /// <summary>
        /// Raises the on device lost.
        /// </summary>
        protected void RaiseOnDeviceLost()
        {
            OnDeviceLost?.Invoke(this, true);
        }
        #endregion
    }
}