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
                    TaskPriority.High => new SolidColorBrush(Color.FromRgb(255, 245, 245)),   // もっと薄い赤
                    TaskPriority.Medium => new SolidColorBrush(Color.FromRgb(255, 255, 245)), // もっと薄い黄色
                    TaskPriority.Low => new SolidColorBrush(Color.FromRgb(245, 255, 245)),    // もっと薄い緑
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