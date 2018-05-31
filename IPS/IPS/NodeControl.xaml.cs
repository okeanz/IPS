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
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Win32;
using System.ComponentModel;


namespace IPS
{
    /// <summary>
    /// Логика взаимодействия для NodeControl.xaml
    /// </summary>
    public partial class NodeControl : UserControl
    {
        public string NCName
        {
            get
            {
                return NodeName.Content.ToString();
            }
            set
            {
                NodeName.Content = value;
            }
        }
        public string Category;
        public Type[] Input, Output;


        public MainWindow cbWindow;

        public MethodInfo Method;

        public List<NodeSocket> NCList
        {
            get
            {
                return RightPanel.Children.OfType<NodeSocket>().Concat(LeftPanel.Children.OfType<NodeSocket>()).ToList();
            }
        }

        public List<NodeSocket> InputSockets, OutputSockets;

        public NodeControl()
        {
            InitializeComponent();
            //this.Width = 250;
            //this.Height = 200;
            this.Margin = new Thickness(0);
            this.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            this.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            this.RenderTransform = new ScaleTransform(0.5, 0.5);

            Header.ContextMenu = MainWindow.DeleteContextMenu;

            var brush = new SolidColorBrush(new Color() { R = 30, G = 30, B = 30, A = 255 });
            Header.ContextMenu.Items.Insert(0, new MenuItem() { Header = "Обновить", Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0) });

            (Header.ContextMenu.Items[1] as MenuItem).Click += (x, y) =>
            {
                foreach (var ncon in GetConnectorList())
                    NodeConnectorClearCurves(ncon);
                cbWindow.MainPanel.Children.Remove(this);
                cbWindow.UpdateProcess(this);
            };

            (Header.ContextMenu.Items[0] as MenuItem).Click += (x, y) =>
            {
                cbWindow.UpdateProcess(this);
            };

            this.MouseDown += NodeControl_MouseDown;


        }



        public NodeControl(MethodInfo m)
            : this()
        {
            NFuncAttribute att = m.GetCustomAttribute<NFuncAttribute>();
            ParameterInfo[] param = m.GetParameters();

            NCName = att.Name;
            Category = att.Category;
            Input = param.Select(x => x.ParameterType).ToArray();
            if (att.OutputTypes == null)
            {
                Output = m.ReturnType == typeof(void) ? new Type[] { } : new Type[] { m.ReturnType };
            }
            else
                Output = att.OutputTypes;

            Method = m;


            foreach (var t in param)
            {
                bool isOut = t.IsOut;

                if (!isOut)
                {
                    NodeSocket nc = new NodeSocket(this, t);
                    LeftPanel.Children.Add(nc);
                    continue;
                }
                if (isOut)
                {
                    if (t.ParameterType.GetElementType() == typeof(string))
                    {
                        var lb = new Label() { MinWidth = 25, MinHeight = 25, Foreground = Brushes.White };
                        MiddlePanel.Children.Add(lb);
                        continue;
                    }
                    if (t.ParameterType.GetElementType() == typeof(BitmapImage))
                    {
                        var img = new Image() { Stretch = Stretch.Fill, MaxHeight = 500, MaxWidth = 800 };
                        MiddlePanel.Children.Add(img);

                        var dcm = new ContextMenu();
                        var brush = new SolidColorBrush(new Color() { R = 30, G = 30, B = 30, A = 255 });
                        dcm.Background = brush;
                        var mi = new MenuItem() { Header = "Open full size", Background = brush, Foreground = Brushes.White, Margin = new Thickness(-3, 0, 0, 0) };
                        mi.Click += (x, y) =>
                        {
                            if (img.Source != null)
                            {
                                var bmp = NodeLibrary.BI2B(img.Source as BitmapImage);
                                Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte> imge = new Emgu.CV.Image<Emgu.CV.Structure.Bgr, byte>(bmp);
                                imge.Save("temp.png");
                                Process.Start("temp.png");
                            }
                        };
                        dcm.Items.Add(mi);

                        img.ContextMenu = dcm;
                        continue;
                    }
                    var aasdqw = t.ParameterType.GetElementType();

                    if (t.ParameterType.GetElementType().BaseType == typeof(Array) || t.ParameterType.GetElementType() == typeof(Array))
                    {
                        var arr = new TableControl();

                        MiddlePanel.Children.Add(arr);


                        continue;
                    }
                }
            }

            if (att.OutputTypes == null && Output.Length != 0)
            {
                NodeSocket ndc = new NodeSocket(this, Output[0]);
                RightPanel.Children.Add(ndc);
            }
            else
                foreach (var t in Output)
                {
                    NodeSocket ndc = new NodeSocket(this, t);
                    RightPanel.Children.Add(ndc);
                }

            InputSockets = LeftPanel.Children.OfType<NodeSocket>().ToList();
            OutputSockets = RightPanel.Children.OfType<NodeSocket>().ToList();

            if (RightPanel.Children.Count == 0)
                RPBorder.Visibility = System.Windows.Visibility.Hidden;


        }

