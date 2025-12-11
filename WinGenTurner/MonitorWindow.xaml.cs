using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace WinGenTurner
{
    public partial class MonitorWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public MonitorWindow()
        {
            InitializeComponent();
            
            // ウィンドウがロードされたときに非アクティブ化スタイルを設定
            Loaded += MonitorWindow_Loaded;
            
            System.Diagnostics.Debug.WriteLine("MonitorWindow 初期化完了");
        }

        private void MonitorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new WindowInteropHelper(this);
            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }

        public void UpdateFrame(BitmapSource bitmap)
        {
            if (bitmap == null)
            {
                System.Diagnostics.Debug.WriteLine("ビットマップがnullです");
                return;
            }

            try
            {
                if (CheckAccess())
                {
                    CameraImage.Source = bitmap;
                    OverlayText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        CameraImage.Source = bitmap;
                        OverlayText.Visibility = Visibility.Collapsed;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"フレーム更新エラー: {ex.Message}");
            }
        }

        public void ShowMessage(string message)
        {
            try
            {
                if (CheckAccess())
                {
                    OverlayText.Text = message;
                    OverlayText.Visibility = Visibility.Visible;
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        OverlayText.Text = message;
                        OverlayText.Visibility = Visibility.Visible;
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"メッセージ表示エラー: {ex.Message}");
            }
        }
    }
}
