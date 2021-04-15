using System;

using WebSocketSharp;

using NitroInterceptor.Message;

namespace NitroInterceptor.Websocket
{
    public class NitroConnection
    {
        private readonly string _socketUrl;

        public event EventHandler<NMessage> OnMessageReceived;
        public event EventHandler<string> OnConnectionStopped;
        public event EventHandler OnConnectionStarted;

        private WebSocket remote;

        public NitroConnection(string socketUrl)
        {
            _socketUrl = socketUrl;
        }

        public void Connect()
        {
            remote = new WebSocket(_socketUrl);

            remote.OnOpen += Remote_OnOpen;
            remote.OnClose += Remote_OnClose;
            remote.OnMessage += Remote_OnMessage;

            remote.Connect();
        }

        private void Remote_OnOpen(object sender, EventArgs e) => OnConnected();
        private void Remote_OnClose(object sender, CloseEventArgs e) => OnDisconnected(e.Reason);
        private void Remote_OnMessage(object sender, MessageEventArgs e) => OnMessage(new NMessage(e.RawData));

        private void OnDisconnected(string reason) => OnConnectionStopped?.Invoke(this, reason);
        private void OnMessage(NMessage message) => OnMessageReceived?.Invoke(this, message);
        private void OnConnected() => OnConnectionStarted?.Invoke(this, EventArgs.Empty);

        public void SendPacket(NMessage message) => remote.Send(message.ToBytes());
    }
}