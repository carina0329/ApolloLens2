using Newtonsoft.Json;
using System;
using Windows.Data.Json;

namespace ApolloLensLibrary.Signalling
{
    public class SignallerMessageProtocol : ISignallerMessageProtocol
    {
        public string MessageKey { get; set; }
        public string MessageValue { get; set; }

        SignallerMessageProtocol(string key, string value)
        {
            this.MessageKey = key;
            this.MessageValue = value;
        }

        public string WrapMessage(string type, string value) 
        {
            JsonObject wrapped = new JsonObject() 
            {
                {
                    this.MessageKey,
                    type
                },
                {
                    this.MessageValue,
                    contents
                }
            };

            return wrapped.Stringify();
        }

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