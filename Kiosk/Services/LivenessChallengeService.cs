using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace FacePass.Kiosk.Services
{
    public enum LivenessChallenge
    {
        LookLeft,
        LookRight,
        Smile,
        Blink
    }

    /// <summary>
    /// Handles random liveness challenge selection and frame-by-frame validation.
    /// The ViewModel feeds each camera frame into EvaluateFrame() while the challenge is active.
    /// A TaskCompletionSource resolves the challenge result; a 5-second timeout auto-fails.
    /// </summary>
    public class LivenessChallengeService
    {
        private static readonly Random _rnd = new();

        // Threshold constants (tune per environment)
        private const double EAR_BLINK_THRESHOLD = 0.20;   // eye aspect ratio below this = blink
        private const double SMILE_RATIO_THRESHOLD = 2.0;   // mouth width/height above this = smile
        private const double HEAD_TURN_OFFSET_RATIO = 0.15; // % of face width deviation

        private readonly CascadeClassifier _eyeCascade;
        private TaskCompletionSource<bool>? _tcs;
        private CancellationTokenSource? _timeoutCts;
        private LivenessChallenge _current;
        private Rectangle _lastFaceRect;

        public LivenessChallengeService(string eyeCascadePath = "haarcascade_eye.xml")
        {
            _eyeCascade = new CascadeClassifier(eyeCascadePath);
        }

        public LivenessChallenge PickRandom()
        {
            var values = Enum.GetValues<LivenessChallenge>();
            _current = values[_rnd.Next(values.Length)];
            return _current;
        }

        /// <summary>
        /// Starts the 5-second liveness window. Returns true if passed, false if timed out.
        /// </summary>
        public Task<bool> BeginAsync(Rectangle initialFaceRect)
        {
            _lastFaceRect = initialFaceRect;
            _tcs = new TaskCompletionSource<bool>();
            _timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            _timeoutCts.Token.Register(() => _tcs.TrySetResult(false));
            return _tcs.Task;
        }

        /// <summary>
        /// Called on every camera frame while the challenge is active.
        /// Evaluates whether the current challenge condition is met.
        /// </summary>
        public void EvaluateFrame(Image<Bgr, byte> frame, Rectangle faceRect)
        {
            if (_tcs == null || _tcs.Task.IsCompleted) return;
            _lastFaceRect = faceRect;

            using var gray = frame.Convert<Gray, byte>();

            switch (_current)
            {
                case LivenessChallenge.LookLeft:
                    EvaluateHeadTurn(gray, faceRect, direction: -1);
                    break;

                case LivenessChallenge.LookRight:
                    EvaluateHeadTurn(gray, faceRect, direction: 1);
                    break;

                case LivenessChallenge.Smile:
                    EvaluateSmile(gray, faceRect);
                    break;

                case LivenessChallenge.Blink:
                    EvaluateBlink(gray, faceRect);
                    break;
            }
        }

        // ────────────────────────────────────────────────────
        // Challenge evaluators
        // ────────────────────────────────────────────────────

        /// <summary>
        /// Detects head turn by locating the brightest horizontal region (nose).
        /// A nose tip consistently offset from the face centre indicates a head turn.
        /// direction: -1 = left, +1 = right
        /// </summary>
        private void EvaluateHeadTurn(Image<Gray, byte> gray, Rectangle faceRect, int direction)
        {
            var face = gray.GetSubRect(faceRect).Resize(60, 60, Emgu.CV.CvEnum.Inter.Linear);
            // Project pixel intensities onto X axis
            double[] colSums = new double[60];
            for (int x = 0; x < 60; x++)
                for (int y = 20; y < 50; y++) // middle vertical band (nose region)
                    colSums[x] += face.Data[y, x, 0];

            // Find brightest column (nose tip)
            int brightest = 0;
            for (int x = 1; x < 60; x++)
                if (colSums[x] > colSums[brightest]) brightest = x;

            int centre = 30;
            double offset = (brightest - centre) / (double)centre; // ±1

            // direction -1 means nose tip should be in right half (face rotated left)
            if (direction == -1 && offset > HEAD_TURN_OFFSET_RATIO) Complete(true);
            if (direction == 1  && offset < -HEAD_TURN_OFFSET_RATIO) Complete(true);
        }

        /// <summary>
        /// Detects a smile by checking the width-to-height ratio of the lower face region.
        /// A smile widens the mouth region significantly.
        /// </summary>
        private void EvaluateSmile(Image<Gray, byte> gray, Rectangle faceRect)
        {
            // Crop lower third of face (mouth region)
            var mouthRect = new Rectangle(
                faceRect.X,
                faceRect.Y + faceRect.Height * 2 / 3,
                faceRect.Width,
                faceRect.Height / 3);

            if (mouthRect.Bottom > gray.Height || mouthRect.Right > gray.Width) return;

            var mouth = gray.GetSubRect(mouthRect);
            // Threshold to get bright pixels (teeth)
            var thresh = mouth.ThresholdBinary(new Gray(180), new Gray(255));
            int whitePixels = CvInvoke.CountNonZero(thresh);

            // If white pixel count is high relative to region, smile is detected
            double ratio = whitePixels / (double)(mouthRect.Width * mouthRect.Height);
            if (ratio > 0.08) Complete(true); // ~8% bright pixels = smile
        }

        /// <summary>
        /// Detects a blink using Haar eye cascade.
        /// A blink causes the eye detector to fail briefly.
        /// </summary>
        private int _eyePresentFrames = 0;
        private int _eyeAbsentFrames = 0;

        private void EvaluateBlink(Image<Gray, byte> gray, Rectangle faceRect)
        {
            var face = gray.GetSubRect(faceRect);
            var eyes = _eyeCascade.DetectMultiScale(face, 1.1, 5, new Size(15, 15));

            if (eyes.Length >= 2)
            {
                _eyePresentFrames++;
                _eyeAbsentFrames = 0;
            }
            else
            {
                if (_eyePresentFrames >= 3) // eyes were visible before
                    _eyeAbsentFrames++;

                // Eyes disappeared for 2+ frames after being present = blink
                if (_eyePresentFrames >= 3 && _eyeAbsentFrames >= 2)
                    Complete(true);
            }
        }

        private void Complete(bool result)
        {
            _tcs?.TrySetResult(result);
            _timeoutCts?.Cancel();
            // Reset blink counters
            _eyePresentFrames = 0;
            _eyeAbsentFrames = 0;
        }

        public void Cancel() => Complete(false);
    }
}
