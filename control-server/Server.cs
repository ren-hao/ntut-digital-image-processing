using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace control_server
{
    class Server :IDisposable
    {
        private readonly string ADDR;
        private readonly int PORT;
        private WebSocketServer _server;
        private bool _isServerStarted = false;
        private List<IWebSocketConnection> _clients = new List<IWebSocketConnection>();

        /// <summary>   Constructor. </summary>
        ///
        /// <param name="addr"> The address. </param>
        /// <param name="port"> The port. </param>
        public Server(string addr, int port)
        {
            this.ADDR = addr;
            this.PORT = port;

            _server = new WebSocketServer("ws://" + addr + ":" + PORT);
        }

        /// <summary>   Starts Server. </summary>
        public void Start()
        {
            if (_isServerStarted) return;
            _clients.Clear();
            _server.ListenerSocket.NoDelay = true;
            _server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    Console.WriteLine("Open!");
                    _clients.Add(socket);
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Close!");
                    _clients.Remove(socket);
                };
            });
            _isServerStarted = true;
        }

        /// <summary>   Sends to all clients. </summary>
        ///
        /// <param name="str">  The string. </param>
        public void SendToAll(string str)
        {
            for (var i = _clients.Count - 1; i >= 0 && i < _clients.Count; i--)
                _clients[i].Send(str);
        }

        /// <summary>   Sends to all clients. </summary>
        ///
        /// <param name="data"> The data. </param>
        public void SendToAll(byte[] data)
        {
            for (var i = _clients.Count - 1; i >= 0 && i < _clients.Count; i--)
                _clients[i].Send(data);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        /// resources.
        /// </summary>
        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
