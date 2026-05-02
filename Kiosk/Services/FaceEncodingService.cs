using System;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;

namespace FacePass.Kiosk.Services
{
    /// <summary>
    /// Handles face extraction and byte encoding for LBPH-based recognition.
    /// Face images are resized to 100x100 grayscale and serialized to byte[],
    /// which is stored as BYTEA in Supabase and compared via Euclidean distance.
    /// </summary>
    public class FaceEncodingService : IDisposable
    {
        private bool _disposed;

        private const int FaceWidth = 100;
        private const int FaceHeight = 100;

        /// <summary>
        /// Extracts and normalizes a face ROI from the given frame.
        /// </summary>
        public Image<Gray, byte> ExtractFace(Image<Bgr, byte> frame, Rectangle faceRect)
        {
            using var gray = frame.Convert<Gray, byte>();
            var face = gray.GetSubRect(faceRect)
                           .Resize(FaceWidth, FaceHeight, Emgu.CV.CvEnum.Inter.Nearest);
            return face;
        }

        /// <summary>
        /// Serializes a normalized grayscale face image to a byte array for Supabase storage.
        /// </summary>
        public byte[] GetEncoding(Image<Gray, byte> face)
        {
            // Serialize via MemoryStream so it can be stored as BYTEA (VARBINARY)
            using var ms = new MemoryStream();
            using var bmp = face.ToBitmap();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return ms.ToArray();
        }

        /// <summary>
        /// Computes Euclidean distance between two byte encodings.
        /// Threshold < 0.6 is a positive match.
        /// </summary>
        public static double EuclideanDistance(byte[] a, byte[] b)
        {
            double sum = 0;
            int len = Math.Min(a.Length, b.Length);
            for (int i = 0; i < len; i++)
                sum += Math.Pow(a[i] - b[i], 2);
            return Math.Sqrt(sum) / len; // normalise by length
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
