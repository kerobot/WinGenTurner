using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System;

namespace WinGenTurner
{
    public partial class StatusWindow : Window
    {
        private MonitorWindow? monitorWindow;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public StatusWindow()
        {
            InitializeComponent();
            Loaded += StatusWindow_Loaded;
        }

        private void StatusWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, 
                GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (monitorWindow == null)
            {
                monitorWindow = new MonitorWindow();
                monitorWindow.Closed += (s, args) => monitorWindow = null;
                
                // StatusWindowの下10ピクセル開けて、横位置を揃える
                monitorWindow.Left = this.Left;
                monitorWindow.Top = this.Top + this.ActualHeight + 10;
                
                monitorWindow.Show();
                
                // Appクラス経由でモニターウィンドウの参照を保持
                if (Application.Current is App app)
                {
                    app.SetMonitorWindow(monitorWindow);
                }
            }
            else
            {
                monitorWindow.Close();
                monitorWindow = null;
                
                if (Application.Current is App app)
                {
                    app.SetMonitorWindow(null);
                }
            }
        }

        public void UpdateCameraStatus(string status, bool isActive)
        {
            Dispatcher.Invoke(() =>
            {
                CameraStatusText.Text = status;
                CameraStatusText.Foreground = isActive 
                    ? System.Windows.Media.Brushes.Green 
                    : System.Windows.Media.Brushes.Red;
            });
        }

        public void UpdateDetectionStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                DetectionStatusText.Text = status;
            });
        }
    }
}
