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
using System.Reflection;
using Microsoft.Win32;
//using System.IO;

namespace IPS
{
    /// <summary>
    /// Логика взаимодействия для NodeConnector.xaml
    /// </summary>
    public partial class NodeSocket : UserControl
    {
        public Brush BaseColorBody;
        public Brush BaseColorStroke;


        public NodeControl ParentNode;
        public SocketTypes SocketType;
        public Type DataType;
        public List<Path> ConnectedCurves;
        bool passive;
        ParameterInfo pInfo;
        Action updater;

        public bool Passive
        {
            get { return passive; }
            set
            {
                passive = value;
                CreateContent();
            }
        }
        object ActiveElement;
        DockPanel APanel;

        public Guid guid;

        protected NodeSocket(NodeControl parentNode, Type nodeDataType, SocketTypes nodeType)
        {
            InitializeComponent();
            ConnectedCurves = new List<Path>();
            BaseColorStroke = Brushes.Black;
            guid = Guid.NewGuid();


            SocketType = nodeType;
            DataType = nodeDataType;
            BaseColorBody = GetBaseColor(nodeDataType);
            ParentNode = parentNode;
            SetBaseColor();

            HorizontalAlignment = nodeType == SocketTypes.Input
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Right;

            updater = () =>
            {
                parentNode.cbWindow.UpdateCurves(parentNode);
                parentNode.cbWindow.UpdateProcess(ParentNode);
            };

            ParentNode.LoadSocketEvents(this);

        }

        /// <summary>
        /// Для входящих
        /// </summary>
        public NodeSocket(NodeControl parentNode, ParameterInfo PInfo)
            : this(parentNode, PInfo.ParameterType, SocketTypes.Input)
        {
            Name = PInfo.Name;
            pInfo = PInfo;
            Passive = pInfo.GetCustomAttribute<InField>() == null;
        }
        /// <summary>
        /// Для исходящих
        /// </summary>
        public NodeSocket(NodeControl parentNode, Type nodeDataType)
            : this(parentNode, nodeDataType, SocketTypes.Output)
        {
            Passive = true;
        }


        Ellipse passiveCircle = null;
        protected void CreateContent()
        {

            DPanel.Children.Clear();
            if (SocketType == SocketTypes.Output)
            {
                var e = PassiveCircle;
                var l = PassiveLabel;
                l.Content = DataType.Name;
                DPanel.Children.Add(l);
                DPanel.Children.Add(e);
                this.HorizontalAlignment = HorizontalAlignment.Right;
                passiveCircle = e;
            }
            else if (Passive)
            {
                var e = PassiveCircle;
                var l = PassiveLabel;
                l.Content = Name + " \\ " + DataType.Name;
                l.HorizontalAlignment = HorizontalAlignment.Right;
                DPanel.Children.Add(e);
                DPanel.Children.Add(l);
                this.HorizontalAlignment = HorizontalAlignment.Left;
                passiveCircle = e;
                DPanel.ContextMenu = ActivateCM;
            }
            else
            {
                var panel = InFieldPanel(pInfo, ParentNode);
                DPanel.Children.Add(panel);
            }
            SetBaseColor();
            if (ConnectedCurves.Count != 0)
                ParentNode.NodeConnectorClearCurves(this);

        }

        public object GetData()
        {
            if (SocketType == SocketTypes.Input)
            {
                if (Passive)
                    if (ConnectedCurves.Count != 0)
                        return ConnectedCurves.First().GetOutputSocket().GetData();
                    else return null;
                else
                {
                    return DPanel.Dispatcher.Invoke(() => { var first = DPanel.Children.OfType<DockPanel>().First(); return (first.Tag as Func<object>).Invoke(); });
                }
            }
            else
            {
                var osocks = ParentNode.GetOutputConnectorList();
                var thissockid = osocks.IndexOf(this);
                if (ParentNode.Data.Length > thissockid)
                    return ParentNode.Data[thissockid];
                else
                    return null;
            }
        }

        public Point GetCenterP(Visual ancestor)
        {
            if (passive)
            {
                var ellipse = DPanel.Children.OfType<Ellipse>().First();
                return
                    ellipse.TransformToAncestor(ancestor)
                        .Transform(new Point(ellipse.ActualWidth / 2, ellipse.ActualHeight / 2));
            }
            else
                throw new NotImplementedException("GetCenterP for Active");
            //return Connector.TransformToAncestor(ancestor).Transform(new Point(Connector.ActualWidth / 2, Connector.ActualHeight / 2));
        }
        public Vector GetCenter(Visual ancestor)
        {
            return (Vector)GetCenterP(ancestor);
        }

