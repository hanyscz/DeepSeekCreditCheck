using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DeepSeekCreditCheck.UI.Services;

public static class WindowIconHelper
{
    private static ImageSource? _cached;

    public static ImageSource GetIcon()
    {
        if (_cached != null) return _cached;

        try
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var icoPath = Path.Combine(exeDir, "Resources", "app.ico");
            if (File.Exists(icoPath))
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(icoPath);
                if (icon != null)
                {
                    using var bmp = icon.ToBitmap();
                    var ms = new MemoryStream();
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = ms;
                    img.EndInit();
                    img.Freeze();
                    _cached = img;
                    return _cached;
                }
            }
        }
        catch { }

        // Generovana ikona — modrý čtvereček s bílým $
        var wb = new WriteableBitmap(16, 16, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[16 * 16 * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = 215;     // B
            pixels[i + 1] = 120; // G
            pixels[i + 2] = 0;   // R
            pixels[i + 3] = 255; // A
        }
        // Jednoduche bílé body pro symbol $
        int[] dollarPixels = { 38, 39, 54, 55, 70, 71, 86, 87, 102, 103, 118, 119, 134, 135, 150, 151, 166, 167, 182, 183, 198, 199, 214, 215, 230, 231, 246, 247 };
        foreach (var pos in dollarPixels)
        {
            if (pos * 4 + 3 < pixels.Length)
            {
                pixels[pos * 4] = 255;
                pixels[pos * 4 + 1] = 255;
                pixels[pos * 4 + 2] = 255;
                pixels[pos * 4 + 3] = 255;
            }
        }
        wb.WritePixels(new System.Windows.Int32Rect(0, 0, 16, 16), pixels, 16 * 4, 0);
        wb.Freeze();
        _cached = wb;
        return _cached;
    }
}
