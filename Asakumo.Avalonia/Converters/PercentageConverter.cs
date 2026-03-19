using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Asakumo.Avalonia.Converters;

/// <summary>
/// Converts a value to a percentage of that value.
/// </summary>
public class PercentageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string paramStr)
        {
            if (double.TryParse(paramStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var percentage))
            {
                return doubleValue * percentage;
            }
        }
        
        // Fallback for non-double values
        if (value != null && parameter is string paramStr2)
        {
            try
            {
                var numericValue = System.Convert.ToDouble(value);
                if (double.TryParse(paramStr2, NumberStyles.Float, CultureInfo.InvariantCulture, out var percentage))
                {
                    return numericValue * percentage;
                }
            }
            catch
            {
                // Ignore conversion errors
            }
        }
        
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