        public void SetBaseColor()
        {
            if (passiveCircle != null)
                passiveCircle.Stroke = GetBaseColor(DataType);
        }
        public void SetColorStroke(Brush brush)
        {
            if (passiveCircle != null)
                passiveCircle.Stroke = brush;
        }

        public Brush GetBaseColor(Type type)
        {
            Brush outp;
            bool good = TypeColors.TryGetValue(type, out outp);
            if (good)
                return outp;
            else
                return Brushes.White;
        }

        public enum SocketTypes { Input, Output }
        static Dictionary<Type, Brush> TypeColors = new Dictionary<Type, Brush>()
            {
                {typeof(double), Brushes.Aquamarine},
                {typeof(int), Brushes.Blue},
                {typeof(byte), Brushes.Violet},
                {typeof(string), Brushes.Red},
                {typeof(System.Drawing.Bitmap), Brushes.Green}
            };

        static Ellipse PassiveCircle
        {
            get
            {
                return new Ellipse
                {
                    Stroke = Brushes.Black,
                    Fill = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 20,
                    Height = 20
                };
            }
        }
        static Label PassiveLabel
        {
            get
            {
                return new Label
                {
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, -4, 0, -5)
                };
            }
        }

        ContextMenu ActivateCM
        {
            get
            {
                var dcm = new ContextMenu();
                var brush = new SolidColorBrush(new Color() { R = 30, G = 30, B = 30, A = 255 });
                dcm.Background = brush;
                var mi = new MenuItem() { Header = "Make Field", Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0) };
                mi.Click += (x, y) => { this.Passive = false; };
                var mi2 = new MenuItem() { Header = "Disconnect", Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0) };
                mi2.Click += (x, y) => { if (ConnectedCurves.Count != 0) ParentNode.NodeConnectorClearCurves(this); };
                dcm.Items.Add(mi);
                dcm.Items.Add(mi2);
                return dcm;
            }
        }

        ContextMenu DeactivateCM
        {
            get
            {
                var dcm = new ContextMenu();
                var brush = new SolidColorBrush(new Color() { R = 30, G = 30, B = 30, A = 255 });
                dcm.Background = brush;
                var mi = new MenuItem() { Header = "Make Socket", Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0) };
                mi.Click += (x, y) => { this.Passive = true; };
                dcm.Items.Add(mi);
                return dcm;
            }
        }


        public void SetActiveFieldValue(object val)
        {
            if (Passive) return;
            var type = ActiveElement.GetType();
            if (type == typeof(TextBox))
            {
                var tb = (TextBox)ActiveElement;
                tb.Text = val as string;
            }
            if (type == typeof(Slider))
            {
                var sl = (Slider)ActiveElement;
                float res;
                var value = Single.TryParse(val as string, out res) ? res : 1.0f;
                sl.Maximum = value + 100;
                sl.Minimum = value < 0 ? value - 100 : 0f;
                sl.Value = value;

                var tbs = APanel.Children.OfType<TextBox>().ToArray();
                tbs[0].Text = sl.Minimum.ToString();
                tbs[1].Text = sl.Maximum.ToString();

                (sl.Tag as Action).Invoke();

            }
            if (type == typeof(Button))
            {
                var b = (Button)ActiveElement;//продолжить
                var path = val as string;
                if (System.IO.File.Exists(path))
                {
                    System.Drawing.Bitmap bmp = null;
                    try
                    {
                        bmp = new System.Drawing.Bitmap(path);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    bmp.Tag = path;
                    APanel.Tag = new Func<object>(() => { var lbmp = bmp; return lbmp; });
                }
            }
        }

        public string GetSaveValue()
        {
            if (Passive) return "$";
            var data = (DPanel.Children.OfType<DockPanel>().First().Tag as Func<object>)();
            if (data == null) throw new Exception("A bit problem with Tags. Alarm!");
            if (data.GetType() == typeof(System.Drawing.Bitmap))
                return (data as System.Drawing.Bitmap).Tag as string;
            else
                return data.ToString();
        }

        DockPanel InFieldPanel(ParameterInfo pInfo, NodeControl parent)
        {
            DockPanel dock = new DockPanel { MinWidth = 50, MinHeight = 25, Margin = new Thickness(5) };
            InField inField = pInfo.GetCustomAttribute<InField>();

            inField = inField == null ? new InField() { PVal = 0 } : inField;

            dock.ContextMenu = DeactivateCM;

            if (pInfo.ParameterType == typeof(string) || pInfo.ParameterType == typeof(int) || pInfo.ParameterType == typeof(double))
            {
                var tb = new TextBox() { MinWidth = 50, MinHeight = 25, Margin = new Thickness(0, 0, 2, 0) };
                ActiveElement = tb;
                var lb = new Label() { Margin = new Thickness(2, 0, 0, 0), Content = string.Format("{0} [{1}]", pInfo.Name, inField.PVal), Foreground = Brushes.White };
                dock.Children.Add(tb);
                dock.Children.Add(lb);

                if (inField.PVal != null)
                    tb.Text = inField.PVal.ToString();
                tb.TextChanged += (a, b) =>
                {
                    parent.cbWindow.UpdateCurves(parent);
                    parent.cbWindow.UpdateProcess(parent);
                };
                dock.Tag = new Func<object>(() => { return tb.Text; });
            }
            if (pInfo.ParameterType == typeof(float))
            {
                var tb1 = new TextBox() { MinWidth = 25, MinHeight = 25, Margin = new Thickness(0, 0, 2, 0), Text = inField.Min.ToString() };
                var tb2 = new TextBox() { MinWidth = 25, MinHeight = 25, Margin = new Thickness(0, 0, 2, 0), Text = inField.Max.ToString() };
                var sl = new Slider() { Minimum = inField.Min, Maximum = inField.Max, MinWidth = 100, Value = Convert.ToDouble(inField.PVal), IsMoveToPointEnabled = true, AutoToolTipPlacement = System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft, AutoToolTipPrecision = 2 };
                var lb = new Label() { Margin = new Thickness(2, 0, 0, 0), Content = string.Format("{0} [{1}] Val: {2}", pInfo.Name, inField.PVal, sl.Value), Foreground = Brushes.White };
                dock.Children.Add(tb1);
                dock.Children.Add(sl);
                dock.Children.Add(tb2);
                dock.Children.Add(lb);

                ActiveElement = sl;

                tb1.TextChanged += (a, b) =>
                {
                    float parse;
                    if (!float.TryParse(tb1.Text, out parse))
                    {
                        tb1.Foreground = Brushes.Red;
                        return;
                    }
                    tb1.Foreground = Brushes.Black;
                    sl.Minimum = parse;
                    //parent.cbWindow.UpdateCurves(parent);
                    //parent.cbWindow.UpdateProcess(parent);
                };
                tb2.TextChanged += (a, b) =>
                {
                    float parse;
                    if (!float.TryParse(tb2.Text, out parse))
                    {
                        tb2.Foreground = Brushes.Red;
                        return;
                    }
                    tb2.Foreground = Brushes.Black;
                    sl.Maximum = parse;
                    //parent.cbWindow.UpdateCurves(parent);
                    //parent.cbWindow.UpdateProcess(parent);
                };

                sl.PreviewMouseUp += (a, b) =>
                    {

                        var val = Math.Round((decimal)sl.Value, 2);
                        lb.Content = string.Format("{0} [{1}] Val: {2}", pInfo.Name, inField.PVal, val);
                        if (KeyboardMods.ChainUpdate)
                        {
                            parent.cbWindow.UpdateCurves(parent);
                            parent.cbWindow.UpdateProcess(parent);
                        }
                        else
                        {
                            parent.Recalc();
                        }
                    };
                sl.Tag = new Action(() =>
                {
                    lb.Content = string.Format("{0} [{1}] Val: {2}", pInfo.Name, inField.PVal, sl.Value);
                });

                dock.Tag = new Func<object>(() => { return sl.Value; });
            }

            if (pInfo.ParameterType == typeof(System.Drawing.Bitmap))
            {
                var b = new Button() { Width = 50, Height = 25, Content = "Load" };
                var lb = new Label() { Margin = new Thickness(2, 0, 0, 0), Content = string.Format("{0}", pInfo.Name), Foreground = Brushes.White };
                dock.Tag = new Func<object>(() => { return null; });
                b.Click += (x, y) =>
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    if (ofd.ShowDialog() == true)
                    {
                        if (System.IO.File.Exists(ofd.FileName))
                        {
                            System.Drawing.Bitmap bmp = null;
                            try
                            {
                                bmp = new System.Drawing.Bitmap(ofd.FileName);
                            }
                            catch (Exception e)
                            {
                                MessageBox.Show(e.Message);
                                return;
                            }
                            bmp.Tag = ofd.FileName;
                            dock.Tag = new Func<object>(() => { var lbmp = bmp; return lbmp; });
                            lb.Content = string.Format("{0}: {1}\r\n({2}\\{3})", pInfo.Name, ofd.FileName, bmp.Size, System.IO.File.ReadAllBytes(ofd.FileName).Length / 1024 + "Kb");
                        }
                    }

                    parent.cbWindow.UpdateProcess(parent);
                };
                ActiveElement = b;
                dock.Children.Add(b);
                dock.Children.Add(lb);
            }
            if (pInfo.ParameterType == typeof(bool))
            {
                var cb = new ComboBox() { Margin = new Thickness(0, 0, 2, 0) };
                var lb = new Label() { Margin = new Thickness(2, 0, 0, 0), Content = string.Format("{0} [{1}]", pInfo.Name, inField.PVal), Foreground = Brushes.White };
                dock.Children.Add(cb);
                dock.Children.Add(lb);

                bool isTrue = true;

                if (inField.PVal != null)
                    isTrue = (bool)inField.PVal;

                cb.Items.Add(new ComboBoxItem() { Content = "True", IsSelected = isTrue });
                cb.Items.Add(new ComboBoxItem() { Content = "False", IsSelected = !isTrue });
                cb.SelectionChanged += (x, y) =>
                {
                    parent.cbWindow.UpdateProcess(parent);
                };
                ActiveElement = cb;
                dock.Tag = new Func<object>(() => { return Convert.ToBoolean((cb.SelectedItem as ComboBoxItem).Content); });
            }
            if (pInfo.ParameterType.IsEnum)
            {
                var cb = new ComboBox() { Margin = new Thickness(0, 0, 2, 0) };
                var lb = new Label() { Margin = new Thickness(2, 0, 0, 0), Content = string.Format("{0} [{1}]", pInfo.Name, inField.PVal), Foreground = Brushes.White };
                dock.Children.Add(cb);
                dock.Children.Add(lb);

                var selected = inField.PVal;

                var enumNames = pInfo.ParameterType.GetEnumNames();
                if (selected != null)
                    foreach (var name in enumNames)
                        cb.Items.Add(new ComboBoxItem() { Content = name, IsSelected = name == selected.ToString() ? true : false });
                else
                {
                    foreach (var name in enumNames)
                        cb.Items.Add(new ComboBoxItem() { Content = name });
                    (cb.Items[0] as ComboBoxItem).IsSelected = true;
                }


                cb.SelectionChanged += (x, y) =>
                {
                    parent.cbWindow.UpdateProcess(parent);
                };
                ActiveElement = cb;
                dock.Tag = new Func<object>(() => { return (string)(cb.SelectedItem as ComboBoxItem).Content; });
            }
            if (pInfo.ParameterType == typeof(System.Drawing.Brush))
            {
                var cb = new ComboBox() { Margin = new Thickness(0, 0, 2, 0) };
                var lb = new Label() { Margin = new Thickness(2, 0, 0, 0), Content = pInfo.Name, Foreground = Brushes.White };
                dock.Children.Add(lb);
                dock.Children.Add(cb);


                var names = typeof(Brushes).GetProperties().Select(x => x.Name).ToArray();

                var selected = inField.PVal == null ? names[0] : inField.PVal;

                foreach (var name in names)
                    cb.Items.Add(new ComboBoxItem() { Content = name, IsSelected = name == selected.ToString() ? true : false });

                cb.SelectionChanged += (x, y) =>
                {
                    parent.cbWindow.UpdateProcess(parent);
                };
                ActiveElement = cb;
                dock.Tag = new Func<object>(() => { return (string)(cb.SelectedItem as ComboBoxItem).Content; });

            }
            APanel = dock;
            return dock;
        }
        float Clamp(float val, float min, float max)
        {
            if (val > max) return max;
            if (val < min) return min;
            return val;
        }

    }
}
