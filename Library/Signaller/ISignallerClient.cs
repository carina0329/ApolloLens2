using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.Networking.Sockets;

namespace ApolloLensLibrary.Signaller
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
        #region Connection

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

        #region Messages

        /// <summary>
        /// Sends message to Signaller.
        /// </summary>
        /// <param name="message">JSON encoded message</param>
        /// <returns>Async</returns>
        Task SendMessage(string key, string message);

        #endregion

        #region Rooms

        /// <summary>
        /// Determines if the client is in a room.
        /// </summary>
        bool IsInRoom();

        /// <summary>
        /// Sends a room creation request to Signaller (Server).
        /// Source only.
        /// </summary>
        void RequestCreateRoom(string name);

        /// <summary>
        /// Sends a room poll request to Signaller (Server).
        /// Client only.
        /// </summary>
        void RequestPollRooms();

        /// <summary>
        /// Sends a room join request to Signaller (Server).
        /// Client/Source both.
        /// </summary>
        void RequestJoinRoom(string name);

        #endregion
    }
}