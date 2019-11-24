using Newtonsoft.Json;
using System;
using Windows.Data.Json;

namespace ApolloLensLibrary.Signaller
{
    public class SignallerMessageProtocol : ISignallerMessageProtocol
    {
        private string MessageKey { get; }
        private string MessageValue { get; }

        /// <summary>
        /// Constructor. Initializes variables.
        /// </summary>
        /// <param name="key">Json Key Encoding</param>
        /// <param name="value">Json Value Encoding</param>
        public SignallerMessageProtocol(string key, string value)
        {
            this.MessageKey = key;
            this.MessageValue = value;
        }

        /// <summary>
        /// Bundles the specified message and type together
        /// into a raw string, containing JSON.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="contents"></param>
        /// <returns></returns>
        public string WrapMessage(string type, string contents) 
        {
            JsonObject wrapped = new JsonObject()
            {
                { 
                    this.MessageKey,
                    JsonValue.CreateStringValue(type)
                },
                { 
                    this.MessageValue,
                    JsonValue.CreateStringValue(contents ?? "")
                }
            };

            return wrapped.Stringify();
        }

        /// <summary>
        /// Parses a raw JSON string into a Message<T>, where
        /// the Message<t> contains the type (an Enum instance)
        /// and the message string itself.
        /// </summary>
        /// <param name="wrapped"></param>
        /// <returns></returns>
        public SignallerMessage UnwrapMessage(string wrapped) 
        {
            if (!JsonObject.TryParse(wrapped, out JsonObject unwrapped))
                throw new ArgumentException("Failed to parse JSON.");
            if (!unwrapped.TryGetValue(this.MessageKey, out IJsonValue type))
                throw new ArgumentException("Failed to parse Message Type.");
            if (!unwrapped.TryGetValue(this.MessageValue, out IJsonValue contents))
                throw new ArgumentException("Failed to parse Message Contents.");

            return new SignallerMessage(type.GetString(), contents.GetString());
        }
    }

    public class SignallerMessage : ISignallerMessage
    {
        public string Type { get; }
        public string Contents { get; }

        public SignallerMessage(string type, string contents)
        {
            this.Type = type;
            this.Contents = contents;
        }
    }
}