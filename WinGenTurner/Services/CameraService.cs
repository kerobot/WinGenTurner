using OpenCvSharp;

namespace WinGenTurner.Services
{
    public class CameraService : IDisposable
    {
        private VideoCapture? capture;
        private bool isRunning;
        private Task? captureTask;
        private CancellationTokenSource? cancellationTokenSource;

        public event Action<Mat>? FrameCaptured;
        public event Action<string>? ErrorOccurred;
        public event Action<bool>? StatusChanged;

        public bool IsRunning => isRunning;

        public bool Start(int cameraIndex = 0)
        {
            if (isRunning)
                return true;

            try
            {
                capture = OpenCamera(cameraIndex);

                if (capture == null || !capture.IsOpened())
                {
                    var errorMsg = $"カメラ {cameraIndex} を開けませんでした。\n" +
                                   "別のアプリケーションがカメラを使用していないか確認してください。";
                    ErrorOccurred?.Invoke(errorMsg);
                    System.Diagnostics.Debug.WriteLine(errorMsg);
                    capture?.Dispose();
                    capture = null;
                    return false;
                }

                // カメラ設定
                ConfigureCamera(capture);

                isRunning = true;
                cancellationTokenSource = new CancellationTokenSource();
                captureTask = Task.Run(() => CaptureLoop(cancellationTokenSource.Token));
                
                StatusChanged?.Invoke(true);
                System.Diagnostics.Debug.WriteLine("カメラ起動成功");
                return true;
            }
            catch (Exception ex)
            {
                var errorMsg = $"カメラ起動エラー: {ex.Message}\n{ex.StackTrace}";
                ErrorOccurred?.Invoke(errorMsg);
                System.Diagnostics.Debug.WriteLine(errorMsg);
                
                capture?.Dispose();
                capture = null;
                return false;
            }
        }

        public void Stop()
        {
            if (!isRunning)
                return;

            System.Diagnostics.Debug.WriteLine("カメラ停止中...");
            isRunning = false;
            
            try
            {
                cancellationTokenSource?.Cancel();
                
                // タスクの完了を待つ(タイムアウト付き)
                if (captureTask != null && !captureTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    System.Diagnostics.Debug.WriteLine("カメラ停止がタイムアウトしました");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"カメラ停止エラー: {ex.Message}");
            }
            finally
            {
                try
                {
                    capture?.Release();
                    capture?.Dispose();
                    capture = null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"カメラリソース解放エラー: {ex.Message}");
                }

                StatusChanged?.Invoke(false);
                System.Diagnostics.Debug.WriteLine("カメラ停止完了");
            }
        }

        private void CaptureLoop(CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.WriteLine("カメラキャプチャループ開始");
            using var frame = new Mat();
            int errorCount = 0;
            const int maxErrors = 10;

            while (!cancellationToken.IsCancellationRequested && capture != null && isRunning)
            {
                try
                {
                    if (!capture.IsOpened())
                    {
                        System.Diagnostics.Debug.WriteLine("カメラが閉じられました");
                        ErrorOccurred?.Invoke("カメラが予期せず閉じられました");
                        break;
                    }

                    bool success = capture.Read(frame);
                    
                    if (!success || frame.Empty())
                    {
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"フレーム読み取り失敗 ({errorCount}/{maxErrors})");
                        
                        if (errorCount >= maxErrors)
                        {
                            ErrorOccurred?.Invoke("連続してフレーム取得に失敗しました");
                            break;
                        }
                        
                        Thread.Sleep(100);
                        continue;
                    }

                    errorCount = 0; // 成功したらエラーカウントをリセット
                    FrameCaptured?.Invoke(frame.Clone());
                    Thread.Sleep(33); // 約30fps
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"フレーム取得エラー: {ex.Message}");
                    ErrorOccurred?.Invoke($"フレーム取得エラー: {ex.Message}");
                    
                    errorCount++;
                    if (errorCount >= maxErrors)
                    {
                        break;
                    }
                    
                    Thread.Sleep(100);
                }
            }

            System.Diagnostics.Debug.WriteLine("カメラキャプチャループ終了");
        }

        public void Dispose()
        {
            Stop();
            cancellationTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }

        // カメラを複数のバックエンドで試行して開く
        private static VideoCapture? OpenCamera(int cameraIndex)
        {
            var backends = new[]
            {
                VideoCaptureAPIs.DSHOW,
                VideoCaptureAPIs.MSMF,
                VideoCaptureAPIs.ANY
            };

            foreach (var backend in backends)
            {
                try
                {
                    var cap = new VideoCapture(cameraIndex, backend);
                    if (cap.IsOpened())
                    {
                        System.Diagnostics.Debug.WriteLine($"カメラ {cameraIndex} を {backend} バックエンドで開きました");
                        return cap;
                    }
                    cap.Dispose();
                    System.Diagnostics.Debug.WriteLine($"{backend} バックエンドで失敗");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"{backend} バックエンドでエラー: {ex.Message}");
                }
            }

            return null;
        }

        // カメラの設定を行う
        private static void ConfigureCamera(VideoCapture capture)
        {
            try
            {
                capture.Set(VideoCaptureProperties.FrameWidth, 640);
                capture.Set(VideoCaptureProperties.FrameHeight, 480);
                capture.Set(VideoCaptureProperties.Fps, 30);
                
                // 設定が反映されたか確認
                var width = capture.Get(VideoCaptureProperties.FrameWidth);
                var height = capture.Get(VideoCaptureProperties.FrameHeight);
                System.Diagnostics.Debug.WriteLine($"カメラ解像度: {width}x{height}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"カメラ設定エラー: {ex.Message}");
                // 設定失敗しても続行
            }
        }

        // 利用可能なカメラを列挙するヘルパーメソッド（最初に見つかったカメラを返す）
        public static int[] GetAvailableCameras()
        {
            var cameras = new System.Collections.Generic.List<int>();
            
            for (int i = 0; i < 5; i++) // 最大5つのカメラをチェック
            {
                try
                {
                    using var cap = OpenCamera(i);
                    if (cap != null && cap.IsOpened())
                    {
                        cameras.Add(i);
                        System.Diagnostics.Debug.WriteLine($"カメラ {i} 検出");
                        // 1つでも見つかったら検索を終了
                        break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"カメラ {i} の検出中にエラー: {ex.Message}");
                }
            }
            
            return [.. cameras];
        }
    }
}