        public void LoadSocketEvents(NodeSocket nc)
        {
            nc.MouseLeftButtonDown += NodeConnector_MouseDown;
            nc.MouseLeftButtonUp += NodeConnector_MouseUp;
            nc.MouseEnter += NodeConnector_MouseEnter;
            nc.MouseLeave += NodeConnector_MouseLeave;
            nc.ContextMenu = MainWindow.DeleteContextMenu;
            var nc1 = nc;
            (nc.ContextMenu.Items[0] as MenuItem).Click += (x, y) => { NodeConnectorClearCurves(nc1); };
        }

        public object[] GetInput()
        {
            var outp = new List<object>();
            var elements = InputSockets;
            foreach (var sock in elements)
                outp.Add(sock.GetData());
            return outp.ToArray();
        }

        public object[] GetOutput()
        {
            var m = Method;
            ParameterInfo[] param = m.GetParameters();
            var method = m;
            var input = GetInput();
            object[] output = new object[] { null };



            object[] parameters;
            List<object> listinput = input.ToList();
            List<object> listParams = new List<object>();
            int counter = 0;
            for (int i = 0; i < param.Length; i++)
            {
                ParameterInfo next = param[i];

                if (!next.IsOut && counter < input.Length)
                {

                    var value = input[counter];
                    if (value == null)
                    {
                        var iData = new List<object>();
                        for (int t = 0; t < param.Length; t++)
                        {
                            if (param[t].IsOut)
                                iData.Add(null);
                        }
                        WriteMiddle(iData.ToArray());
                        return output;
                    }


                    listParams.Add(input[counter++]);
                }
                else
                {
                    listParams.Add(null);
                }
            }

            parameters = listParams.ToArray();

            object processed = null;
            try
            {
                ConvertFields(parameters, param);
                processed = method.Invoke(null, parameters);

                cbWindow.Dispatcher.Invoke(() =>
                {
                    MainBorder.BorderBrush = Brushes.Black;
                    ToolTip = null;
                });
            }
            catch (Exception e)
            {
                cbWindow.Dispatcher.Invoke(() =>
                {
                    MainBorder.BorderBrush = Brushes.Red;
                    ToolTip = new ToolTip() { Content = e };
                });
                return output;
            }
            bool retVoid = method.ReturnType == typeof(void);
            bool retObjArr = method.ReturnType == typeof(object[]);

            if (processed != null)
                if (retObjArr)
                    output = (object[])processed;
                else
                    if (!retVoid)
                        output = new object[] { processed };
                    else
                        return output;
            else
                if (!retVoid)
                    return output;

            var infoData = new List<object>();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (param[i].IsOut)
                    infoData.Add(parameters[i]);
            }
            WriteMiddle(infoData.ToArray());

