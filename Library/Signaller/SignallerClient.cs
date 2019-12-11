using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Collections.Generic;
using Windows.Networking.Sockets;

namespace ApolloLensLibrary.Signaller

{
    public class SignallerClient : ISignallerClient
    {
        #region Variables

        /// <summary>
        /// Used for registration with Signaller.
        /// Refers to future WebRTC connection: can be a "client" or "source."
        /// </summary>
        public string RegistrationId { get; }

        /// <summary>
        /// Used for room registration with Signaller.
        /// </summary>
        public string RoomId { get; set; }

        /// <summary>
        /// Signaller address.
        /// </summary>
        /// <value>Needs to be in the form "ws://..." or "wss://..."</value>
        private string Address { get; set; }

        /// <summary>
        /// Signaller port.
        /// </summary>
        private string Port { get; set; }

        private MessageWebSocket WebSocket { get; set; }
        public bool IsConnected { get; set; }

        /// <summary>
        /// Used to store message type signifiers from Library/Utilities/config.json.
        /// Assigns appropriate handlers.
        /// </summary>
        public Dictionary<string, EventHandler<SignallerMessage>> MessageHandlers { get; set; }

        private SignallerMessageProtocol MessageProtocol { get; set; }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor. Initializes variables.
        /// </summary>
        /// <param name="id">Registration Id</param>
        public SignallerClient(string id)
        {
            this.RegistrationId = id;
            this.RoomId = "";
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
                this.Address = "ws://" + (string)jobj.SelectToken("Signaller.Address");
                this.Port = (string)jobj.SelectToken("Signaller.Port");
                this.MessageProtocol = new SignallerMessageProtocol
                (
                    (string)jobj.SelectToken("Signaller.MessageKey"),
                    (string)jobj.SelectToken("Signaller.MessageValue")
                );
                this.MessageHandlers = new Dictionary<string, EventHandler<SignallerMessage>>();
                JArray messageTypes = (JArray)jobj.SelectToken("Signaller.MessageTypes");
                foreach (JToken type in messageTypes)
                {
                    EventHandler<SignallerMessage> empty = null;
                    this.MessageHandlers.Add((string)type, empty);
                }

                this.MessageHandlers["RoomCreate"] = ReceivedRoomCreateMessage;
                this.MessageHandlers["RoomJoin"] = ReceivedRoomJoinMessage;
            }
        }

        #endregion

        #region Connection

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
                Uri addr = new Uri(this.Address);
                UriBuilder addrBuilder = new UriBuilder(addr);
                addrBuilder.Port = Int32.Parse(this.Port);
                await this.WebSocket.ConnectAsync(addrBuilder.Uri);
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

            // sanity check: is "Register" still in config.json? Otherwise, change this.
            if (!this.MessageHandlers.ContainsKey("Register"))
                throw new ArgumentException("Check config.json for changes to Signaller Registration Message Type");
            await this.SendMessage("Register", this.RegistrationId);
            this.ConnectionSuccessUIHandler?.Invoke(this, EventArgs.Empty);
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
        /// UI Handler for connection success.
        /// </summary>
        public event EventHandler ConnectionSuccessUIHandler;

        /// <summary>
        /// UI Handler for connection end.
        /// </summary>
        public event EventHandler ConnectionEndedUIHandler;

        /// <summary>
        /// UI Handler for connection failure.
        /// </summary>
        public event EventHandler ConnectionFailedUIHandler;

        #endregion

        #region Messages

        /// <summary>
        /// Sends message to Signaller.
        /// </summary>
        /// <param name="message">JSON encoded message</param>
        /// <returns>Async</returns>
        public async Task SendMessage(string key, string message)
        {
            if (!this.IsConnected || this.WebSocket == null)
                return;
            if (!this.MessageHandlers.ContainsKey(key))
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
                    this.MessageHandlers[(string)unwrapped.Type]?.Invoke(this, unwrapped);
                }
            }
            catch (Exception ex)
            {
                Windows.Web.WebErrorStatus webErrorStatus = WebSocketError.GetStatus
                (
                    ex.GetBaseException().HResult
                );
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                this.IsConnected = false;
                this.ConnectionFailedUIHandler?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Rooms

        /// <summary>
        /// Determines if the client is currently in a room.
        /// </summary>
        public bool IsInRoom()
        {
            return this.RoomId != "";
        }

        /// <summary>
        /// Sends a room creation request to Signaller (Server).
        /// Source only.
        /// </summary>
        /// <param name="name"></param>
        public async void RequestCreateRoom(string name)
        {
            await this.SendMessage("RoomCreate", name);
        }

        /// <summary>
        /// Sends a room poll request to Signaller (Server).
        /// Client only.
        /// </summary>
        public async void RequestPollRooms()
        {
            await this.SendMessage("RoomPoll", "");
        }

        /// <summary>
        /// Sends a room join request to Signaller (Server).
        /// Client/Source both.
        /// </summary>
        /// <param name="name"></param>
        public async void RequestJoinRoom(string name)
        {
            await this.SendMessage("RoomJoin", name);
        }

        /// <summary>
        /// Room Create Message Handler.
        /// </summary>
        private void ReceivedRoomCreateMessage(object sender, SignallerMessage message)
        {
            if (message.Contents == "")
            {
                this.RoomCreateFailedUIHandler?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                this.RequestJoinRoom(message.Contents);
            }
        }

        /// <summary>
        /// Room Join Message Handler.
        /// </summary>
        private void ReceivedRoomJoinMessage(object sender, SignallerMessage message)
        {
            if (message.Contents == "")
            {
                this.RoomJoinFailedUIHandler?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                this.RoomId = message.Contents;
                this.RoomJoinSuccessUIHandler?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// UI Handler for room creation failure.
        /// </summary>
        public event EventHandler RoomCreateFailedUIHandler;

        /// <summary>
        /// UI Handler for room join failure.
        /// </summary>
        public event EventHandler RoomJoinSuccessUIHandler;

        /// <summary>
        /// UI Handler for room join failure.
        /// </summary>
        public event EventHandler RoomJoinFailedUIHandler;

        #endregion
    }
}