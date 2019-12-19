//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;

namespace CSAudioTool
{

    class WAVStream : Stream, ISampleProvider
    {
        private readonly MemoryStream innerStream;
        private readonly WaveFormat waveFormat;
        private long readPosition;
        private long writePosition;

        public WAVStream()
        {
            innerStream = new MemoryStream();
        }
        public WAVStream(WaveFormat WaveFormat)
        {
            waveFormat = WaveFormat;
            innerStream = new MemoryStream();
        }

        /// <summary>
        /// Wave format of this wave provider
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;
        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return true; } }

        public override void Flush()
        {
            lock (innerStream)
            {
                innerStream.Flush();
            }
        }

        public override long Length
        {
            get
            {
                lock (innerStream)
                {
                    return innerStream.Length;
                }
            }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (innerStream)
            {
                innerStream.Position = readPosition;
                int red = innerStream.Read(buffer, offset, count);
                readPosition = innerStream.Position;

                return red;
            }
        }
        public int Read(float[] buffer, int offset, int count)
        {
            lock (innerStream)
            {
                int bytesNeeded = count * 4;
                byte[] localbuffer = new byte[bytesNeeded];
                int bytesRead = this.Read(localbuffer, 0, bytesNeeded);
                int samplesRead = bytesRead / 4;
                int outputIndex = offset;
                for (int n = 0; n < bytesRead; n += 4)
                {
                    buffer[outputIndex++] = BitConverter.ToSingle(localbuffer, n);
                }
                return samplesRead;
            }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (innerStream)
            {
                innerStream.Position = writePosition;
                innerStream.Write(buffer, offset, count);
                writePosition = innerStream.Position;
            }
        }
    }
}
