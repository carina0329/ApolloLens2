namespace ApolloLensLibrary.Signalling
{
    public class SignallerClient : ISignallerClient
    {
        #region variables

        string RegistrationId { get; }
        string Address { get; }
        bool IsConnected { get; }
        MessageWebSocket WebSocket { get; }

        Dictionary<string, string> MessageType { get; }
        SignallerMessageProtocol MessageProtocol { get; }

        #endregion

        #region init

        SignallerClient(string id)
        {
            this.RegistrationId = id;
            this.IsConnected = false;
            this.WebSocket = null;
            this.Configure();
        }

        void Configure()
        {
            using (StreamReader r = new StreamReader("../Utilities/config.json"))
            {
                string json = r.ReadToEnd();
                var jobj = JObject.Parse(json);
                this.Address = (string)jobj.SelectToken("Address");
                this.MessageProtocol = new SignallerMessageProtocol
                (
                    (string)jobj.SelectToken("MessageKey");
                    (string)jobj.SelectToken("MessageValue");
                );
                this.MessageType = new Dictionary<string, string>();
                JArray messageTypes = (JArray)jobj.SelectToken("MessageTypes");
                foreach (JToken type in messageTypes)
                {
                    this.MessageType.add((string)type, (string)type);
                }
            }
        }

        #endregion

        #region connection

        public async Task ConnectToSignaller()
        {
            try
            {
                this.WebSocket = new MessageWebSocket();
                this.WebSocket.Control.MessageType = SocketMessageType.Utf8;
                this.WebSocket.MessageReceived += this.ReceivedMessage;
                this.WebSocket.Closed += this.ConnectionEnded;
                await this.WebSocket.ConnectAsync(new Uri(this.Address));
                await this.ConnectionSucceeded();
            }
            catch
            {
                this.IsConnected = false;
                this.ConnectionFailedUIHandler?.Invoke(this, EventArgs.Empty);
            }
        }

        public void DisconnectFromSignaller()
        {
            this.IsConnected = false;
            this.WebSocket.Close(1000, "");
        }

        private async void ConnectionSucceeded()
        {
            this.IsConnected = true;
            System.Diagnostics.Debug.WriteLine("Connected to Signaller.");

            await this.SendMessage
            (
                this.MessageProtocol.WrapMessage
                (
                    this.MessageType["Register"],
                    this.RegistrationId
                )
            );
        }

        private void ConnectionEnded(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            this.IsConnected = false;
            this.WebSocket.Dispose();
            this.WebSocket = null;
            this.ConnectionEndedUIHandler?.Invoke(this, EventArgs.Empty);
        }

        event EventHandler ConnectionEndedUIHandler;
        event EventHandler ConnectionFailedUIHandler;

        #endregion

        #region messages

        public Task SendMessage(string message)
        {
            if (this.WebSocket == null)
                throw new ArgumentException("Websocket doesn't exist.");

            // Use a datawriter to write the specified 
            // message to the websocket.
            using (var dataWriter = new DataWriter(this.WebSocket.OutputStream))
            {
                dataWriter.WriteString(message);
                await dataWriter.StoreAsync();
                dataWriter.DetachStream();
            }
        }

        private void ReceivedMessage(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                // Use a datareader to read the message 
                // out of the websocket args.
                using (DataReader dataReader = args.GetDataReader())
                {
                    dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                    string wrapped = dataReader.ReadString(dataReader.UnconsumedBufferLength);
                    SignallerMessage unwrapped = this.SignallerMessageProtocol.UnwrapMessage(wrapped);
                    this.ReceivedMessageExternalHandler?.Invoke(this, unwrapped);
                }
            }
            catch (Exception ex)
            {
                Windows.Web.WebErrorStatus webErrorStatus = WebSocketError.GetStatus
                (
                    ex.GetBaseException().HResult
                );
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        event EventHandler<SignallerMessage> ReceivedMessageExternalHandler;

        #endregion
    }
}