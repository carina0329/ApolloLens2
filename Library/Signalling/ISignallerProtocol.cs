using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApolloLensLibrary.Signalling
{
    /// <summary>
    /// Wraps and unwraps messages in JSON format, using a
    /// string as a message type signifier.
    /// Allows messages to be distinguished by a string.
    /// </summary>
    public abstract class ISignallerMessageProtocol
    {
        string MessageKey { get; set; }
        string MessageValue { get; set; }

        /// <summary>
        /// Bundles the specified message and type together
        /// into a raw string, containing JSON.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="contents"></param>
        /// <returns></returns>
        public abstract string WrapMessage(string type, string contents);

        /// <summary>
        /// Parses a raw JSON string into a Message<T>, where
        /// the Message<t> contains the type (an Enum instance)
        /// and the message string itself.
        /// </summary>
        /// <param name="wrapped"></param>
        /// <returns></returns>
        public abstract ISignallerMessage UnwrapMessage(string wrapped);
    }

    public class ISignallerMessage
    {
        string Type { get; }
        string Contents { get; }
    }
}
