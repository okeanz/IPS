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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace IPS
{
    public partial class MainWindow : Window
    {
        public static ContextMenu DeleteContextMenu
        {
            get
            {
                var dcm = new ContextMenu();
                var brush = new SolidColorBrush(new Color() { R = 30, G = 30, B = 30, A = 255 });
                dcm.Background = brush;
                dcm.Items.Add(new MenuItem() { Header = "Удалить", Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0) });
                return dcm;
            }
        }



        //Активация создания соединения NodeConnector
        bool NConnDrag = false;
        NodeSocket initNode;
        private void NodeConnector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            initNode = sender as NodeSocket;
            if (initNode.Passive)
                NConnDrag = true;
            else
                initNode = null;


        }

        //Закрепление соединения между NodeConnector
        private void NodeConnector_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!NConnDrag) return;

            var nc = sender as NodeSocket;
            nc.SetBaseColor();
            initNode.SetColorStroke(Brushes.Black);
            if (nc.SocketType == initNode.SocketType)
            {
                MessageBox.Show("Same node type");
                Window_MouseLeftButtonDown(sender, e);
                return;
            }
            if (nc.ParentNode == initNode.ParentNode)
            {
                MessageBox.Show("Infinite cycle");
                Window_MouseLeftButtonDown(sender, e);
                return;
            }
            if (nc.ConnectedCurves.Select(x => x.GetInitSocket()).Contains(initNode)) return;
            if (nc.SocketType == NodeSocket.SocketTypes.Input && nc.ConnectedCurves.Count > 0)
            {
                MessageBox.Show("There are existing connections left");
                Window_MouseLeftButtonDown(sender, e);
                return;
            }
            if (initNode.SocketType == NodeSocket.SocketTypes.Input && initNode.ConnectedCurves.Count > 0)
            {
                MessageBox.Show("There are existing connections left");
                Window_MouseLeftButtonDown(sender, e);
                return;
            }
            //if (initNode.DataType != nc.DataType) return;
            Vector start = (Vector)initNode.GetCenter(MainPanel);
            Vector end = (Vector)nc.GetCenter(MainPanel);

            var path = CreatePath(start, end, nc.SocketType == NodeSocket.SocketTypes.Input ? NodeSocket.SocketTypes.Output : NodeSocket.SocketTypes.Input);
            MainPanel.Children.Remove(Drawed);
            Drawed = null;
            MainPanel.Children.Add(path);
            nc.ConnectedCurves.Add(path);
            initNode.ConnectedCurves.Add(path);

            path.SetSockets(initNode, nc);

            (path.ContextMenu.Items[0] as MenuItem).Click += (x, y) =>
            {
                MainPanel.Children.Remove(path);
                path.GetInitSocket().ConnectedCurves.Remove(path);
                path.GetEndSocket().ConnectedCurves.Remove(path);
                UpdateProcess(nc.ParentNode);
            };
            UpdateProcess(nc.ParentNode);
            NConnDrag = false;
            //MessageBox.Show(MainGrid.Children.OfType<Path>().Count().ToString());
        }


        //Подсветка NodeConnector при наведении мыши
        private void NodeConnector_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!NConnDrag) return;
            var nc = sender as NodeSocket;
            bool good = false;
            if (initNode.SocketType != nc.SocketType)
                if (nc.ParentNode != initNode.ParentNode)
                    if (!nc.ConnectedCurves.Select(x => x.GetInitSocket()).Contains(initNode))
                        if (!(nc.SocketType == NodeSocket.SocketTypes.Input && nc.ConnectedCurves.Count > 0))
                            if (!(initNode.SocketType == NodeSocket.SocketTypes.Input && initNode.ConnectedCurves.Count > 0))
                                //if (initNode.DataType == nc.DataType)
                                good = true;
            nc.SetColorStroke(good ? Brushes.Green : Brushes.Red);

        }


        private void NodeConnector_MouseLeave(object sender, MouseEventArgs e)
        {
            var nc = sender as NodeSocket;
            if (!NConnDrag) return;
            nc.SetBaseColor();
        }

        //Активация перетаскивания окна Node
        Vector startPos;
        bool NNodeDrag = false;
        NodeControl DraggingNC;
        private void NodeControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (NConnDrag) return;
            startPos = (Vector)Mouse.GetPosition(this);
            DraggingNC = sender as NodeControl;
            NNodeDrag = true;
        }

        //Перетаскивание Node
        private void NodeControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!NNodeDrag) return;
            if (NConnDrag) return;
            if (DraggingNC == null) return;
            var scale = zoomTransform.ScaleX;
            Vector diff = (startPos - (Vector)Mouse.GetPosition(this)) / scale;

            if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
            {
                var rect = DraggingNC;
                var mar = rect.Margin;
                mar.Left -= diff.X;
                mar.Top -= diff.Y;
                var h = Window.GetWindow(this).ActualHeight;
                var w = Window.GetWindow(this).ActualWidth;

                mar.Left = mar.Left < 0 ? 0 : mar.Left;
                // mar.Left = mar.Left > w - rect.ActualWidth ? w - rect.ActualWidth : mar.Left;

                mar.Top = mar.Top <= 0 ? 0 : mar.Top;
                // mar.Top = mar.Top > h - rect.ActualHeight ? h - rect.ActualHeight : mar.Top;

                rect.Margin = mar;
                startPos = (Vector)Mouse.GetPosition(this);

                UpdateCurves(DraggingNC as NodeControl);
            }
        }

        public void UpdateCurves(NodeControl nc)
        {
            UpdateCurves(nc, new Vector());
        }
        //Обновление кривых при перемещении окна
        public void UpdateCurves(NodeControl nc, Vector offset)
        {
            var connectors = nc.GetConnectorList();
            if (MainPanel.Children.OfType<Path>().Count() == 0) return;
            foreach (var con in connectors)
                for (int i = 0; i < con.ConnectedCurves.Count; i++)
                {
                    var oldpath = con.ConnectedCurves[i];

                    MainPanel.Children.Remove(oldpath);
                    var farSock = con.SocketType == NodeSocket.SocketTypes.Input ? oldpath.GetOutputSocket() : oldpath.GetInputSocket();
                    var path = CreatePath(con.GetCenter(MainPanel) - offset, farSock.GetCenter(MainPanel), con.SocketType);
                    path.SetSockets(con, farSock);

                    MainPanel.Children.Add(path);

                    con.ConnectedCurves.Remove(oldpath);
                    farSock.ConnectedCurves.Remove(oldpath);

                    con.ConnectedCurves.Add(path);
                    farSock.ConnectedCurves.Add(path);

                }
            
        }

        //Создание соединения NodeConnector
        Path Drawed;
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!NConnDrag) return;
            if (Mouse.LeftButton != MouseButtonState.Pressed) return;

            var path = CreatePath((Vector)initNode.GetCenter(MainPanel), (Vector)Mouse.GetPosition(MainPanel), initNode.SocketType);
            initNode.SetColorStroke(Brushes.DeepSkyBlue);
            path.Stroke = Brushes.DeepSkyBlue;


            if (Drawed == null)
            {
                MainPanel.Children.Add(path);
                Drawed = path;
            }
            else
            {
                MainPanel.Children.Remove(Drawed);
                MainPanel.Children.Add(path);
                Drawed = path;
            }

        }

        //Создание кривой Безье между двумя точками
        private Path CreatePath(Vector start, Vector end, NodeSocket.SocketTypes nodeType)
        {
            double inc = (start - end).Length / 3;

            var add = new Vector(inc, 0);
            var offset = new Vector(5, 0);

            var mid1 = start + (nodeType == NodeSocket.SocketTypes.Output ? add : -add);
            var mid2 = end - (nodeType == NodeSocket.SocketTypes.Output ? add : -add);

            Point[] points = 
            {
                (Point)mid1,
                (Point)mid2,
                (Point)end
            };
            var pbs = new PolyBezierSegment(points, true);

            var psc = new PathSegmentCollection { pbs };


            var pf = new PathFigure((Point)start, psc, false);
            var pfc = new PathFigureCollection { pf };

            var path = new Path
            {
                Data = new PathGeometry(pfc),
                StrokeThickness = 1.5,
                Stroke = Brushes.White
            };
            //path.IsHitTestVisible = false;
            Canvas.SetZIndex(path, -1);

            path.MouseLeftButtonUp += Window_MouseLeftButtonUp;

            path.ContextMenu = DeleteContextMenu;



            return path;
        }

        //Сброс состояний активации создания соединения и перетаскивания\динамическое создание нода по типу исходящего
        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (NConnDrag && Drawed != null)
            {
                var dcm = new ContextMenu();
                var brush = new SolidColorBrush(new Color() { R = 30, G = 30, B = 30, A = 255 });
                dcm.Background = brush;

                List<NodeControl> list = new List<NodeControl>();
                foreach (var node in NodeLibrary.Nodes)
                    if (initNode.SocketType == NodeSocket.SocketTypes.Input)
                    {
                        var outpTyped = node.GetOutputConnectorList().Where(x => (x.DataType == initNode.DataType || (x.DataType.IsIConvertible() && initNode.DataType.IsIConvertible())) && x.Passive);
                        if (outpTyped.Count() == 0) continue;
                        list.Add(node);
                    }
                    else
                    {
                        var inpTyped = node.GetInputConnectorList().Where(x => (((x.DataType == initNode.DataType) && x.Passive) || (x.DataType.IsIConvertible() && initNode.DataType.IsIConvertible())) && x.Passive);
                        if (inpTyped.Count() == 0) continue;
                        list.Add(node);
                    }

                var CheckList = new Queue<TreeViewItem>();
                var FList = new List<MenuItem>();
                var AList = new List<MenuItem>();

                foreach (var node in NodesPanel.Items.OfType<TreeViewItem>())
                {
                    CheckList.Enqueue(node);
                    var mi = new MenuItem() { Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0), Header = node.Header, Tag = node.Tag };
                    AList.Add(mi);
                    FList.Add(mi);
                }

                while(CheckList.Count != 0)
                {
                    var node = CheckList.Dequeue();
                    if (node.HasItems)
                    {
                        foreach (var inNode in node.Items.OfType<TreeViewItem>())
                        {
                            CheckList.Enqueue(inNode);
                            var mi = new MenuItem() { Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0), Header = inNode.Header, Tag = inNode.Tag };
                            var res = AList.Find(x => x.Header == node.Header);
                            res.Items.Add(mi);
                            AList.Add(mi);
                        }
                        foreach(var lb in node.Items.OfType<Label>())
                        {
                            var mi = new MenuItem() { Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0), Header = lb.Content, Tag = lb.Tag };
                            var res = AList.Find(x => x.Header == node.Header);
                            res.Items.Add(mi);
                            AList.Add(mi);
                        }
                    }
                }





                //foreach (var i in FList) dcm.Items.Add(i);

                foreach (var nc in list)
                {
                    var mi = new MenuItem() { Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0), Header = nc.NCName, Tag = nc };
                    mi.Click += (x, y) =>
                    {
                        var nconn = nc.GetConnectorList().Where(a => a.SocketType != initNode.SocketType && (a.DataType == initNode.DataType || a.DataType.IsIConvertible() && initNode.DataType.IsIConvertible())).First();
                        if (nconn.SocketType == NodeSocket.SocketTypes.Input && nconn.ConnectedCurves.Count > 0)
                        {
                            MessageBox.Show("There are existing connections left");
                            Window_MouseLeftButtonDown(sender, e);
                            return;
                        }
                        if (initNode.SocketType == NodeSocket.SocketTypes.Input && initNode.ConnectedCurves.Count > 0)
                        {
                            MessageBox.Show("There are existing connections left");
                            Window_MouseLeftButtonDown(sender, e);
                            return;
                        }
                        var created = AddNC(mi.Header.ToString());

                        var margin = created.Margin;
                        var mx = Mouse.GetPosition(MainPanel).X;
                        var my = Mouse.GetPosition(MainPanel).Y;
                        margin.Top = my;
                        margin.Left = mx;
                        created.Margin = margin;

                        created.Loaded += (q, w) =>
                            {
                                var readyConn = created.GetConnectorList().First(a => a.SocketType != initNode.SocketType && (a.DataType == initNode.DataType || a.DataType.IsIConvertible() && initNode.DataType.IsIConvertible()));
                                NodeConnector_MouseLeftButtonUp(readyConn, e);
                                var red = readyConn.GetCenter(MainPanel);
                                var gr = new Vector(margin.Left, margin.Top);

                                var diff = red - gr;
                                margin = created.Margin;
                                margin.Top -= diff.Y;
                                margin.Left -= diff.X;
                                created.Margin = margin;

                                UpdateCurves(created, diff);
                            };
                    };
                    dcm.Items.Add(mi);
                }
                Drawed.ContextMenu = dcm;
                dcm.IsOpen = true;
                return;
            }



            NConnDrag = false;
            NNodeDrag = false;
            DraggingNC = null;

            if (initNode != null)
                initNode.SetBaseColor();
            initNode = null;

            if (Drawed == null) return;
            MainPanel.Children.Remove(Drawed);
            Drawed = null;
        }

        //деактивация динамического создания нода
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (NConnDrag && Drawed != null)
                if (Drawed.ContextMenu != null)
                {
                    NConnDrag = false;
                    Window_MouseLeftButtonUp(sender, e);
                }
        }


        //Загрузка приложения
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadNodesLibrary();

            //CreateBaseNode();
        }

        //Загрузка библиотеки нодов и постройка списка
        private void LoadNodesLibrary()
        {
            Sheduler.Start();
            NodeLibrary.InitializeLibrary();

            var categories = NodeLibrary.Nodes.Select(x => x.Category).Distinct().ToList();
            foreach (var cat in categories)
            {
                var incCat = cat.Split(new char[] { '/' });

                var tvs = NodesPanel.Items.OfType<TreeViewItem>().Where(x => ((string)x.Header) == incCat[0]);
                var asdq = NodesPanel.Items.OfType<TreeViewItem>();
                TreeViewItem tvitem = tvs.Count() > 0 ? tvs.First() : null;

                if (tvitem == null)
                {
                    tvitem = new TreeViewItem() { Header = incCat[0], Margin = new Thickness(0, 5, 10, 5) };
                    NodesPanel.Items.Add(tvitem);
                }


                var nodes = NodeLibrary.Nodes.Where(x => x.Category == incCat[0]);
                foreach (var node in nodes)
                {
                    if (tvitem.Items.OfType<Label>().Where(x => ((string)x.Content) == node.NCName).Count() != 0) continue;
                    tvitem.Items.Add(new Label() { Content = node.NCName, Margin = new Thickness(0, 0, 10, 0) });
                }

                string path = incCat[0];
                var preTvi = tvitem;

                for (int i = 1; i < incCat.Length; i++)
                {
                    path += "/" + incCat[i];
                    var tvis = preTvi.Items.OfType<TreeViewItem>().Where(x => ((string)x.Header) == path);
                    TreeViewItem tvi = tvis.Count() > 0 ? tvis.First() : null;
                    if (tvi == null)
                    {
                        tvi = new TreeViewItem() { Header = incCat[i], Margin = new Thickness(0, 5, 10, 5) };
                        preTvi.Items.Add(tvi);
                    }

                    var catNodes = NodeLibrary.Nodes.Where(x => x.Category == path);
                    foreach (var node in catNodes)
                    {
                        if (tvi.Items.OfType<Label>().Where(x => (string)x.Content == node.NCName).Count() != 0) continue;
                        tvi.Items.Add(new Label() { Content = node.NCName, Margin = new Thickness(0, 0, 10, 0) });
                    }

                    preTvi = tvi;
                }
            }
        }
        Random R = new Random();
        public void UpdateProcess(NodeControl updated)
        {
            Action visible = () => { PBarCanvas.Dispatcher.Invoke(() => { PBarCanvas.Visibility = System.Windows.Visibility.Visible; }); };
            Action hide = () => { PBarCanvas.Dispatcher.Invoke(() => { PBarCanvas.Visibility = System.Windows.Visibility.Hidden; }); };

            Action<int> add = (x) => { Progress.Dispatcher.Invoke(() => { Progress.Value += Progress.Maximum/x; }); };
            Progress.Value = 0;

            Action act = () =>
                {
                    List<Action> acts = null;
                    visible();
                    Thread.Sleep(5);
                    Dispatcher.Invoke(() => { acts = updateProcess(updated); });

                    
                    foreach (var a in acts)
                    {
                        a();
                        add(acts.Count);
                        Thread.Sleep(5);
                    }
                    hide();
                };

            Sheduler.DoIt(act);
            
        }

        public static class Sheduler
        {
            public static ConcurrentQueue<Action> shedule;

            public static void Start()
            {
                shedule = new ConcurrentQueue<Action>();
                var t = new Thread(() =>
                {
                    while (true)
                    {
                        if (shedule.Count != 0)
                        {
                            Action res = null;
                            if (shedule.TryDequeue(out res) && res != null)
                            {
                                res();
                            }
                        }
                    }
                });
                t.IsBackground = true;
                t.Start();
            }

            public static void DoIt(Action act)
            {
                shedule.Enqueue(act);
            }

        }





        List<Action> updateProcess(NodeControl updated)
        {
            List<Action> result = new List<Action>();
            var allNodes = MainPanel.Children.OfType<NodeControl>().ToList();
            var onlyClosed = allNodes.Where(x => x.GetOutputConnectorList().Count == 0);
            var onlyLast = allNodes.Where(x => x.GetOutputConnectorList().All(y => y.ConnectedCurves.Count == 0));
            var conc = onlyClosed.Concat(onlyLast).ToList();

            var starter = allNodes.Where(x => x.GetInputConnectorList().All(y => y.Passive == false));

            if (updated == null)
                foreach (var n in allNodes)
                    result.Add(n.Recalc);
            else
            {
                var chain = new List<NodeControl>();
                var toCheck = new Queue<NodeControl>();
                toCheck.Enqueue(updated);
                while (toCheck.Count != 0)
                {
                    var i = toCheck.Dequeue();
                    chain.Add(i);
                    var inSockets = i.GetInputConnectorList();
                    if (inSockets.Where(x => x.Passive).All(x => x.ConnectedCurves.Count != 0) || inSockets.Count == 0) //all inputs connected
                        foreach (var a in i.GetOutputConnectorList())
                            foreach (var curve in a.ConnectedCurves)
                                toCheck.Enqueue(curve.GetInputNC());
                }
                var b = new SolidColorBrush(Color.FromRgb((byte)(15 * R.Next(1, 18)), (byte)(15 * R.Next(1, 18)), (byte)(15 * R.Next(1, 18))));

                foreach (var geto in chain)
                {
                    result.Add(geto.Recalc);
                }
            }
            return result;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (NodesPanel.SelectedItem == null || NodesPanel.SelectedItem is TreeViewItem) return;
            var nc = AddNC((string)(NodesPanel.SelectedItem as Label).Content);



            Point center = new Point(ActualWidth / 2 - 255, ActualHeight / 2);
            center = this.TranslatePoint(center, MainPanel);

            var margin = nc.Margin;

            margin.Left = center.X;
            margin.Top = center.Y;

            nc.Margin = margin;

        }

        public NodeControl AddNC(string name)
        {
            var nc = new NodeControl(NodeLibrary.Nodes.Find(x => x.NCName == name).Method);
            nc.EventNodeDragLBDown += NodeControl_MouseLeftButtonDown;
            nc.EventNodeDragLBMove += NodeControl_MouseMove;
            MainPanel.MouseMove += NodeControl_MouseMove;
            nc.EventNodeConnectorMouseLBDown += NodeConnector_MouseLeftButtonDown;
            nc.EventNodeConnectorMouseLBUp += NodeConnector_MouseLeftButtonUp;
            nc.EventNodeConnectorMouseEnter += NodeConnector_MouseEnter;
            nc.EventNodeConnectorMouseLeave += NodeConnector_MouseLeave;
            nc.cbWindow = this;
            MainPanel.Children.Add(nc);
            return nc;
        }


        private Vector GetElementCenter(FrameworkElement element)
        {
            //Point relativePoint = element.TransformToAncestor(MainPanel).Transform(new Point(0, 0));
            //return new Vector(relativePoint.X + element.ActualWidth / 2, relativePoint.Y + element.ActualHeight / 2);
            return ((Vector)element.TransformToAncestor(MainPanel).Transform(new Point(element.ActualWidth / 2, element.ActualHeight / 2)));
        }

        public void DrawPoint(Point point)
        {
            DrawPoint(point, Brushes.White);
        }
        public void DrawPoint(Point point, Brush color)
        {
            NodeSocket nc = new NodeSocket(null, typeof(int));
            nc.SetColorStroke(color);

            MainPanel.Children.Add(nc);
            nc.Loaded += (a, b) =>
                {
                    var margin = nc.Margin;
                    margin.Left = point.X - nc.ActualWidth / 2;
                    margin.Top = point.Y - nc.ActualHeight / 2;
                    nc.Margin = margin;
                    nc.RenderTransform = new ScaleTransform(0.5, 0.5, nc.ActualWidth / 2, nc.ActualHeight / 2);
                };







        }

    }
}
