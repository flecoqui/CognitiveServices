using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechClient
{
    class WebSocketMessage
    {
        /* #region Public Properties */
        public string Body { get; private set; }
        public System.Collections.Generic.Dictionary<string, string> Headers { get; private set; }
        public string Path
        {
            get
            {
                return this.GetHeader("Path");
            }
        }
        public string RequestId
        {
            get
            {
                return this.GetHeader("X-RequestId");
            }
        }
        public string GetHeader(string key)
        {
            if (Headers.ContainsKey(key))
                return Headers[key];
            return string.Empty;
        }
        static public WebSocketMessage CreateSocketMessage(string message)
        {
            WebSocketMessage webSocketMessage = new WebSocketMessage();
            if (webSocketMessage != null)
            {
                webSocketMessage.Headers = new System.Collections.Generic.Dictionary<string, string>();
                StringReader str = new StringReader(message);
                bool wasPreviousLineEmpty = true;
                string line = null;
                do
                {
                    line = str.ReadLine();
                    if (line == string.Empty)
                    {
                        if (wasPreviousLineEmpty) break;
                    }
                    else
                    {
                        var colonIndex = line.IndexOf(':');
                        if (colonIndex > -1)
                        {
                            webSocketMessage.Headers.Add(line.Substring(0, colonIndex), line.Substring(colonIndex + 1));
                        }
                    }

                } while (line != null);
                webSocketMessage.Body = str.ReadToEnd();
            }
            return webSocketMessage;
        }
    }
}
