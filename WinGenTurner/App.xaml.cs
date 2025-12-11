using Hardcodet.Wpf.TaskbarNotification;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows;
using WinGenTurner.Services;

namespace WinGenTurner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon? notifyIcon;
        private StatusWindow? statusWindow;
        private CameraService? cameraService;
        private FaceDetectionService? faceDetectionService;
        private KeyInputService? keyInputService;
        private MonitorWindow? monitorWindow;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            
            statusWindow = new StatusWindow();
            statusWindow.Show();

            // Haar Cascadeファイルの確認とダウンロード
            statusWindow.UpdateCameraStatus("Initializing...", false);
            statusWindow.UpdateDetectionStatus("Checking Cascade file...");
            
            bool cascadesReady = await CascadeHelper.EnsureCascadeFilesExist();
            if (!cascadesReady)
            {
                MessageBox.Show(
                    "必要なファイルのダウンロードに失敗しました。\nインターネット接続を確認してください。",
                    "初期化エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            InitializeServices();
        }

        private void InitializeServices()
        {
            cameraService = new CameraService();
            faceDetectionService = new FaceDetectionService();
            keyInputService = new KeyInputService();

            // イベントハンドラの登録
            cameraService.FrameCaptured += OnFrameCaptured;
            cameraService.StatusChanged += OnCameraStatusChanged;
            cameraService.ErrorOccurred += OnError;

            faceDetectionService.GestureDetected += OnGestureDetected;
            faceDetectionService.ProcessedFrameReady += OnProcessedFrameReady;

            // 顔検出サービスの初期化
            if (!faceDetectionService.Initialize())
            {
                MessageBox.Show(
                    "顔検出の初期化に失敗しました。\nHaar Cascadeファイルが見つかりません。",
                    "初期化エラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            // 利用可能なカメラをチェック
            var availableCameras = CameraService.GetAvailableCameras();
            System.Diagnostics.Debug.WriteLine($"利用可能なカメラ数: {availableCameras.Length}");
            
            if (availableCameras.Length == 0)
            {
                MessageBox.Show(
                    "カメラが検出されませんでした。\n" +
                    "カメラを接続してアプリケーションを再起動してください。\n\n" +
                    "考えられる原因:\n" +
                    "- 他のアプリケーションがカメラを使用中\n" +
                    "- カメラドライバーがインストールされていない\n" +
                    "- デバイスマネージャーでカメラが無効化されている",
                    "カメラエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                statusWindow?.UpdateCameraStatus("The camera cannot be found.", false);
                return;
            }

            // カメラ開始（最初の利用可能なカメラを使用）
            int cameraIndex = availableCameras[0];
            System.Diagnostics.Debug.WriteLine($"カメラ {cameraIndex} を起動中...");
            
            if (cameraService.Start(cameraIndex))
            {
                statusWindow?.UpdateCameraStatus("Starting up", true);
                statusWindow?.UpdateDetectionStatus("Standby...");
                System.Diagnostics.Debug.WriteLine("カメラサービス起動成功");
            }
            else
            {
                statusWindow?.UpdateCameraStatus("Error", false);
                MessageBox.Show(
                    $"カメラ {cameraIndex} の起動に失敗しました。\n" +
                    "他のアプリケーションがカメラを使用していないか確認してください。",
                    "カメラエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void SetMonitorWindow(MonitorWindow? window)
        {
            monitorWindow = window;
        }

        private void OnFrameCaptured(Mat frame)
        {
            faceDetectionService?.ProcessFrame(frame);
        }

        private void OnProcessedFrameReady(Mat frame)
        {
            // モニターウィンドウに表示
            if (monitorWindow != null && !frame.Empty())
            {
                var bitmap = frame.ToWriteableBitmap();
                monitorWindow.UpdateFrame(bitmap);
            }
        }

        private void OnGestureDetected(GestureType gesture)
        {
            switch (gesture)
            {
                case GestureType.LookingUp:
                    keyInputService?.SendUpKey();
                    statusWindow?.UpdateDetectionStatus("Looking up.");
                    break;
                case GestureType.MouthOpen:
                    keyInputService?.SendDownKey();
                    statusWindow?.UpdateDetectionStatus("Mouth open.");
                    break;
            }
        }

        private void OnCameraStatusChanged(bool isActive)
        {
            statusWindow?.UpdateCameraStatus(isActive ? "Starting up" : "Pausing", isActive);
        }

        private void OnError(string error)
        {
            Dispatcher.Invoke(() =>
            {
                statusWindow?.UpdateCameraStatus($"Error: {error}", false);
            });
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            cameraService?.Dispose();
            faceDetectionService?.Dispose();
            notifyIcon?.Dispose();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Shutdown();
        }
    }
}