            return output;
        }


        object[] data;
        public object[] Data
        {
            get
            {
                if (data == null)
                    Recalc();
                return data;
            }
        }
        public void Recalc()
        {
            data = GetOutput();
        }

        //AltUpdate
        void NodeControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (KeyboardMods.AltMod)
                cbWindow.UpdateProcess(this);
        }


        void ConvertFields(object[] array, ParameterInfo[] param)
        {
            if (array.Length != param.Length) throw new Exception("array.length != param.length");
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] is IConvertible && param[i].ParameterType.GetInterfaces().Contains(typeof(IConvertible)) && !param[i].ParameterType.IsEnum)
                    array[i] = Convert.ChangeType(array[i], param[i].ParameterType);

                if (param[i].ParameterType.IsEnum)
                    array[i] = Enum.Parse(param[i].ParameterType, array[i] as string);

                if (param[i].ParameterType == typeof(System.Drawing.Brush))
                {
                    array[i] = typeof(System.Drawing.Brushes).GetProperty(array[i].ToString()).GetValue(null);
                }
            }
        }


        string[] GetInpFields()
        {
            var list = LeftPanel.Children.OfType<TextBox>().Select<TextBox, string>(x => x.Text);
            return list.ToArray();
        }

        void WriteMiddle(object[] fields)
        {
            FrameworkElement[] mpels = null;

            mpels = MiddlePanel.Dispatcher.Invoke(() => { return MiddlePanel.Children.OfType<FrameworkElement>().ToArray(); });

            for (int i = 0; i < mpels.Count(); i++)
            {
                if (mpels[i] is Label)
                {
                    var lb = mpels[i] as Label;
                    lb.Dispatcher.Invoke(() => { lb.Content = null; cbWindow.UpdateCurves(this); });
                }
                if (mpels[i] is Image)
                {
                    var img = mpels[i] as Image;
                    cbWindow.Dispatcher.Invoke(() => { img.Source = null; cbWindow.UpdateCurves(this); });
                }
                if (mpels[i] is TableControl)
                {
                    var tc = mpels[i] as TableControl;
                    tc.Dispatcher.Invoke(() => { tc.UpdateArray(null); cbWindow.UpdateCurves(this); });
                }
            }

            for (int i = 0; i < mpels.Count() && i < fields.Length; i++)
            {
                if (mpels[i] is Label)
                {
                    var lb = mpels[i] as Label;
                    lb.Dispatcher.Invoke(() => { lb.Content = fields[i] == null ? "" : fields[i]; cbWindow.UpdateCurves(this); });
                }
                if (mpels[i] is Image)
                {
                    var img = mpels[i] as Image;
                    if (fields[i] != null)
                    {
                        var imgs = (fields[i] as ImageSource).Clone();
                        imgs.Freeze();
                        cbWindow.Dispatcher.Invoke(() => { var sad = imgs.Clone(); img.Source = sad; cbWindow.UpdateCurves(this); });
                    }
                    else
                        cbWindow.Dispatcher.Invoke(() => { img.Source = null; });
                }
                if (mpels[i] is TableControl)
                {
                    var tc = mpels[i] as TableControl;
                    if (fields[i] != null)
                    {
                        tc.Dispatcher.Invoke(() => { tc.UpdateArray(fields[i] as Array); cbWindow.UpdateCurves(this); });
                    }

                }

            }
        }

        public List<NodeSocket> GetConnectorList()
        {
            return InputSockets.Concat(OutputSockets).ToList();
        }

        public List<NodeSocket> GetInputConnectorList()
        {
            return GetConnectorList().Where(x => x.SocketType == NodeSocket.SocketTypes.Input).ToList();
        }

        public List<NodeSocket> GetOutputConnectorList()
        {
            return GetConnectorList().Where(x => x.SocketType == NodeSocket.SocketTypes.Output).ToList();
        }

    }

}
