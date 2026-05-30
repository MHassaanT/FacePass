using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using FacePass.Management.Services;
using Newtonsoft.Json.Linq;

namespace FacePass.Management.Views
{
    public partial class BiometricRegistrationDialog : Window
    {
        private readonly long _studentId;
        private VideoCapture? _capture;
        private CascadeClassifier? _faceCascade;
        private DispatcherTimer? _timer;

        private Emgu.CV.Image<Bgr, byte>? _latestFrame;
        private System.Drawing.Rectangle _latestFaceRect;

        public BiometricRegistrationDialog(long studentId, string studentName)
        {
            InitializeComponent();
            _studentId = studentId;
            StudentNameText.Text = studentName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _capture = new VideoCapture(0);
                string cascadePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "haarcascade_frontalface_default.xml");
                if (!File.Exists(cascadePath)) cascadePath = "haarcascade_frontalface_default.xml";
                _faceCascade = new CascadeClassifier(cascadePath);

                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(33) // ~30 fps
                };
                _timer.Tick += Timer_Tick;
                _timer.Start();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Camera error: {ex.Message}";
                CaptureBtn.IsEnabled = false;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_capture == null || !_capture.IsOpened) return;

            using var frame = _capture.QueryFrame();
            if (frame == null || frame.IsEmpty) return;

            var imageFrame = frame.ToImage<Bgr, byte>();
            _latestFrame?.Dispose();
            _latestFrame = imageFrame.Clone();

            using var grayFrame = imageFrame.Convert<Gray, byte>();
            var faces = _faceCascade?.DetectMultiScale(grayFrame, 1.1, 10, System.Drawing.Size.Empty);

            if (faces != null && faces.Length > 0)
            {
                _latestFaceRect = faces[0];
                DrawFaceBoundingBox(_latestFaceRect, imageFrame.Width, imageFrame.Height);
            }
            else
            {
                _latestFaceRect = System.Drawing.Rectangle.Empty;
                FaceBoundingBox.Visibility = Visibility.Collapsed;
            }

            CameraFeed.Source = ConvertToBitmapSource(imageFrame);
        }

        private void DrawFaceBoundingBox(System.Drawing.Rectangle face, int matWidth, int matHeight)
        {
            double viewW = CameraFeed.ActualWidth;
            double viewH = CameraFeed.ActualHeight;

            if (viewW == 0 || viewH == 0) return;

            double ratioX = viewW / matWidth;
            double ratioY = viewH / matHeight;
            double scale = Math.Max(ratioX, ratioY);

            double offsetX = (viewW - (matWidth * scale)) / 2;
            double offsetY = (viewH - (matHeight * scale)) / 2;

            FaceBoundingBox.Width = face.Width * scale;
            FaceBoundingBox.Height = face.Height * scale;
            System.Windows.Controls.Canvas.SetLeft(FaceBoundingBox, (face.X * scale) + offsetX);
            System.Windows.Controls.Canvas.SetTop(FaceBoundingBox, (face.Y * scale) + offsetY);
            FaceBoundingBox.Visibility = Visibility.Visible;
        }

        private async void CaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_latestFrame == null || _latestFaceRect == System.Drawing.Rectangle.Empty)
            {
                StatusText.Text = "⚠️ No face detected. Please look at the camera.";
                return;
            }

            CaptureBtn.IsEnabled = false;
            StatusText.Text = "Processing...";

            try
            {
                // Extract face ROI, resize to 100x100 grayscale
                using var gray = _latestFrame.Convert<Gray, byte>();
                using var face = gray.GetSubRect(_latestFaceRect).Resize(100, 100, Emgu.CV.CvEnum.Inter.Nearest);
                
                // Serialize to byte[]
                byte[] encodingBytes;
                using (var ms = new MemoryStream())
                {
                    using var bmp = face.ToBitmap();
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    encodingBytes = ms.ToArray();
                }

                string hexEncoding = "\\x" + Convert.ToHexString(encodingBytes);

                var payload = new JObject
                {
                    ["student_id"] = _studentId,
                    ["vector_data_bytea"] = hexEncoding
                }; 

                using var client = SupabaseRestClient.Create();
                var request = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseRestClient.BaseUrl}/rest/v1/FACE_ENCODINGS")
                {
                    Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Prefer", "return=representation");

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                StatusText.Text = "✅ Biometric registered successfully!";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00E676"));
                
                await Task.Delay(1500);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"❌ Error: {ex.Message}";
                CaptureBtn.IsEnabled = true;
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _timer?.Stop();
            _capture?.Dispose();
            _faceCascade?.Dispose();
            _latestFrame?.Dispose();
        }

        private static BitmapSource ConvertToBitmapSource(Emgu.CV.Image<Bgr, byte> img)
        {
            var mat = img.Mat;
            int width = mat.Width;
            int height = mat.Height;
            int stride = mat.Step;

            var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
            bitmap.Lock();
            try
            {
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
    }
}
