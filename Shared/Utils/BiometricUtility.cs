using System;
using System.IO;

namespace FacePass.Shared.Utils
{
    public static class BiometricUtility
    {
        /// <summary>
        /// Serializes a float array (128-d encoding) into a byte array for Supabase storage.
        /// </summary>
        public static byte[] SerializeEncoding(float[] encoding)
        {
            if (encoding == null) return Array.Empty<byte>();

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(encoding.Length);
                    foreach (var val in encoding)
                    {
                        writer.Write(val);
                    }
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Deserializes a byte array from Supabase back into a float array.
        /// </summary>
        public static float[] DeserializeEncoding(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<float>();

            using (var ms = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(ms))
                {
                    int length = reader.ReadInt32();
                    float[] encoding = new float[length];
                    for (int i = 0; i < length; i++)
                    {
                        encoding[i] = reader.ReadSingle();
                    }
                    return encoding;
                }
            }
        }
    }
}
