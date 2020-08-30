﻿using System;
using System.Drawing;
using System.Runtime.Remoting;
using System.Security.Permissions;

namespace Capture.Interface {
    [Serializable]
    public class ScreenshotRequest : MarshalByRefObject, IDisposable {
        private bool _disposed;

        public ScreenshotRequest(Rectangle region, Size resize) : this(Guid.NewGuid(), region, resize) { }

        public ScreenshotRequest(Rectangle region) : this(Guid.NewGuid(), region, null) { }

        public ScreenshotRequest(Guid requestId, Rectangle region) : this(requestId, region, null) { }

        public ScreenshotRequest(Guid requestId, Rectangle region, Size? resize) {
            RequestId = requestId;
            RegionToCapture = region;
            Resize = resize;
        }

        public Guid RequestId { get; set; }
        public Rectangle RegionToCapture { get; set; }
        public Size? Resize { get; set; }
        public ImageFormat Format { get; set; }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ScreenshotRequest Clone() => new ScreenshotRequest(RequestId, RegionToCapture, Resize) {Format = Format};

        ~ScreenshotRequest() => Dispose(false);

        protected virtual void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) Disconnect();
                _disposed = true;
            }
        }

        /// <summary>
        ///     Disconnects the remoting channel(s) of this object and all nested objects.
        /// </summary>
        private void Disconnect() => RemotingServices.Disconnect(this);

        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService() =>
            // Returning null designates an infinite non-expiring lease.
            // We must therefore ensure that RemotingServices.Disconnect() is called when
            // it's no longer needed otherwise there will be a memory leak.
            null;
    }
}