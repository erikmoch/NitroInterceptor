using WebSocketSharp;
using WebSocketSharp.Server;

using NitroInterceptor.Message;

namespace NitroInterceptor.Websocket
{
    public class NitroCommunication
    {
        private readonly string _socketUrl;

        private static WebSocketServer _server;

        public static WebSocketSessionManager Local => _server.WebSocketServices["/"].Sessions;
        public static Interceptor _interceptor;
        public static WebSocket remote;

        public NitroCommunication(string socketUrl, Interceptor interceptorInstance)
        {
            _socketUrl = socketUrl;
            _interceptor = interceptorInstance;
        }

        public void Start(int serverPort)
        {
            remote = new WebSocket(_socketUrl);

            _server = new WebSocketServer(serverPort);
            _server.AddWebSocketService<Server>("/");
            _server.Start();

            remote.OnMessage += (object sender, MessageEventArgs e) =>
            {
                NMessage message = new NMessage(e.RawData, false);
                _interceptor.OnMessage(message);

                if (!message.IsBlocked)
                    Local.Broadcast(message.ToBytes());
            };
            remote.OnOpen += (object sender, System.EventArgs e) => _interceptor.OnConnected();
            remote.OnClose += (object sender, CloseEventArgs e) => _interceptor.OnDisconnected(e.Reason);
        }

        public void Stop()
        {
            if (remote.IsAlive)
                remote.Close();
            if (_server.IsListening)
                _server.Stop();
        }

        public void SendToServer(NMessage message) => remote.Send(message.ToBytes());
        public void SendToClient(NMessage message) => Local.Broadcast(message.ToBytes());
    }


    public class Server : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            if (!NitroCommunication.remote.IsAlive)
            {
                NitroCommunication.remote.SslConfiguration = Context.WebSocket.SslConfiguration;
                NitroCommunication.remote.Connect();
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.IsBinary)
            {
               NMessage message = new NMessage(e.RawData, true);
               NitroCommunication._interceptor.OnMessage(message);

                if (!message.IsBlocked)
                    NitroCommunication.remote.Send(message.ToBytes());
            }
        }
    }
}