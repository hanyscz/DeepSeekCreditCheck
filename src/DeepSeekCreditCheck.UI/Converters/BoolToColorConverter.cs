using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DeepSeekCreditCheck.UI.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
            return isAvailable ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
