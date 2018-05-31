using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IPS
{
    public static class Helper
    {
        public static ContextMenu WPFImageOFSMenu(BitmapImage img)// контекстное меню "Открыть в полном размере"
        {
                var dcm = new ContextMenu();
                var brush = new SolidColorBrush(new Color() { R = 30, G = 30, B = 30, A = 255 });
                dcm.Background = brush;
                var mi = new MenuItem() { Header = "Open full size", Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0) };
                mi.Click += (x, y) =>
                {
                    if (img != null)
                    {
                        var bmp = NodeLibrary.BI2B(img);
                        Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte> imge = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(bmp);
                        imge.Save("temp.png");
                        System.Diagnostics.Process.Start("temp.png");
                    }
                };
                dcm.Items.Add(mi);
                return dcm;
            
        }
    }
}
