using System;
using System.Threading.Tasks;
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

        bool IsConnected { get; }
        MessageWebSocket WebSocket { get; set; }

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
        void DisconnectFromSignaller();

        /// <summary>
        /// Handler for connection success. 
        /// </summary>
        event EventHandler ConnectionSucceeded;

        /// <summary>
        /// Handler for connection failure.
        /// </summary>
        event EventHandler ConnectionFailed;

        /// <summary>
        /// Handler for connection end.
        /// </summary>
        event EventHandler ConnectionEnded;


        #endregion

        #region messages

        /// <summary>
        /// Handler for message from Signaller.
        /// </summary>
        event EventHandler<MessageWebSocket> ReceivedMessage;

        /// <summary>
        /// Sends message to Signaller.
        /// </summary>
        /// <param name="message">Plaintext message</param>
        /// <returns>Async</returns>
        Task SendMessage(string message);

        #endregion
    }
}