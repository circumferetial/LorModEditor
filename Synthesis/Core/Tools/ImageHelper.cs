using System.IO;
using System.Windows.Media.Imaging;
using Synthesis.Core.Log;

namespace Synthesis.Core.Tools;

public static class ImageHelper
{
    public static BitmapImage? LoadBitmapWithoutLocking(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            using (var streamSource = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                bitmapImage.StreamSource = streamSource;
                bitmapImage.EndInit();
            }
            bitmapImage.Freeze();
            return bitmapImage;
        }
        catch (Exception ex)
        {
            Logger.Error("Error loading bitmap", ex);
            return null;
        }
    }
}
