using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace ApolloLensLibrary.Signalling
{
    /// <summary>
    /// An implementation of the IBasicSignaller interface
    /// using websockets. Very simple code. Very simple
    /// corresponding server is also possible.
    /// </summary>
    public class WebsocketSignaller : IBasicSignaller
    {
        private MessageWebSocket WebSocket { get; set; }

        public event EventHandler ConnectionSucceeded;
        public event EventHandler ConnectionFailed;
        /// <summary>
        /// Intended as a UI-handler for connections ended unexpectedly.
        /// </summary>
        public event EventHandler ConnectionEnded;
        public event EventHandler<string> ReceivedMessage;
        public bool connected { get; set; } = false;
        public string identity { get; set; }

        // moved
        /// <summary>
        /// Constructor. Defines connection identity.
        /// </summary>
        /// <param name="identity">"client" or "source"</param>
        public WebsocketSignaller(string connectionIdentity)
        {
            this.identity = connectionIdentity;
            this.ConnectionSucceeded += WebsocketSignaller_ConnectionSucceeded;
        }

        /// <summary>
        /// Connect to the server at the specified address.
        /// </summary>
        /// <param name="address">
        /// Needs to be in the form "ws://..." or "wss://..."
        /// </param>
        /// <returns></returns>
        public async Task ConnectToServer(string address)
        {
            try
            {
                this.WebSocket = new MessageWebSocket();
                this.WebSocket.Control.MessageType = SocketMessageType.Utf8;
                this.WebSocket.MessageReceived += this.WebSocket_MessageReceived;
                this.WebSocket.Closed += this.WebSocket_Closed;
                this.connected = true;
                await this.WebSocket.ConnectAsync(new Uri(address));
                this.ConnectionSucceeded?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                this.connected = false;
                this.ConnectionFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void DisconnectFromServer()
        {
            this.connected = false;
            this.WebSocket.Close(1000, "");
        }

        public async Task SendMessage(string message)
        {
            // This should probably throw an exception
            // instead of quietly returning.
            if (this.WebSocket == null)
                return;

            // Use a datawriter to write the specified 
            // message to the websocket.
            using (var dataWriter = new DataWriter(this.WebSocket.OutputStream))
            {
                dataWriter.WriteString(message);
                await dataWriter.StoreAsync();
                dataWriter.DetachStream();
            }
        }

        /// <summary>
        /// Registers identity with signaller on success.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void WebsocketSignaller_ConnectionSucceeded(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Connection Succeeded.");
            await this.SendMessage(this.identity);
        }


        private void WebSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                // Use a datareader to read the message 
                // out of the websocket args.
                using (DataReader dataReader = args.GetDataReader())
                {
                    dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                    var rawMessage = dataReader.ReadString(dataReader.UnconsumedBufferLength);
                    this.ReceivedMessage?.Invoke(this, rawMessage);
                }
            }
            catch (Exception ex)
            {
                // This should probably rethrow since exceptions
                // are currently silenced.
                Windows.Web.WebErrorStatus webErrorStatus = 
                    WebSocketError.GetStatus(ex.GetBaseException().HResult);
            }
        }

        // moved.
        private void WebSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            this.connected = false;
            this.WebSocket.Dispose();
            this.WebSocket = null;
            this.ConnectionEnded?.Invoke(this, EventArgs.Empty);
        }
    }
}
