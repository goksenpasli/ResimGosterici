using System.Runtime.InteropServices;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ResimGösterici
{

    internal static class ExtensionMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool DestroyIcon(this IntPtr handle);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr ExtractIcon(this IntPtr hInst, string lpszExeFileName, int nIconIndex);

        public static BitmapSource IconCreate(this string filepath)
        {
            if (filepath != null)
            {
                try
                {
                    using System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(filepath);
                    BitmapSource bitmapsource = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    icon.Dispose();

                    if (bitmapsource.CanFreeze)
                    {
                        bitmapsource.Freeze();
                    }
                    return bitmapsource;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        }

        
    }
}
