using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IPS
{
    public static class NodePath
    {
        public static NodeSocket GetInitSocket(this Path path)
        {
            if (path.Tag == null) return null;
            var sockets = path.Tag as NodeSocket[];
            return sockets[0];
        }
        public static NodeSocket GetEndSocket(this Path path)
        {
            if (path.Tag == null) return null;
            var sockets = path.Tag as NodeSocket[];
            return sockets[1];
        }



        public static void SetInitSocket(this Path path, NodeSocket socket)
        {
            if (path.Tag == null) 
                path.Tag = new NodeSocket[2];
            (path.Tag as NodeSocket[])[0] = socket;
        }
        public static void SetEndSocket(this Path path, NodeSocket socket)
        {
            if (path.Tag == null)
                path.Tag = new NodeSocket[2];
            (path.Tag as NodeSocket[])[1] = socket;
        }

        public static void SetSockets(this Path path, NodeSocket init, NodeSocket end)
        {
            path.Tag = new NodeSocket[] {init, end};
        }

        public static NodeSocket[] GetSockets(this Path path)
        {
            return path.Dispatcher.Invoke(() => { return path.Tag as NodeSocket[]; });
        }

        public static NodeControl GetInputNC(this Path path)
        {
            return GetInputSocket(path).ParentNode;
        }

        public static NodeControl GetOutputNC(this Path path)
        {
            return GetOutputSocket(path).ParentNode;
        }

        public static NodeSocket GetInputSocket(this Path path)
        {
            var sockets = path.GetSockets();
            var inSock = sockets.First(x => x.SocketType == NodeSocket.SocketTypes.Input);
            return inSock;
        }
        public static NodeSocket GetOutputSocket(this Path path)
        {
            var sockets = path.GetSockets();
            var outSock = sockets.First(x => x.SocketType == NodeSocket.SocketTypes.Output);
            return outSock;
        }
    }
}
