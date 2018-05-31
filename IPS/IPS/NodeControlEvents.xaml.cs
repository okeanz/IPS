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
    public partial class NodeControl : UserControl
    {
        public MouseButtonEventHandler EventNodeConnectorMouseLBDown;
        public MouseButtonEventHandler EventNodeConnectorMouseLBUp;
        public MouseEventHandler EventNodeConnectorMouseEnter;
        public MouseEventHandler EventNodeConnectorMouseLeave;

        public MouseButtonEventHandler EventNodeDragLBDown;
        public MouseEventHandler EventNodeDragLBMove;

        void NodeConnector_MouseEnter(object sender, MouseEventArgs e)
        {
            if (EventNodeConnectorMouseEnter != null)
            {
                EventNodeConnectorMouseEnter(sender, e);
            }
        }

        void NodeConnector_MouseLeave(object sender, MouseEventArgs e)
        {
            if (EventNodeConnectorMouseLeave != null)
            {
                EventNodeConnectorMouseLeave(sender, e);
            }
        }

        //Нажатие на NodeConnector
        private void NodeConnector_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (EventNodeConnectorMouseLBDown != null)
            {
                EventNodeConnectorMouseLBDown(sender, e);
            }
        }

        //Активация события отпускания клавши над NodeConnector
        private void NodeConnector_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (EventNodeConnectorMouseLBUp != null)
            {
                EventNodeConnectorMouseLBUp(sender, e);
            }
        }

        //Активация события нажатия на Node для перетаскивания
        private void NodeDrag_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (EventNodeDragLBDown != null)
            {
                EventNodeDragLBDown(sender, e);
            }
        }

        //Активация события движения мыши при перетаскивании Node
        private void NodeDrag_MouseMove(object sender, MouseEventArgs e)
        {
            if (EventNodeDragLBMove != null)
            {
                EventNodeDragLBMove(sender, e);
            }
        }



        //Событие нажатия на заголовок Node
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            NodeDrag_MouseDown(this, e);
        }

        //Событие движения мыжи по заголовку Node
        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            NodeDrag_MouseMove(this, e);
        }

        //Удаление связей между нодами из контекстного меню
        public void NodeConnectorClearCurves(NodeSocket initnode)
        {
            foreach (var curve in initnode.ConnectedCurves)
            {
                var endnode = initnode.SocketType == NodeSocket.SocketTypes.Input ? curve.GetOutputSocket() : curve.GetInputSocket();
                cbWindow.MainPanel.Children.Remove(curve);
                endnode.ConnectedCurves.Remove(curve);
            }
            initnode.ConnectedCurves.Clear();
            cbWindow.UpdateProcess(initnode.ParentNode);
        }

    }
}
