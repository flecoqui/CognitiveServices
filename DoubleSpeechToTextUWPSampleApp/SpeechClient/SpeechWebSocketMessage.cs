//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//********************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using System.IO;
using Windows.Data.Json;

namespace SpeechClient
{
    public sealed class SpeechWebSocketMessage
    {
        public string Path { get; set; }
        public string RequestId { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
        public byte[] PayLoad { get; set; }
        public SpeechWebSocketMessage()
        {
            Path = string.Empty;
            RequestId = string.Empty;
            ContentType = string.Empty;
            Content = string.Empty;
            PayLoad = null;
        }
        public SpeechWebSocketMessage(
            string path,
            string requestId,
            string contentType,
            string content,
            [ReadOnlyArray()]  byte[] payLoad)
        {
            Path = path;
            RequestId = requestId;
            ContentType = contentType;
            Content = content;
            PayLoad = payLoad;
        }
        public byte[] ToBytes()
        {
            var outputBuilder = new System.Text.StringBuilder();
            outputBuilder.Append("Path: " + this.Path +  "\r\n");
            outputBuilder.Append("X-RequestId: " + this.RequestId + "\r\n");
            outputBuilder.Append("X-Timestamp: " + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK") + "\r\n");
            if (!string.IsNullOrEmpty(this.ContentType))
                outputBuilder.Append("Content-Type: " + this.ContentType + "\r\n");
            if (!string.IsNullOrEmpty(this.Content))
                outputBuilder.Append( this.Content );

            byte[] header = System.Text.Encoding.ASCII.GetBytes(outputBuilder.ToString());

            byte[] headerHeadBytes = BitConverter.GetBytes((UInt16)header.Length);
            bool isBigEndian = !BitConverter.IsLittleEndian;
            byte[] headerLength = !isBigEndian ? new byte[] { headerHeadBytes[1], headerHeadBytes[0] } : new byte[] { headerHeadBytes[0], headerHeadBytes[1] };
            if(this.PayLoad != null)
                return headerLength.Concat(header).Concat(PayLoad).ToArray();
            else
                return headerLength.Concat(header).ToArray();
        }
    }
}
