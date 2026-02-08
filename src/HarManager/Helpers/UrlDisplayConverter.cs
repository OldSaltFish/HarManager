using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HarManager.Helpers
{
    public class UrlDisplayConverter : IValueConverter
    {
        public static readonly UrlDisplayConverter Instance = new UrlDisplayConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string url) return value;
            
            // parameter is the simplified mode boolean
            if (parameter is bool simplified && simplified)
            {
                try
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        return uri.PathAndQuery;
                    }
                }
                catch
                {
                    // Fallback to original
                }
            }
            
            return url;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

