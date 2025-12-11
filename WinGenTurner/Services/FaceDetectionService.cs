using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WinGenTurner.Services
{
    public enum GestureType
    {
        None,
        LookingUp,
        MouthOpen
    }

    public class FaceDetectionService : IDisposable
    {
        private CascadeClassifier? faceCascade;
        private CascadeClassifier? eyeCascade;
        private CascadeClassifier? smileCascade;
        private bool isInitialized;

        private const double LOOKING_UP_THRESHOLD = 0.35; // 顔の上部35%より上に目がある
        private const double SMILE_DETECTION_THRESHOLD = 1.3; // スマイル検出の閾値（幅/高さ比）

        public event Action<GestureType>? GestureDetected;
        public event Action<Mat>? ProcessedFrameReady;

        public bool Initialize()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // OpenCVの分類器データを読み込み
                faceCascade = new CascadeClassifier(System.IO.Path.Combine(baseDir, "haarcascade_frontalface_default.xml"));
                eyeCascade = new CascadeClassifier(System.IO.Path.Combine(baseDir, "haarcascade_eye.xml"));
                smileCascade = new CascadeClassifier(System.IO.Path.Combine(baseDir, "haarcascade_smile.xml"));

                isInitialized = !faceCascade.Empty() && !eyeCascade.Empty() && !smileCascade.Empty();
                
                if (!isInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("Cascadeファイルの読み込みに失敗:");
                    System.Diagnostics.Debug.WriteLine($"  顔: {!faceCascade.Empty()}");
                    System.Diagnostics.Debug.WriteLine($"  目: {!eyeCascade.Empty()}");
                    System.Diagnostics.Debug.WriteLine($"  スマイル: {!smileCascade.Empty()}");
                }

                return isInitialized;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初期化エラー: {ex.Message}");
                return false;
            }
        }

        public void ProcessFrame(Mat frame)
        {
            if (!isInitialized || frame.Empty())
                return;

            using var gray = new Mat();
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.EqualizeHist(gray, gray);

            var faces = faceCascade!.DetectMultiScale(gray, 1.1, 3, HaarDetectionTypes.ScaleImage, new Size(30, 30));

            foreach (var face in faces)
            {
                // 顔の矩形を描画
                Cv2.Rectangle(frame, face, Scalar.Green, 2);

                using var faceROI = new Mat(gray, face);

                // 目の検出
                var eyes = eyeCascade!.DetectMultiScale(faceROI, 1.1, 3);
                if (eyes.Length >= 2)
                {
                    // 目の位置を計算
                    var avgEyeY = eyes.Average(e => e.Y + e.Height / 2.0);
                    var faceTop = face.Y;
                    var faceHeight = face.Height;

                    // 目が顔の上部にあるかチェック（上を見ている）
                    var eyePosition = (avgEyeY - faceTop) / faceHeight;

                    foreach (var eye in eyes)
                    {
                        var eyeRect = new Rect(face.X + eye.X, face.Y + eye.Y, eye.Width, eye.Height);
                        Cv2.Rectangle(frame, eyeRect, Scalar.Blue, 2);
                    }

                    if (eyePosition < LOOKING_UP_THRESHOLD)
                    {
                        GestureDetected?.Invoke(GestureType.LookingUp);
                        Cv2.PutText(frame, "Looking up.", new Point(face.X, face.Y - 10),
                            HersheyFonts.HersheySimplex, 0.9, Scalar.Yellow, 2);
                    }
                }

                // スマイル/口の開きの検出（顔の下半分を対象）
                var mouthROI = new Rect(face.X, face.Y + (int)(face.Height * 0.5), 
                    face.Width, (int)(face.Height * 0.5));
                using var mouthArea = new Mat(gray, mouthROI);
                
                // スマイル検出（口が開いているかを検出）
                var smiles = smileCascade!.DetectMultiScale(mouthArea, 1.8, 20, HaarDetectionTypes.ScaleImage, new Size(25, 25));
                if (smiles.Length > 0)
                {
                    var smile = smiles[0];
                    var smileRect = new Rect(mouthROI.X + smile.X, mouthROI.Y + smile.Y, 
                        smile.Width, smile.Height);
                    Cv2.Rectangle(frame, smileRect, Scalar.Red, 2);

                    // スマイル検出された場合は口が開いていると判定
                    GestureDetected?.Invoke(GestureType.MouthOpen);
                    Cv2.PutText(frame, "Mouth open.", new Point(face.X, face.Y + face.Height + 25),
                        HersheyFonts.HersheySimplex, 0.9, Scalar.Yellow, 2);
                }
            }

            ProcessedFrameReady?.Invoke(frame);
        }

        public void Dispose()
        {
            faceCascade?.Dispose();
            eyeCascade?.Dispose();
            smileCascade?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
