using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ResimGösterici
{
    internal class ShadowedImage : Image
    {
        public bool ShowShadow
        {
            get { return (bool)GetValue(ShowShadowProperty); }
            set { SetValue(ShowShadowProperty, value); }
        }
        public static readonly DependencyProperty ShowShadowProperty = DependencyProperty.Register("ShowShadow", typeof(bool), typeof(ShadowedImage), new PropertyMetadata(false));

        public bool ShowFileIcon
        {
            get { return (bool)GetValue(ShowFileIconProperty); }
            set { SetValue(ShowFileIconProperty, value); }
        }
        public static readonly DependencyProperty ShowFileIconProperty = DependencyProperty.Register("ShowFileIcon", typeof(bool), typeof(ShadowedImage), new PropertyMetadata(false));

        protected override void OnRender(DrawingContext dc)
        {
            if (ShowShadow)
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(70, 128, 128, 128)), null, new Rect(new Point(2.5, 2.5), new Size(ActualWidth, ActualHeight)));
            }
            base.OnRender(dc);
            if (ShowFileIcon)
            {
                dc.DrawImage((DataContext as Resim).Yol.OriginalString.IconCreate(), new Rect(0, 0, 16, 16));
            }
        }
    }
}
