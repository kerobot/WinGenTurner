using System.IO;
using System.Net.Http;

namespace WinGenTurner.Services
{
    public static class CascadeHelper
    {
        private const string OPENCV_BASE_URL = "https://raw.githubusercontent.com/opencv/opencv/4.x/data/haarcascades/";
        
        // OpenCVのGitHubリポジトリに実際に存在するファイル名
        private static readonly string[] RequiredCascades = new[]
        {
            "haarcascade_frontalface_default.xml",
            "haarcascade_eye.xml",
            "haarcascade_smile.xml"  // 口の検出には smile を使用
        };

        public static async Task<bool> EnsureCascadeFilesExist()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var cascade in RequiredCascades)
            {
                var filePath = Path.Combine(baseDir, cascade);
                if (!File.Exists(filePath))
                {
                    try
                    {
                        await DownloadCascadeFile(cascade, filePath);
                    }
                    catch (Exception ex)
                    {
                        // エラー詳細をログに記録
                        System.Diagnostics.Debug.WriteLine($"Failed to download {cascade}: {ex.Message}");
                        return false;
                    }
                }
            }

            return true;
        }

        private static async Task DownloadCascadeFile(string fileName, string destinationPath)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);
            
            var url = OPENCV_BASE_URL + fileName;
            var data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(destinationPath, data);
        }

        // ファイル名の別名マッピング（後方互換性のため）
        public static string GetCascadeFileName(string cascadeType)
        {
            return cascadeType switch
            {
                "mouth" => "haarcascade_smile.xml",
                "face" => "haarcascade_frontalface_default.xml",
                "eye" => "haarcascade_eye.xml",
                _ => cascadeType
            };
        }
    }
}
