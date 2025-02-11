using System;
using System.Globalization;
using System.Windows.Data;

namespace TaskManager
{
    public class PriorityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.WriteLine($"Converting value: {value} of type {value?.GetType()}");
            if (value is TaskPriority priority)
            {
                var result = priority switch
                {
                    TaskPriority.Low => "低",
                    TaskPriority.Medium => "中",
                    TaskPriority.High => "高",
                    _ => string.Empty
                };
                System.Diagnostics.Debug.WriteLine($"Converted to: {result}");
                return result;
            }
            System.Diagnostics.Debug.WriteLine("Value is not TaskPriority");
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.WriteLine($"ConvertBack value: {value} of type {value?.GetType()}");
            if (value is string text)
            {
                var result = text switch
                {
                    "低" => TaskPriority.Low,
                    "中" => TaskPriority.Medium,
                    "高" => TaskPriority.High,
                    _ => TaskPriority.Medium
                };
                System.Diagnostics.Debug.WriteLine($"ConvertBack result: {result}");
                return result;
            }
            System.Diagnostics.Debug.WriteLine("ConvertBack: Value is not string, returning Medium");
            return TaskPriority.Medium;
        }
    }
}