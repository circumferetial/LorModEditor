using System.IO;
using System.Windows.Media.Imaging;

namespace Synthesis.Core.Tools;

public static class ImageHelper
{
    public static BitmapImage? LoadBitmapWithoutLocking(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            
            // 【关键修改】
            // 1. OnLoad 必须保留：这确保图片加载进内存后，立即关闭流，释放文件锁。
            bitmap.CacheOption = BitmapCacheOption.OnLoad;

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze(); // 冻结对象，允许跨线程访问
            return bitmap;
        }
        catch (Exception ex)
        {
            // 记录日志
            Log.Logger.Error("Error loading bitmap", ex); // 假设你有Logger
            return null;
        }
    }
}