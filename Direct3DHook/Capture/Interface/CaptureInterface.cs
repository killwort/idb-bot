﻿using System;
using System.Drawing;
using System.Threading;

namespace Capture.Interface {
    [Serializable]
    public delegate void MessageReceivedEvent(MessageReceivedEventArgs message);

    [Serializable]
    public delegate void ScreenshotReceivedEvent(ScreenshotReceivedEventArgs response);

    [Serializable]
    public delegate void DisconnectedEvent();

    [Serializable]
    public delegate void ScreenshotRequestedEvent(ScreenshotRequest request);

    [Serializable]
    public class CaptureInterface : MarshalByRefObject {
        /// <summary>
        ///     The client process Id
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        ///     Used to confirm connection to IPC server channel
        /// </summary>
        public DateTime Ping() => DateTime.Now;

        #region Events

        #region Server-side Events

        /// <summary>
        ///     Server event for sending debug and error information from the client to server
        /// </summary>
        public event MessageReceivedEvent RemoteMessage;

        /// <summary>
        ///     Server event for receiving screenshot image data
        /// </summary>
        public event ScreenshotReceivedEvent ScreenshotReceived;

        #endregion

        #region Client-side Events

        /// <summary>
        ///     Client event used to communicate to the client that it is time to create a screenshot
        /// </summary>
        public event ScreenshotRequestedEvent ScreenshotRequested;

        /// <summary>
        ///     Client event used to notify the hook to exit
        /// </summary>
        public event DisconnectedEvent Disconnected;

        #endregion

        #endregion

        #region Public Methods

        #region Still image Capture

        private object _lock = new object();
        private Guid? _requestId;
        private Action<Screenshot> _completeScreenshot;
        private ManualResetEvent _wait = new ManualResetEvent(false);

        /// <summary>
        ///     Get a fullscreen screenshot with the default timeout of 2 seconds
        /// </summary>
        public Screenshot GetScreenshot() => GetScreenshot(Rectangle.Empty, new TimeSpan(0, 0, 2), null, ImageFormat.Bitmap);

        /// <summary>
        ///     Get a screenshot of the specified region
        /// </summary>
        /// <param name="region">the region to capture (x=0,y=0 is top left corner)</param>
        /// <param name="timeout">maximum time to wait for the screenshot</param>
        public Screenshot GetScreenshot(Rectangle region, TimeSpan timeout, Size? resize, ImageFormat format) {
            lock (_lock) {
                Screenshot result = null;
                _requestId = Guid.NewGuid();
                _wait.Reset();

                SafeInvokeScreenshotRequested(
                    new ScreenshotRequest(_requestId.Value, region) {
                        Format = format,
                        Resize = resize
                    }
                );

                _completeScreenshot = sc => {
                    try {
                        Interlocked.Exchange(ref result, sc);
                    } catch {
                    }

                    _wait.Set();
                };

                _wait.WaitOne(timeout);
                _completeScreenshot = null;
                return result;
            }
        }

        public IAsyncResult BeginGetScreenshot(Rectangle region, TimeSpan timeout, AsyncCallback callback = null, Size? resize = null, ImageFormat format = ImageFormat.Bitmap) {
            Func<Rectangle, TimeSpan, Size?, ImageFormat, Screenshot> getScreenshot = GetScreenshot;

            return getScreenshot.BeginInvoke(region, timeout, resize, format, callback, getScreenshot);
        }

        public Screenshot EndGetScreenshot(IAsyncResult result) {
            var getScreenshot = result.AsyncState as Func<Rectangle, TimeSpan, Size?, ImageFormat, Screenshot>;
            if (getScreenshot != null)
                return getScreenshot.EndInvoke(result);
            return null;
        }

        public void SendScreenshotResponse(Screenshot screenshot) {
            if (_requestId != null && screenshot != null && screenshot.RequestId == _requestId.Value)
                if (_completeScreenshot != null)
                    _completeScreenshot(screenshot);
        }

        #endregion

        /// <summary>
        ///     Tell the client process to disconnect
        /// </summary>
        public void Disconnect() => SafeInvokeDisconnected();

        /// <summary>
        ///     Send a message to all handlers of <see cref="CaptureInterface.RemoteMessage" />.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Message(MessageType messageType, string format, params object[] args) => Message(messageType, string.Format(format, args));

        public void Message(MessageType messageType, string message) => SafeInvokeMessageRecevied(new MessageReceivedEventArgs(messageType, message));

        #endregion

        #region Private: Invoke message handlers

        private void SafeInvokeMessageRecevied(MessageReceivedEventArgs eventArgs) {
            if (RemoteMessage == null)
                return; //No Listeners

            MessageReceivedEvent listener = null;
            var dels = RemoteMessage.GetInvocationList();

            foreach (var del in dels)
                try {
                    listener = (MessageReceivedEvent) del;
                    listener.Invoke(eventArgs);
                } catch (Exception) {
                    //Could not reach the destination, so remove it
                    //from the list
                    RemoteMessage -= listener;
                }
        }

        private void SafeInvokeScreenshotRequested(ScreenshotRequest eventArgs) {
            if (ScreenshotRequested == null)
                return; //No Listeners

            ScreenshotRequestedEvent listener = null;
            var dels = ScreenshotRequested.GetInvocationList();

            foreach (var del in dels)
                try {
                    listener = (ScreenshotRequestedEvent) del;
                    listener.Invoke(eventArgs);
                } catch (Exception) {
                    //Could not reach the destination, so remove it
                    //from the list
                    ScreenshotRequested -= listener;
                }
        }

        private void SafeInvokeScreenshotReceived(ScreenshotReceivedEventArgs eventArgs) {
            if (ScreenshotReceived == null)
                return; //No Listeners

            ScreenshotReceivedEvent listener = null;
            var dels = ScreenshotReceived.GetInvocationList();

            foreach (var del in dels)
                try {
                    listener = (ScreenshotReceivedEvent) del;
                    listener.Invoke(eventArgs);
                } catch (Exception) {
                    //Could not reach the destination, so remove it
                    //from the list
                    ScreenshotReceived -= listener;
                }
        }

        private void SafeInvokeDisconnected() {
            if (Disconnected == null)
                return; //No Listeners

            DisconnectedEvent listener = null;
            var dels = Disconnected.GetInvocationList();

            foreach (var del in dels)
                try {
                    listener = (DisconnectedEvent) del;
                    listener.Invoke();
                } catch (Exception) {
                    //Could not reach the destination, so remove it
                    //from the list
                    Disconnected -= listener;
                }
        }

        #endregion
    }

    /// <summary>
    ///     Client event proxy for marshalling event handlers
    /// </summary>
    public class ClientCaptureInterfaceEventProxy : MarshalByRefObject {
        #region Lifetime Services

        public override object InitializeLifetimeService() =>
            //Returning null holds the object alive
            //until it is explicitly destroyed
            null;

        #endregion

        public void DisconnectedProxyHandler() {
            if (Disconnected != null)
                Disconnected();
        }

        public void ScreenshotRequestedProxyHandler(ScreenshotRequest request) {
            if (ScreenshotRequested != null)
                ScreenshotRequested(request);
        }

        #region Event Declarations

        /// <summary>
        ///     Client event used to communicate to the client that it is time to create a screenshot
        /// </summary>
        public event ScreenshotRequestedEvent ScreenshotRequested;

        /// <summary>
        ///     Client event used to notify the hook to exit
        /// </summary>
        public event DisconnectedEvent Disconnected;

        #endregion
    }
}
