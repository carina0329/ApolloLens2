using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;

namespace ApolloLensLibrary.Signaller 
{
    /// <summary>
    /// Defines the "signaller" interface.
    /// Functionality includes (to/from signaller):
    /// (1) Connect & Register
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
        public string registrationId { get; set; }

        public bool isConnected { get; set; } = false;
        private MessageWebSocket WebSocket { get; set; }

        #endregion

        #region connection

        /// <summary>
        /// Connects to Signaller (Server).
        /// </summary>
        /// <param name="address">Connection Parameters</param>
        /// <returns>Async</returns>
        Task ConnectToSignaller(string address);

        /// <summary>
        /// Disconnects from Signaller (Server).
        /// </summary>
        public void DisconnectFromSignaller();

        /// <summary>
        /// Handler for connection success. 
        /// </summary>
        public event EventHandler ConnectionSucceeded;

        /// <summary>
        /// Handler for connection failure.
        /// </summary>
        public event EventHandler ConnectionFailed;

        /// <summary>
        /// Handler for connection end.
        /// </summary>
        public event EventHandler ConnectionEnded;


        #endregion

        #region messages

        /// <summary>
        /// Handler for message from Signaller.
        /// </summary>
        event EventHandler<string> ReceivedMessage;

        /// <summary>
        /// Sends plaintext message to Signaller.
        /// </summary>
        /// <param name="message">Plaintext message</param>
        /// <returns>Async</returns>
        Task SendMessage(string message);

        #endregion
    }
}