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
        private bool _disposed;

        public bool IsOpened => _capture.IsOpened;

        public event EventHandler<Image<Bgr, byte>>? FrameReady;

        public CameraService(int deviceIndex = 0, double fps = 30)
        {
            // ── FIX: Do NOT call _capture.Start() or subscribe to ImageGrabbed.
            // The original code used BOTH Start() (continuous background loop) AND
            // a timer calling Grab() — these conflict and crash the VideoCapture
            // after ~2 seconds. Use only the timer + Read() approach instead.
            _capture = new VideoCapture(deviceIndex);

            _frameTimer = new System.Timers.Timer(1000.0 / fps) { AutoReset = true };
            _frameTimer.Elapsed += OnTimerElapsed;
            _frameTimer.Start();
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_disposed) return;
            try
            {
                var mat = new Mat();
                // Read() = Grab() + Retrieve() in one call, thread-safe for our timer
                if (_capture.Read(mat) && !mat.IsEmpty)
                {
                    // ToImage copies pixel data so the caller owns the object safely
                    FrameReady?.Invoke(this, mat.ToImage<Bgr, byte>());
                }
                mat.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Camera] Frame error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _frameTimer.Stop();
            _frameTimer.Dispose();
            _capture.Dispose();
        }
    }
}