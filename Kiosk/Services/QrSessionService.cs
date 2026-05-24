using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QRCoder;

namespace FacePass.Kiosk.Services
{
    /// <summary>
    /// Creates a 30-second QR session: inserts a row into Supabase (Supabase generates
    /// session_guid as a default UUID column), then generates a QR code bitmap encoding
    /// the returned session data as JSON.
    /// </summary>
    public class QrSessionService
    {
        private readonly SupabaseFaceRepository _repo;
        private readonly long _classroomId;

        public QrSessionService(SupabaseFaceRepository repo, long classroomId)
        {
            _repo = repo;
            _classroomId = classroomId;
        }

        /// <summary>
        /// Inserts a new QR session into Supabase and returns the rendered QR bitmap.
        /// The session expires in 2 minutes.
        /// </summary>
        public async Task<(Bitmap qrBitmap, string sessionGuid, DateTime expiresAt)> CreateSessionAsync()
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(2);
            var row = await _repo.InsertQrSessionAsync(_classroomId, expiresAt);

            if (row == null)
                throw new InvalidOperationException("Supabase did not return the created QR session row.");

            var sessionGuid = row["session_guid"]!.ToString();
            var classroomId = _classroomId.ToString();
            var expiresStr  = expiresAt.ToString("o");

            var payload = JsonConvert.SerializeObject(new
            {
                session_guid = sessionGuid,
                classroom_id = classroomId,
                expires_at   = expiresStr
            });

            var bitmap = GenerateQrBitmap(payload);
            return (bitmap, sessionGuid, expiresAt);
        }

        private static Bitmap GenerateQrBitmap(string payload)
        {
            using var generator = new QRCodeGenerator();
            var data   = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCode(data);
            return qrCode.GetGraphic(pixelsPerModule: 6, darkColorHtmlHex: "#1A1A1A", lightColorHtmlHex: "#FFFFFF");
        }

        /// <summary>
        /// Converts a System.Drawing.Bitmap to a byte array (PNG) for binding to WPF ImageSource.
        /// </summary>
        public static byte[] BitmapToPngBytes(Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }
}
