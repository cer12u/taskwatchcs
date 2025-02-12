using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskManager
{
    public class PriorityBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskPriority priority)
            {
                return priority switch
                {
                    TaskPriority.High => new SolidColorBrush(Color.FromRgb(255, 240, 240)),   // 薄い赤
                    TaskPriority.Medium => new SolidColorBrush(Color.FromRgb(255, 255, 240)), // 薄い黄色
                    TaskPriority.Low => new SolidColorBrush(Color.FromRgb(245, 245, 245)),    // 薄いグレー
                    _ => new SolidColorBrush(Colors.Transparent)
                };
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}