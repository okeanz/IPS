using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Ink;
//using System.Drawing;

namespace IPS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TranslateTransform panTransform;
        ScaleTransform zoomTransform;
        TransformGroup bothTransforms;

        Point Center;

        double minZoom = 1.0, maxZoom = 5.0;

        public MainWindow()
        {
            InitializeComponent();

            Center = new Point(MainPanel.ActualWidth/2,MainPanel.ActualHeight/2);

            this.MouseWheel += MainWindow_MouseWheel;
            var rr = MainPanel.RenderTransform;

            panTransform = new TranslateTransform();
            zoomTransform = new ScaleTransform();
            bothTransforms = new TransformGroup();


            bothTransforms.Children.Add(panTransform);
            bothTransforms.Children.Add(zoomTransform);

            MainPanel.RenderTransform = bothTransforms;


            zoomTransform.ScaleX = 1.0;
            zoomTransform.ScaleY = 1.0;

            MainPanel.MouseDown += MainPanel_MouseDown;
            this.MouseMove += MainPanel_MouseMove;
            MainPanel.MouseUp += MainPanel_MouseUp;

            
        }





        //Активация перемещения MainPanel
        bool moveMPanel = false;
        Vector MPStartDrag;
        void MainPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                moveMPanel = true;
                MPStartDrag = (Vector)e.GetPosition(this);

            }
        }
        //Перемещение MainPanel
        void MainPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!moveMPanel) return;
            if (NNodeDrag) return;
            if (NConnDrag) return;
            var scale = zoomTransform.ScaleX;
            Vector diff = (MPStartDrag - (Vector)Mouse.GetPosition(this));// / scale;

            if (e.MouseDevice.RightButton == MouseButtonState.Pressed)
            {
                var rect = MainPanel;
                var mar = rect.Margin;
                var marWork = new Thickness(0, 35, 255, 0);
                int maxDiff = 50;

                mar.Left -= diff.X;
                mar.Right += diff.X;
                mar.Bottom += diff.Y;
                mar.Top -= diff.Y;

                //mar.Left = mar.Left > marWork.Left + maxDiff ? marWork.Left + maxDiff : mar.Left;
                //mar.Right = mar.Right > marWork.Right + maxDiff ? marWork.Right + maxDiff : mar.Right;

                //mar.Top = mar.Top > marWork.Top + maxDiff ? marWork.Top + maxDiff : mar.Top;
                //mar.Bottom = mar.Bottom > marWork.Bottom + maxDiff ? marWork.Bottom + maxDiff : mar.Bottom;

                rect.Margin = mar;

                //panTransform.X -= diff.X;
                //panTransform.Y -= diff.Y;

                MPStartDrag = (Vector)Mouse.GetPosition(this);

            }
        }
        //Сброс состояния перемещения MainPanel
        void MainPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                moveMPanel = false;
                MPStartDrag = new Vector();

            }
        }

        void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)//e.Delta = 120
        {
            Point position = e.GetPosition(MainPanel);

            var posWin = e.GetPosition(this);


            if (!MainPanel.IsMouseOver) return;
            if (e.Delta != 0.0)
            {

                zoomTransform.ScaleX = Clamp(minZoom, maxZoom, zoomTransform.ScaleX + zoomTransform.ScaleX * (e.Delta / 1000.0));
                zoomTransform.ScaleY = zoomTransform.ScaleX;

                var p = new Point(this.ActualWidth / 2,this.ActualHeight / 2);
                var center = this.TransformToVisual(MainPanel).Transform(p);
                zoomTransform.CenterX = center.X;
                zoomTransform.CenterY = center.Y;
            }

            
        }

        double Clamp(double min, double max, double value)
        {
            var outp = value;
            if (outp < min) outp = min;
            if (outp > max) outp = max;
            return outp;
        }

        private void MenuLoad_Click(object sender, RoutedEventArgs e)
        {
            MainPanel.Children.RemoveRange(0, MainPanel.Children.Count);

            OpenFileDialog ofd = new OpenFileDialog();

            bool? good = ofd.ShowDialog();
            string[] settings, nodes, paths;

            string full = String.Empty;

            if (good == true)
                using (StreamReader sr = File.OpenText(ofd.FileName))
                {

                    full = sr.ReadToEnd();
                }
            Title = string.Format("IPS | [{0}]",ofd.FileName);
            var spl = full.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            settings = spl.Where((x, y) => y > spl.IndexOf("[Settings]") && y < spl.IndexOf("[End]")).ToArray();
            spl.Remove("[Settings]");
            spl.Remove("[End]");
            nodes = spl.Where((x, y) => y > spl.IndexOf("[Nodes]") && y < spl.IndexOf("[End]")).ToArray();
            spl.Remove("[Nodes]");
            spl.Remove("[End]");
            paths = spl.Where((x, y) => y > spl.IndexOf("[Paths]") && y < spl.IndexOf("[End]")).ToArray();

            for (int i = 0; i < nodes.Count(); i += 2)
            {
                var first = nodes[i].Split(new char[] { '_' });
                var ncName = first[0].Substring(0, first[0].Length);
                if (NodeLibrary.Nodes.Find(n => n.NCName == ncName) == null) continue;

                var nc = AddNC(ncName);

                double top = double.Parse(first[1]);
                double left = double.Parse(first[2]);
                var margin = nc.Margin;
                margin.Top = top;
                margin.Left = left;
                nc.Margin = margin;

                var guidsNvals = nodes[i + 1].Split(new char[] { '_' });

                var guids = new List<string>();
                var vals = new List<string>();

                foreach(var gnv in guidsNvals)
                {
                    var spltd = gnv.Split('|');
                    guids.Add(spltd[0]);
                    vals.Add(spltd[1]);
                }

                
                var conns = nc.GetConnectorList();

                if (guids.Count != conns.Count) continue;

                for (int k = 0; k < conns.Count; k++)
                {
                    conns[k].guid = Guid.Parse(guids[k]);
                    conns[k].SetActiveFieldValue(vals[k]);
                }

            }

            var ncs = MainPanel.Children.OfType<NodeControl>();
            List<NodeSocket> nodeConns = new List<NodeSocket>();
            foreach (var nControl in ncs)
                nodeConns.AddRange(nControl.GetConnectorList());

            foreach (var path in paths)
            {
                var splited = path.Split(new char[] { '_' });
                var start = nodeConns.Find(x => x.guid.ToString() == splited[0]);
                var end = nodeConns.Find(x => x.guid.ToString() == splited[1]);
                if (start == null || end == null) continue;

                var Dpath = CreatePath(new Vector(), new Vector(), NodeSocket.SocketTypes.Output);
                MainPanel.Children.Add(Dpath);
                start.ConnectedCurves.Add(Dpath);
                end.ConnectedCurves.Add(Dpath);

                Dpath.SetSockets(start, end);


                (Dpath.ContextMenu.Items[0] as MenuItem).Click += (x, y) =>
                {
                    MainPanel.Children.Remove(Dpath);
                    end.ConnectedCurves.Remove(Dpath);
                    start.ConnectedCurves.Remove(Dpath);
                    UpdateProcess(Dpath.GetInputNC());
                };
            }
            System.Diagnostics.Debug.WriteLine("tst");
            foreach (var nc in MainPanel.Children.OfType<NodeControl>())
            {
                nc.Loaded += (x,y) => 
                {
                    System.Diagnostics.Debug.WriteLine("cuvesUpdatedTask");
                    UpdateCurves(nc);
                    
                };
            }




            UpdateProcess(null);
        }

        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.AddExtension = true;
            sfd.DefaultExt = "*.txt";
            sfd.ValidateNames = true;


            bool? good = sfd.ShowDialog();
            if (good == true)
                using (StreamWriter sw = File.CreateText(sfd.FileName))
                {
                    StringBuilder nodes = new StringBuilder();
                    StringBuilder settings = new StringBuilder();
                    StringBuilder paths = new StringBuilder();

                    foreach (var node in MainPanel.Children.OfType<NodeControl>())
                    {
                        nodes.AppendLine(node.NCName + "_" + node.Margin.Top + "_" + node.Margin.Left);
                        var ncs = node.GetConnectorList().Select(x => x.guid.ToString() + "|"+ (x.GetSaveValue())).ToArray();

                        string nodeConns = string.Join("_", ncs);
                        nodes.AppendLine(nodeConns);
                    }

                    foreach (var path in MainPanel.Children.OfType<System.Windows.Shapes.Path>())
                    {
                        paths.AppendLine(path.GetInitSocket().guid + "_" + path.GetEndSocket().guid);
                    }




                    sw.WriteLine("[Settings]");
                    sw.WriteLine(settings.ToString());
                    sw.WriteLine("[End]");
                    sw.WriteLine("[Nodes]");
                    sw.WriteLine(nodes.ToString());
                    sw.WriteLine("[End]");
                    sw.WriteLine("[Paths]");
                    sw.WriteLine(paths.ToString());
                    sw.WriteLine("[End]");

                    sw.Close();
                }

        }

       



    }

    public static class KeyboardMods
    {
        public static bool ChainUpdate
        {
            get
            {
                return !(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            }
        }

        public static bool AltMod
        {
            get
            {
                return (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt));
            }
        }
    }

}
