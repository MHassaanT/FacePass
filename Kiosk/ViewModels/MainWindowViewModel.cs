using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV.Structure;
using FacePass.Kiosk.Services;

namespace FacePass.Kiosk.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        // ── Services ──────────────────────────────────────────────────────────
        private readonly CameraService _camera;
        private readonly FaceDetectionService _detector;
        private readonly FaceEncodingService _encoder;
        private readonly SupabaseFaceRepository _faceRepo;
        private readonly LivenessChallengeService _liveness;
        private readonly QrSessionService _qrService;
        private readonly AttendanceService _attendance;
        private readonly Dispatcher _ui;

        // ── Config ────────────────────────────────────────────────────────────
        private readonly long _classroomId;
        private readonly long _courseId;

        // ── State ─────────────────────────────────────────────────────────────
        private bool _awaitingLiveness;
        private Rectangle _currentFaceRect;

        // ── FIX: Guard flag so we don't fire a new HTTP recognition call on
        //    every single camera frame. Without this, 30 concurrent Supabase
        //    requests per second are launched the moment a face is visible.
        private bool _isRecognizing;

        // ── Bindable properties ───────────────────────────────────────────────
        private BitmapSource? _cameraFrame;
        public BitmapSource? CameraFrame
        {
            get => _cameraFrame;
            set { _cameraFrame = value; OnPropertyChanged(); }
        }

        private BitmapSource? _qrSource;
        public BitmapSource? QrSource
        {
            get => _qrSource;
            set { _qrSource = value; OnPropertyChanged(); }
        }

        private Rect _faceRect;
        public Rect FaceRect
        {
            get => _faceRect;
            set { _faceRect = value; OnPropertyChanged(); }
        }

        private bool _faceVisible;
        public bool FaceVisible
        {
            get => _faceVisible;
            set { _faceVisible = value; OnPropertyChanged(); }
        }

        private string _challengeText = string.Empty;
        public string ChallengeText
        {
            get => _challengeText;
            set { _challengeText = value; OnPropertyChanged(); }
        }

        private bool _challengeVisible;
        public bool ChallengeVisible
        {
            get => _challengeVisible;
            set { _challengeVisible = value; OnPropertyChanged(); }
        }

        private string _bannerText = string.Empty;
        public string BannerText
        {
            get => _bannerText;
            set { _bannerText = value; OnPropertyChanged(); }
        }

        private bool _bannerVisible;
        public bool BannerVisible
        {
            get => _bannerVisible;
            set { _bannerVisible = value; OnPropertyChanged(); }
        }

        private bool _qrVisible;
        public bool QrVisible
        {
            get => _qrVisible;
            set { _qrVisible = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "Initializing...";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isStatusVisible = true;
        public bool IsStatusVisible
        {
            get => _isStatusVisible;
            set { _isStatusVisible = value; OnPropertyChanged(); }
        }

        // ── Constructor ───────────────────────────────────────────────────────
        public MainWindowViewModel(
            CameraService camera,
            FaceDetectionService detector,
            FaceEncodingService encoder,
            SupabaseFaceRepository faceRepo,
            LivenessChallengeService liveness,
            QrSessionService qrService,
            AttendanceService attendance,
            long classroomId,
            long courseId)
        {
            _camera = camera;
            _detector = detector;
            _encoder = encoder;
            _faceRepo = faceRepo;
            _liveness = liveness;
            _qrService = qrService;
            _attendance = attendance;
            _classroomId = classroomId;
            _courseId = courseId;
            _ui = Dispatcher.CurrentDispatcher;

            _camera.FrameReady += OnFrameReady;

            // QR refresh loop — every 30 seconds
            var qrTimer = new DispatcherTimer(DispatcherPriority.Background, _ui)
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            qrTimer.Tick += async (_, _) => await RefreshQrAsync();
            qrTimer.Start();

            _ = RefreshQrAsync();

            if (!_camera.IsOpened)
            {
                StatusMessage = "⚠️ Camera Error: No device detected. Check connection.";
                IsStatusVisible = true;
            }
            else
            {
                StatusMessage = "System Ready — Please look at the camera";
                // Hide status after 5 seconds if everything is fine
                _ = Task.Delay(5000).ContinueWith(_ => _ui.Invoke(() => IsStatusVisible = false));
            }
        }

        // ── QR Refresh ────────────────────────────────────────────────────────
        private async Task RefreshQrAsync()
        {
            try
            {
                var (bitmap, _, _) = await _qrService.CreateSessionAsync();
                QrSource = ConvertBitmap(bitmap);
                QrVisible = true;
                
                if (StatusMessage.Contains("Offline") || StatusMessage.Contains("Error"))
                {
                    StatusMessage = "Connection Restored";
                    _ = Task.Delay(3000).ContinueWith(_ => _ui.Invoke(() => IsStatusVisible = false));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QR] Critical Error: {ex.Message}");
                
                string errorDetail = ex.Message;
                if (errorDetail.Contains("401") || errorDetail.Contains("403"))
                    errorDetail = "Invalid API Key/AnonKey";
                else if (errorDetail.Contains("404"))
                    errorDetail = "Table 'qr_sessions' not found";
                
                StatusMessage = $"⚠️ Supabase Error: {errorDetail}";
                IsStatusVisible = true;
            }
        }

        private int _frameSkipCounter = 0;

        // ── Camera frame handler ──────────────────────────────────────────────
        private void OnFrameReady(object? sender, Emgu.CV.Image<Bgr, byte> img)
        {
            try
            {
                // 1. UI Preview: Clone and pass ownership to the UI lambda
                var uiClone = img.Clone();
                _ui.BeginInvoke(() =>
                {
                    using (uiClone)
                    {
                        try { CameraFrame = ConvertToBitmapSource(uiClone); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[UI] {ex.Message}"); }
                    }
                });

                // 2. Analysis: Throttled background processing
                if (++_frameSkipCounter % 5 == 0)
                {
                    var analysisClone = img.Clone();
                    int matWidth = analysisClone.Width;
                    int matHeight = analysisClone.Height;

                    Task.Run(() =>
                    {
                        // Background thread: Detect faces
                        Rectangle[] faces;
                        try { faces = _detector.DetectFaces(analysisClone); }
                        catch (Exception ex) 
                        { 
                            System.Diagnostics.Debug.WriteLine($"[Detect] {ex.Message}"); 
                            analysisClone.Dispose();
                            return; 
                        }

                        // Pass results back to UI thread
                        _ui.BeginInvoke(() =>
                        {
                            using (analysisClone) 
                            {
                                if (faces.Length == 0)
                                {
                                    FaceVisible = false;
                                    return;
                                }

                                var face = faces[0];
                                
                                // --- ACCURATE UNIFORM-TO-FILL SCALING ---
                                var window = System.Windows.Application.Current.MainWindow;
                                if (window != null)
                                {
                                    double viewW = window.ActualWidth;
                                    double viewH = window.ActualHeight;
                                    
                                    double ratioX = viewW / matWidth;
                                    double ratioY = viewH / matHeight;
                                    double scale = Math.Max(ratioX, ratioY);

                                    double offsetX = (viewW - (matWidth * scale)) / 2;
                                    double offsetY = (viewH - (matHeight * scale)) / 2;

                                    FaceRect = new Rect(
                                        (face.X * scale) + offsetX, 
                                        (face.Y * scale) + offsetY, 
                                        face.Width * scale, 
                                        face.Height * scale);
                                }

                                FaceVisible = true;
                                _currentFaceRect = face;

                                if (_awaitingLiveness)
                                {
                                    _liveness.EvaluateFrame(analysisClone, face);
                                }
                                else if (!_isRecognizing)
                                {
                                    _isRecognizing = true;
                                    _ = RecognizeAndProcessAsync(analysisClone.Clone(), face);
                                }
                            }
                        });
                    });
                }
            }
            finally
            {
                img.Dispose(); // Dispose original frame
            }
        }

        // ── Recognition → Liveness → Attendance pipeline ─────────────────────
        private async Task RecognizeAndProcessAsync(
            Emgu.CV.Image<Bgr, byte> frame, Rectangle faceRect)
        {
            try
            {
                var grayFace = _encoder.ExtractFace(frame, faceRect);
                var encoding = _encoder.GetEncoding(grayFace);

                var stored = await _faceRepo.GetAllEncodingsAsync();
                var match = FindClosestMatch(stored, encoding);

                if (match == null)
                {
                    // No match — reset so the next frame can try again
                    _isRecognizing = false;
                    frame.Dispose();
                    return;
                }

                _awaitingLiveness = true;
                var challenge = _liveness.PickRandom();

                _ui.Invoke(() =>
                {
                    ChallengeText = FormatChallenge(challenge);
                    ChallengeVisible = true;
                });

                var passed = await _liveness.BeginAsync(faceRect);

                _ui.Invoke(() =>
                {
                    ChallengeVisible = false;
                    _awaitingLiveness = false;
                });

                if (!passed)
                {
                    await _attendance.LogAsync(
                        match.Value, _courseId, _classroomId,
                        method: "face", status: "suspicious",
                        flaggedReason: "Liveness challenge failed or timed out");
                }
                else
                {
                    await _attendance.LogAsync(
                        match.Value, _courseId, _classroomId,
                        method: "face", status: "present");

                    var name = await _faceRepo.GetStudentNameAsync(match.Value);
                    _ui.Invoke(() => ShowBanner($"✅  Welcome, {name}!"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Recognition] Error: {ex.Message}");
                _ui.Invoke(() =>
                {
                    _awaitingLiveness = false;
                    ChallengeVisible = false;
                });
            }
            finally
            {
                frame.Dispose();
                _isRecognizing = false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static long? FindClosestMatch(Dictionary<long, byte[]> stored, byte[] live)
        {
            const double Threshold = 0.6;
            long? best = null;
            double bestDist = double.MaxValue;

            foreach (var kv in stored)
            {
                var dist = FaceEncodingService.EuclideanDistance(kv.Value, live);
                if (dist < Threshold && dist < bestDist)
                {
                    bestDist = dist;
                    best = kv.Key;
                }
            }
            return best;
        }

        private static string FormatChallenge(LivenessChallenge c) => c switch
        {
            LivenessChallenge.LookLeft => "👈  Please look LEFT",
            LivenessChallenge.LookRight => "👉  Please look RIGHT",
            LivenessChallenge.Smile => "😊  Please SMILE",
            LivenessChallenge.Blink => "👁  Please BLINK",
            _ => string.Empty
        };

        private void ShowBanner(string message)
        {
            BannerText = message;
            BannerVisible = true;

            var timer = new DispatcherTimer(DispatcherPriority.Normal, _ui)
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (_, _) =>
            {
                BannerVisible = false;
                timer.Stop();
            };
            timer.Start();
        }

        private static BitmapSource ConvertBitmap(Bitmap bmp)
        {
            var hBitmap = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        private static BitmapSource ConvertToBitmapSource(Emgu.CV.Image<Bgr, byte> img)
        {
            var mat = img.Mat;
            int width = mat.Width;
            int height = mat.Height;
            int stride = mat.Step;

            // Use WriteableBitmap to ensure we have a safe, UI-owned copy of the pixels
            var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
            bitmap.Lock();
            try
            {
                // Copy pixels from native Mat buffer to WriteableBitmap back buffer
                unsafe
                {
                    Buffer.MemoryCopy(
                        (void*)mat.DataPointer,
                        (void*)bitmap.BackBuffer,
                        bitmap.BackBufferStride * height,
                        stride * height);
                }
                bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                bitmap.Unlock();
            }
            bitmap.Freeze();
            return bitmap;
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        // ── INotifyPropertyChanged ────────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── IDisposable ───────────────────────────────────────────────────────
        public void Dispose()
        {
            _camera.FrameReady -= OnFrameReady;
            _camera.Dispose();
            _encoder.Dispose();
        }
    }
}