using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;

namespace WinGenTurner.Services
{
    public class KeyInputService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_UP = 0x26;
        private const byte VK_DOWN = 0x28;

        private DateTime lastUpKeyTime = DateTime.MinValue;
        private DateTime lastDownKeyTime = DateTime.MinValue;
        private const int KEY_COOLDOWN_MS = 1000; // キー入力のクールダウン時間（1秒）

        public void SendUpKey()
        {
            if ((DateTime.Now - lastUpKeyTime).TotalMilliseconds < KEY_COOLDOWN_MS)
                return;

            SendKey(VK_UP);
            lastUpKeyTime = DateTime.Now;
        }

        public void SendDownKey()
        {
            if ((DateTime.Now - lastDownKeyTime).TotalMilliseconds < KEY_COOLDOWN_MS)
                return;

            SendKey(VK_DOWN);
            lastDownKeyTime = DateTime.Now;
        }

        private void SendKey(byte virtualKey)
        {
            try
            {
                // キーダウン
                keybd_event(virtualKey, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                Thread.Sleep(50);
                // キーアップ
                keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception)
            {
                // キー送信エラーは無視
            }
        }
    }
}
