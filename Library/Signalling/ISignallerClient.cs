using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.Networking.Sockets;

namespace ApolloLensLibrary.Signalling 
{
    /// <summary>
    /// Defines the "signaller" interface.
    /// Functionality includes (to/from signaller):
    /// (1) Connect / Register
    /// (2) Send
    /// (3) Receive
    /// (4) Disconnect
    /// </summary>
    public interface ISignallerClient 
    {
        #region variables

        /// <summary>
        /// Used for registration with Signaller.
        /// Refers to future WebRTC connection: can be a "client" or "source."
        /// </summary>
        string RegistrationId { get; }
        
        /// <summary>
        /// Signaller address.
        /// </summary>
        /// <value>Needs to be in the form "ws://..." or "wss://..."</value>
        string Address { get; }
        bool IsConnected { get; }
        MessageWebSocket WebSocket { get; }

        /// <summary>
        /// Used to store message type signifiers from Library/Utilities/config.json.
        /// Must be referenced by any Signaller users to behave appropriately with Signaller.
        /// </summary>
        Dictionary<string, string> MessageType { get; }
        SignallerMessageProtocol MessageProtocol { get; }

        #endregion

        #region init

        /// <summary>
        /// Read all from Library/Utilities/config.json
        /// under "Signaller" and populate appropriately.
        /// </summary>
        void Configure();

        #endregion

        #region connection

        /// <summary>
        /// Connects to Signaller (Server).
        /// </summary>
        /// <param name="address">Connection Parameters</param>
        /// <returns>Async</returns>
        Task ConnectToSignaller();

        /// <summary>
        /// Disconnects from Signaller (Server).
        /// </summary>
        void DisconnectFromSignaller();

        /// <summary>
        /// Handler for connection success.
        /// Registers with Signaller (Server).
        /// </summary>
        void ConnectionSucceeded();

        /// <summary>
        /// Handler for connection end.
        /// Associated with Websocket.
        /// </summary>
        void ConnectionEnded(IWebSocket sender, WebSocketClosedEventArgs args);

        /// <summary>
        /// UI Handler for connection end.
        /// </summary>
        event EventHandler ConnectionEndedUIHandler;

                /// <summary>
        /// UI Handler for connection failure.
        /// </summary>
        event EventHandler ConnectionFailedUIHandler;


        #endregion

        #region messages

        /// <summary>
        /// Sends message to Signaller.
        /// </summary>
        /// <param name="message">Plaintext message</param>
        /// <returns>Async</returns>
        Task SendMessage(string message);

        /// <summary>
        /// Handler for received message.
        /// Associated with Websocket.
        /// </summary>
        void ReceivedMessage(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args);

        /// <summary>
        /// External Handler for received message from Signaller.
        /// </summary>
        event EventHandler<SignallerMessage> ReceivedMessageExternalHandler;

        #endregion
    }
}