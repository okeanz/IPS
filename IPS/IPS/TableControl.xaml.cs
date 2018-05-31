using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IPS
{
    /// <summary>
    /// Логика взаимодействия для TableControl.xaml
    /// </summary>
    public partial class TableControl : UserControl
    {
        Array lastUpdated = null;

        public TableControl()
        {
            InitializeComponent();
            CBFormat.SelectionChanged += (x, y) => { UpdateArray(lastUpdated); };
        }

        public void UpdateArray(Array arr)
        {
            lastUpdated = arr;
            
            MainGrid.Children.Clear();
            MainGrid.ColumnDefinitions.Clear();
            MainGrid.RowDefinitions.Clear();

            if (arr == null)
            {
                DockInfo.Visibility = System.Windows.Visibility.Hidden;
                return;
            }
            else
                DockInfo.Visibility = System.Windows.Visibility.Visible;

            if (arr.Rank == 1 && CBFormat.SelectedIndex == 1)
            {
                var r = (int)Math.Sqrt(arr.Length);
                var l = (int)(arr.Length / r) + 1;

                var oArr = Array.CreateInstance(arr.GetType().GetElementType(), r, l);
                int c = 0;
                for (int t = 0; t < r; t++)
                    for (int v = 0; v < l; v++)
                        if (c < arr.Length)
                            oArr.SetValue((arr as dynamic)[c++], new int[] { t, v });
                arr = oArr;
            }

            if (arr.Rank == 1)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    var value = arr.GetValue(i);
                    if (value == null) continue;
                    FrameworkElement FElement = null;
                    if (value.GetType().IsIConvertible() || value.GetType() == typeof(NodeLibrary.IndexedValue))
                        FElement = new Label() { Content = value, Foreground = Brushes.White };
                    if (value.GetType() == typeof(BitmapImage))
                    {
                        var val = (value as BitmapImage).Clone();
                        FElement = new Image() { Source = val, Margin = new Thickness(2), MinHeight = 250, MaxHeight = 250, MinWidth = 250, MaxWidth = 250, ContextMenu = Helper.WPFImageOFSMenu(val) };
                    }

                    MainGrid.ColumnDefinitions.Add(new ColumnDefinition());
                    MainGrid.Children.Add(FElement);
                    Grid.SetColumn(FElement, i);
                }

            }

            if (arr.Rank == 2)
            {

                for (int i = 0; i < arr.GetLength(0); i++)
                {
                    MainGrid.RowDefinitions.Add(new RowDefinition());
                    for (int k = 0; k < arr.GetLength(1); k++)
                    {
                        if (i == 0) MainGrid.ColumnDefinitions.Add(new ColumnDefinition());

                        var value = arr.GetValue(new int[] { i, k });
                        if (value == null) continue;

                        UIElement FElement = default(UIElement);
                        if (value.GetType().IsIConvertible() || value.GetType() == typeof(NodeLibrary.IndexedValue))
                            FElement = new Label() { Content = value, Foreground = Brushes.White };
                        if (value.GetType() == typeof(BitmapImage))
                        {
                            var val = (value as BitmapImage).Clone();
                            FElement = new Image() { Source = value as ImageSource, Margin = new Thickness(2), MinHeight = 250, MaxHeight = 250, MinWidth = 250, MaxWidth = 250, ContextMenu = Helper.WPFImageOFSMenu(val) };
                        }
                        MainGrid.Children.Add(FElement);
                        Grid.SetColumn(FElement, k);
                        Grid.SetRow(FElement, i);
                    }
                }

            }




            

        }




    }
}
