using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ResimGösterici
{
    internal class ShadowedImage : Image
    {
        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(70,128,128,128)), null, new Rect(new Point(2.5, 2.5), new Size(ActualWidth, ActualHeight)));
            base.OnRender(dc);
        }
    }
}
