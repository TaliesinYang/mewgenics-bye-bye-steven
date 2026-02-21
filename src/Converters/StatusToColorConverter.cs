using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MewgenicsSaveGuardian.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            bool b => b ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                        : new SolidColorBrush(Color.FromRgb(158, 158, 158)),
            int i when parameter?.ToString() == "penalty" =>
                i == 1 ? new SolidColorBrush(Color.FromRgb(244, 67, 54))
                       : new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
