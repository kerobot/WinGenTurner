using System.Windows;
using System.Windows.Media.Imaging;

namespace WinGenTurner
{
    public partial class MonitorWindow : Window
    {
        public MonitorWindow()
        {
            InitializeComponent();
        }

        public void UpdateFrame(BitmapSource bitmap)
        {
            Dispatcher.Invoke(() =>
            {
                CameraImage.Source = bitmap;
                OverlayText.Visibility = Visibility.Collapsed;
            });
        }

        public void ShowMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                OverlayText.Text = message;
                OverlayText.Visibility = Visibility.Visible;
            });
        }
    }
}
