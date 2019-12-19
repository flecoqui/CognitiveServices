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
using System.Collections.Generic;
using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;
using NAudio.Wave;
using System.Threading;

namespace CSAudioTool
{
    public class WAVSpeechStreamFromWaveProvider : PullAudioInputStreamCallback
    {

        private readonly IWaveProvider innerProvider;
        private readonly WaveFormat waveFormat;
        private bool bFirstRead;
        public WAVSpeechStreamFromWaveProvider(IWaveProvider Provider)
        {
            innerProvider = Provider;
            waveFormat = Provider.WaveFormat;
            bFirstRead = true;

        }
        //var fileContents = new byte[]
        //{
        //        0x52, 0x49, 0x46, 0x46, // "RIFF"
        //        0x26, 0x00, 0x00, 0x00, // ChunkSize = 38
        //        0x57, 0x41, 0x56, 0x45, // "WAVE"
        //        0x66, 0x6d, 0x74, 0x20, // "fmt "
        //        0x12, 0x00, 0x00, 0x00, // Subchunk1Size = 18
        //        0x07, 0x00, 0x02, 0x00, // AudioFormat = 7, NumChannels = 2
        //        0x40, 0x1f, 0x00, 0x00, // SampleRate = 8000
        //        0x80, 0x3e, 0x00, 0x00, // ByteRate = 16000
        //        0x02, 0x00, 0x08, 0x00, // BlockAlign = 2, BitsPerSample = 8
        //        0x00, 0x00,             // ExtraParamSize = 0
        //        0x64, 0x61, 0x74, 0x61, // "data"
        //        0x00, 0x00, 0x00, 0x00, // Subchunk2Size = 0
        //};
        public byte[] CreateWAVHeaderBuffer(uint Len)
        {
            uint headerLen = 46;
            byte[] updatedBuffer = new byte[headerLen];
            if (updatedBuffer != null)
            {
                System.Text.UTF8Encoding.UTF8.GetBytes("RIFF").CopyTo(updatedBuffer, 0);
                BitConverter.GetBytes(4 + 18 + 8 + Len + 8).CopyTo(updatedBuffer, 4);
                System.Text.UTF8Encoding.UTF8.GetBytes("WAVE").CopyTo(updatedBuffer, 8);
                System.Text.UTF8Encoding.UTF8.GetBytes("fmt ").CopyTo(updatedBuffer, 12);
                BitConverter.GetBytes(18).CopyTo(updatedBuffer, 16);
                BitConverter.GetBytes((short)waveFormat.Encoding).CopyTo(updatedBuffer, 20);
                BitConverter.GetBytes((short)waveFormat.Channels).CopyTo(updatedBuffer, 22);
                BitConverter.GetBytes(waveFormat.SampleRate).CopyTo(updatedBuffer, 24);
                BitConverter.GetBytes(waveFormat.AverageBytesPerSecond).CopyTo(updatedBuffer, 28);
                BitConverter.GetBytes((short)waveFormat.BlockAlign).CopyTo(updatedBuffer, 32);
                BitConverter.GetBytes((short)waveFormat.BitsPerSample).CopyTo(updatedBuffer, 34);
                BitConverter.GetBytes((short)0).CopyTo(updatedBuffer, 36);
                System.Text.UTF8Encoding.UTF8.GetBytes("data").CopyTo(updatedBuffer, 38);
                BitConverter.GetBytes(Len).CopyTo(updatedBuffer, 42);
            }
            return updatedBuffer;
        }
        /// <summary>
        /// Wave format of this wave provider
        /// </summary>
        public WaveFormat WaveFormat => waveFormat;
        public bool CanRead { get { return true; } }

        public bool CanSeek { get { return false; } }

        public bool CanWrite { get { return true; } }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        public  long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            // Write the WAV header in the buffer
            if (bFirstRead == true)
            {

                byte[] wavbuffer = CreateWAVHeaderBuffer(0);
                if(wavbuffer != null)
                {
                    for(int i = 0; i < wavbuffer.Length;i++)
                    {
                        buffer[offset + i] = wavbuffer[i];
                    }
                    bFirstRead = false;
                    return wavbuffer.Length;
                }
            }
            if (innerProvider != null)
            {
                return innerProvider.Read(buffer, offset, count);
            }
            return 0;
        }
        public int Read(float[] buffer, int offset, int count)
        {
            return innerProvider.ToSampleProvider().Read(buffer, offset, count);
        }
        public long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] buffer, int offset, int count)
        {

            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, uint size)
        {
            // Write the WAV header in the buffer
            if (bFirstRead == true)
            {
                int tempo = (int)(3200 * 1000) / (waveFormat.AverageBytesPerSecond);
                System.Threading.Tasks.Task.Delay(tempo).Wait();
                byte[] wavbuffer = CreateWAVHeaderBuffer(0);
                if (wavbuffer != null)
                {
                    for (int i = 0; i < wavbuffer.Length; i++)
                    {
                        buffer[i] = wavbuffer[i];
                    }
                    bFirstRead = false;
                //    Console.WriteLine("Read: " + wavbuffer.Length.ToString() + " bytes");
                    return wavbuffer.Length;
                }
            }
            if (innerProvider != null)
            {
                //int tempo = (int)(3200 * 1000) / (waveFormat.AverageBytesPerSecond);
                //System.Threading.Tasks.Task.Delay(tempo).Wait();
                int bytesRead = 0;
                while (bytesRead < size)
                {
                    int read = innerProvider.Read(buffer, bytesRead, (int)(size-bytesRead));
                    if (read < (size - bytesRead))
                    {
                        Thread.Sleep(1);
                    }
                    bytesRead += read;
                }
                //Console.WriteLine("Read: " + count.ToString() + " bytes");
                return bytesRead;
            }
            return 0;
        }

        public override void Close()
        {
            // close and cleanup resources.
        }
    }
}
