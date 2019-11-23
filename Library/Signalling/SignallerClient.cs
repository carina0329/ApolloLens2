using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Collections.Generic;
using Windows.Networking.Sockets;

namespace ApolloLensLibrary.Signalling

{
    public class SignallerClient : ISignallerClient
    {
        #region variables

        /// <summary>
        /// Used for registration with Signaller.
        /// Refers to future WebRTC connection: can be a "client" or "source."
        /// </summary>
        private string RegistrationId { get; }

        /// <summary>
        /// Signaller address.
        /// </summary>
        /// <value>Needs to be in the form "ws://..." or "wss://..."</value>
        private string Address { get; set; }

        private MessageWebSocket WebSocket { get; set; }
        public bool IsConnected { get; set; }

        /// <summary>
        /// Used to store message type signifiers from Library/Utilities/config.json.
        /// Must be referenced by any Signaller users to behave appropriately with Signaller.
        /// </summary>
        public Dictionary<string, string> MessageType { get; set; }
        private SignallerMessageProtocol MessageProtocol { get; set; }

        #endregion

        #region init

        /// <summary>
        /// Constructor. Initializes variables.
        /// </summary>
        /// <param name="id">Registration Id</param>
        public SignallerClient(string id)
        {
            this.RegistrationId = id;
            this.IsConnected = false;
            this.WebSocket = null;
            this.Configure();
        }

        /// <summary>
        /// Read all from Library/Utilities/config.json
        /// under "Signaller" and populate appropriately.
        /// </summary>
        private void Configure()
        {
            using (StreamReader r = File.OpenText("Library\\Utilities\\config.json"))
            {
                string json = r.ReadToEnd();
                var jobj = JObject.Parse(json);
                this.Address = (string)jobj.SelectToken("Signaller.Address");
                this.MessageProtocol = new SignallerMessageProtocol
                (
                    (string)jobj.SelectToken("Signaller.MessageKey"),
                    (string)jobj.SelectToken("Signaller.MessageValue")
                );
                this.MessageType = new Dictionary<string, string>();
                JArray messageTypes = (JArray)jobj.SelectToken("Signaller.MessageTypes");
                foreach (JToken type in messageTypes)
                {
                    System.Diagnostics.Debug.WriteLine((string)type);
                    this.MessageType.Add((string)type, (string)type);
                }
            }
        }

        #endregion

        #region connection

        /// <summary>
        /// Connects to Signaller (Server).
        /// </summary>
        /// <param name="address">Connection Parameters</param>
        /// <returns>Async</returns>
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

        /// <summary>
        /// Disconnects from Signaller (Server).
        /// </summary>
        public void DisconnectFromSignaller()
        {
            this.IsConnected = false;
            this.WebSocket.Close(1000, "");
        }

        /// <summary>
        /// Handler for connection success.
        /// Registers with Signaller (Server).
        /// </summary>
        private async Task ConnectionSucceeded()
        {
            this.IsConnected = true;
            System.Diagnostics.Debug.WriteLine("Connected to Signaller.");

            await this.SendMessage(this.MessageType["Register"], this.RegistrationId);
        }

        /// <summary>
        /// Handler for connection end.
        /// Associated with Websocket.
        /// </summary>
        private void ConnectionEnded(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            this.IsConnected = false;
            this.WebSocket.Dispose();
            this.WebSocket = null;
            this.ConnectionEndedUIHandler?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// UI Handler for connection end.
        /// </summary>
        public event EventHandler ConnectionEndedUIHandler;

        /// <summary>
        /// UI Handler for connection failure.
        /// </summary>
        public event EventHandler ConnectionFailedUIHandler;

        #endregion

        #region messages

        /// <summary>
        /// Sends message to Signaller.
        /// </summary>
        /// <param name="message">JSON encoded message</param>
        /// <returns>Async</returns>
        public async Task SendMessage(string key, string message)
        {
            if (this.WebSocket == null)
                throw new ArgumentException("Websocket doesn't exist.");
            if (!this.MessageType.ContainsKey(key))
                throw new ArgumentException($"Invalid key {key} used.");

            string wrapped = this.MessageProtocol.WrapMessage(key, message);

            // Use a datawriter to write the specified 
            // message to the websocket.
            using (var dataWriter = new DataWriter(this.WebSocket.OutputStream))
            {
                dataWriter.WriteString(wrapped);
                await dataWriter.StoreAsync();
                dataWriter.DetachStream();
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Handler for received message.
        /// Associated with Websocket.
        /// </summary>
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
                    SignallerMessage unwrapped = this.MessageProtocol.UnwrapMessage(wrapped);
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

        /// <summary>
        /// External Handler for received message from Signaller.
        /// </summary>
        public event EventHandler<SignallerMessage> ReceivedMessageExternalHandler;

        #endregion
    }
}