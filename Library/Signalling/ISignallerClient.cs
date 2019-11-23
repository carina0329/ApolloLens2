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
    /// (3) Receive (implemented through a private handler)
    /// (4) Disconnect
    /// </summary>
    public interface ISignallerClient 
    {
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

        #endregion

        #region messages

        /// <summary>
        /// Sends message to Signaller.
        /// </summary>
        /// <param name="message">Plaintext message</param>
        /// <returns>Async</returns>
        Task SendMessage(string message);

        #endregion
    }
}