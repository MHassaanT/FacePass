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
    /// <summary>
    /// Main ViewModel for the Kiosk window.
    /// Orchestrates the camera, face detection, recognition, liveness, QR and attendance pipeline.
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
    {
        // ── Services ─────────────────────────────────────────────────────────
        private readonly CameraService          _camera;
        private readonly FaceDetectionService   _detector;
        private readonly FaceEncodingService    _encoder;
        private readonly SupabaseFaceRepository _faceRepo;
        private readonly LivenessChallengeService _liveness;
        private readonly QrSessionService       _qrService;
        private readonly AttendanceService      _attendance;
        private readonly Dispatcher             _ui;

        // ── Config (populated from AppConfig) ────────────────────────────────
        private readonly Guid _classroomId;
        private readonly Guid _courseId;

        // ── State ─────────────────────────────────────────────────────────────
        private bool _awaitingLiveness;
        private Rectangle _currentFaceRect;

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

        // ── Constructor ───────────────────────────────────────────────────────
        public MainWindowViewModel(
            CameraService camera,
            FaceDetectionService detector,
            FaceEncodingService encoder,
            SupabaseFaceRepository faceRepo,
            LivenessChallengeService liveness,
            QrSessionService qrService,
            AttendanceService attendance,
            Guid classroomId,
            Guid courseId)
        {
            _camera     = camera;
            _detector   = detector;
            _encoder    = encoder;
            _faceRepo   = faceRepo;
            _liveness   = liveness;
            _qrService  = qrService;
            _attendance = attendance;
            _classroomId = classroomId;
            _courseId    = courseId;
            _ui         = Dispatcher.CurrentDispatcher;

            _camera.FrameReady += OnFrameReady;

            // Start QR refresh loop
            var qrTimer = new DispatcherTimer(DispatcherPriority.Background, _ui)
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            qrTimer.Tick += async (_, _) => await RefreshQrAsync();
            qrTimer.Start();

            // Generate first QR immediately
            _ = RefreshQrAsync();
        }

        // ── QR Refresh ────────────────────────────────────────────────────────
        private async Task RefreshQrAsync()
        {
            try
            {
                var (bitmap, _, _) = await _qrService.CreateSessionAsync();
                var src = ConvertBitmap(bitmap);
                _ui.Invoke(() =>
                {
                    QrSource  = src;
                    QrVisible = true;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[QR] Error: {ex.Message}");
            }
        }

        // ── Camera frame handler ──────────────────────────────────────────────
        private void OnFrameReady(object? sender, Emgu.CV.Image<Bgr, byte> img)
        {
            _ui.BeginInvoke(async () =>
            {
                // Display live video
                CameraFrame = ConvertToBitmapSource(img);

                // Detect faces
                var faces = _detector.DetectFaces(img);
                if (faces.Length == 0)
                {
                    FaceVisible = false;
                    return;
                }

                var face = faces[0];
                FaceRect    = new Rect(face.X, face.Y, face.Width, face.Height);
                FaceVisible = true;
                _currentFaceRect = face;

                if (_awaitingLiveness)
                {
                    // Feed frame to liveness evaluator
                    _liveness.EvaluateFrame(img, face);
                }
                else
                {
                    await RecognizeAndProcessAsync(img, face);
                }
            });
        }

        // ── Recognition → Liveness → Attendance pipeline ─────────────────────
        private async Task RecognizeAndProcessAsync(
            Emgu.CV.Image<Bgr, byte> frame, Rectangle faceRect)
        {
            try
            {
                // 1. Extract encoding
                var grayFace = _encoder.ExtractFace(frame, faceRect);
                var encoding = _encoder.GetEncoding(grayFace);

                // 2. Fetch stored encodings and match
                var stored = await _faceRepo.GetAllEncodingsAsync();
                var match  = FindClosestMatch(stored, encoding);
                if (match == null) return; // no match – do nothing

                // 3. Start liveness challenge
                _awaitingLiveness = true;
                var challenge = _liveness.PickRandom();
                ChallengeText    = FormatChallenge(challenge);
                ChallengeVisible = true;

                var passed = await _liveness.BeginAsync(faceRect);

                ChallengeVisible = false;
                _awaitingLiveness = false;

                if (!passed)
                {
                    await _attendance.LogAsync(
                        match.Value, _courseId, _classroomId,
                        method: "face", status: "suspicious",
                        flaggedReason: "Liveness challenge failed or timed out");
                    return;
                }

                // 4. Log attendance as present
                await _attendance.LogAsync(
                    match.Value, _courseId, _classroomId,
                    method: "face", status: "present");

                // 5. Show welcome banner
                var name = await _faceRepo.GetStudentNameAsync(match.Value);
                ShowBanner($"✅  Welcome, {name}!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Recognition] Error: {ex.Message}");
                _awaitingLiveness = false;
                ChallengeVisible  = false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Guid? FindClosestMatch(Dictionary<Guid, byte[]> stored, byte[] live)
        {
            const double Threshold = 0.6;
            Guid? best = null;
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
            LivenessChallenge.LookLeft  => "👈  Please look LEFT",
            LivenessChallenge.LookRight => "👉  Please look RIGHT",
            LivenessChallenge.Smile     => "😊  Please SMILE",
            LivenessChallenge.Blink     => "👁  Please BLINK",
            _ => string.Empty
        };

        private void ShowBanner(string message)
        {
            BannerText    = message;
            BannerVisible = true;

            var timer = new DispatcherTimer(DispatcherPriority.Normal, _ui) { Interval = TimeSpan.FromSeconds(3) };
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
            return BitmapSource.Create(
                mat.Width, mat.Height,
                96, 96,
                System.Windows.Media.PixelFormats.Bgr24,
                null,
                mat.DataPointer,
                mat.Step * mat.Height,
                mat.Step);
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
