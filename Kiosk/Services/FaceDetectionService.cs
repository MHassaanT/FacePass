using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace FacePass.Kiosk.Services
{
    public class FaceDetectionService
    {
        private readonly CascadeClassifier _faceCascade;

        public FaceDetectionService(string cascadePath = "haarcascade_frontalface_default.xml")
        {
            _faceCascade = new CascadeClassifier(cascadePath);
        }

        public Rectangle[] DetectFaces(Image<Bgr, byte> frame)
        {
            using var gray = frame.Convert<Gray, byte>();
            var faces = _faceCascade.DetectMultiScale(
                gray,
                scaleFactor: 1.1,
                minNeighbors: 5,
                minSize: new Size(60, 60));
            return faces;
        }
    }
}
