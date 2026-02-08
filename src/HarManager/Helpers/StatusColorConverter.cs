using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace HarManager.Helpers
{
    public class StatusColorConverter : IValueConverter
    {
        public static readonly StatusColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int status || (value is string s && int.TryParse(s, out status)))
            {
                if (status >= 200 && status < 300)
                    return Brushes.ForestGreen;
                if (status >= 300 && status < 400)
                    return Brushes.Orange;
                if (status >= 400 && status < 600)
                    return Brushes.Crimson;
            }
            
            return Brushes.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
