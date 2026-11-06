using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HaloSyncWin.UI.Common
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool v && v;
            if (Invert) b = !b;

            if (targetType == typeof(Visibility))
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }

            if (targetType == typeof(Brush) || targetType == typeof(Color))
            {
                if (parameter != null)
                {
                    try
                    {
                        var colorStr = parameter.ToString();
                        if (colorStr == "True")
                        {
                            return b ? new SolidColorBrush(Color.FromRgb(75, 255, 145)) : new SolidColorBrush(Color.FromRgb(102, 102, 114));
                        }
                        if (b && !string.IsNullOrEmpty(colorStr))
                        {
                            return (SolidColorBrush)new BrushConverter().ConvertFromString(colorStr)!;
                        }
                    }
                    catch { }
                }
                return Binding.DoNothing;
            }

            if (parameter != null && parameter.ToString() == "Running")
            {
                return b ? "Stop Sync" : "Start Sync";
            }

            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility vis && vis == Visibility.Visible;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = value != null;
            if (Invert) hasValue = !hasValue;
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float f) return $"{(int)Math.Round(f * 100)}%";
            if (value is double d) return $"{(int)Math.Round(d * 100)}%";
            return "0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class EqualityToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                if (targetType == typeof(Visibility)) return Visibility.Collapsed;
                return false;
            }

            bool matches = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
            if (targetType == typeof(Visibility))
            {
                return matches ? Visibility.Visible : Visibility.Collapsed;
            }
            return matches;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter != null)
            {
                return Enum.Parse(targetType, parameter.ToString()!);
            }
            return Binding.DoNothing;
        }
    }

    public class EqualityToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Invert ? Visibility.Visible : Visibility.Collapsed;

            bool matches = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
            if (Invert) matches = !matches;
            return matches ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}

