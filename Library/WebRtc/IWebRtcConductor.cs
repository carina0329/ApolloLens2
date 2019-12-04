using System;
using Org.WebRtc;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Defines an interface capable of accessing local
/// media and conducting a configurable two way
/// video / audio call between devices.
/// Implemented using WebRtc nuget package in other
/// assemblies.
/// </summary>
namespace ApolloLensLibrary.WebRtc
{
    /// <summary>
    /// WebRtcConductor Interface.
    /// (1) Initializes Media (handled through implementation)
    /// (2) Initializes Peer WebRTC Connection
    /// (3) Conducts call
    /// </summary>
    public interface IWebRtcConductor
    {
        #region WebRtcInitializer

        /// <summary>
        /// WebRTC Establishment: Sends Offer
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        Task SendOffer(RTCSessionDescription offer);

        /// <summary>
        /// WebRTC Establishment: Sends Answer
        /// </summary>
        /// <param name="answer"></param>
        /// <returns></returns>
        Task SendAnswer(RTCSessionDescription answer);

        /// <summary>
        /// WebRTC Establishment: Sends Ice Candidate
        /// </summary>
        /// <param name="iceCandidate"></param>
        /// <returns></returns>
        Task SendIceCandidate(RTCIceCandidate iceCandidate);

        /// <summary>
        /// WebRTC Establishment: Sends Shutdown
        /// </summary>
        /// <returns></returns>
        Task SendShutdown();

        #endregion

        #region CallConductor

        /// <summary>
        /// Start a "call" based on the current
        /// media options
        /// </summary>
        /// <returns></returns>
        Task StartCall();

        /// <summary>
        /// Shut down a running call, release most 
        /// resources.
        /// </summary>
        /// <returns></returns>
        Task Shutdown();

        #endregion
    }
}
