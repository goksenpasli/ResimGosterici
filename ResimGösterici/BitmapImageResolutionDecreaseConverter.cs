using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ResimGösterici
{
    public class BitmapImageResolutionDecreaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Uri uri)
            {
                BitmapImage bi = new();
                bi.BeginInit();
                bi.UriSource = uri;
                bi.DecodePixelHeight= (int)System.Windows.SystemParameters.PrimaryScreenHeight;
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
