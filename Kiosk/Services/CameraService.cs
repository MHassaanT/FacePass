using System;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Timers;

namespace FacePass.Kiosk.Services
{
    public class CameraService : IDisposable
    {
        private readonly VideoCapture _capture;
        private readonly System.Timers.Timer _frameTimer;
        public event EventHandler<Image<Bgr, byte>>? FrameReady;

        public CameraService(int deviceIndex = 0, double fps = 30)
        {
            _capture = new VideoCapture(deviceIndex);
            _capture.ImageGrabbed += OnImageGrabbed;

            _frameTimer = new System.Timers.Timer(1000 / fps) { AutoReset = true };
            _frameTimer.Elapsed += (s, e) => _capture.Grab();
            _frameTimer.Start();
            _capture.Start();
        }

        private void OnImageGrabbed(object? sender, EventArgs e)
        {
            var mat = new Mat();
            _capture.Retrieve(mat);
            var img = mat.ToImage<Bgr, byte>();
            FrameReady?.Invoke(this, img);
        }

        public void Dispose()
        {
            _frameTimer?.Stop();
            _capture?.Dispose();
            _frameTimer?.Dispose();
        }
    }
}
