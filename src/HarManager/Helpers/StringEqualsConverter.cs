using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace HarManager.Helpers
{
    public class StringEqualsConverter : IValueConverter
    {
        public static readonly StringEqualsConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null && parameter == null) return true;
            if (value == null || parameter == null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter != null)
            {
                return parameter.ToString();
            }
            return BindingOperations.DoNothing;
        }
    }
}
