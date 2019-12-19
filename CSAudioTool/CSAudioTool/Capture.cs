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
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using NAudio.Wave.SampleProviders;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;

namespace CSAudioTool
{

    partial class Program
    {
        // Test WASAPI capture in WAV file using the default WAVE Format
        static string GetNewFilePath(string Output)
        {
            int Index = 0;
            string path = string.Empty;
            string extension = string.Empty;
            int pos = Output.LastIndexOf('.');
            if (pos > 0)
            {
                path = Output.Substring(0, pos);
                extension = Output.Substring(pos + 1);
            }
            else
            {
                path = Output.Substring(0, pos);
                extension = string.Empty;
            }

            while (File.Exists(Output))
            {
                Index++;
                Output = path + "(" + Index.ToString() + ")." + extension;
            }
            return Output;
        }
        public static bool CaptureLoopback(string Input, string DeviceID, string Output, string Format, long Duration)
        {
            bool result = false;
            WasapiLoopbackCapture capture;
            MMDevice d;
            if ((!string.IsNullOrEmpty(DeviceID)) &&
               ((d = GetDevice(DeviceID)) != null))
                capture = new WasapiLoopbackCapture(d);
            else
                capture = new WasapiLoopbackCapture();
            if (capture != null)
            {
                var outputFilePath = GetNewFilePath(Output);
                var writer = new WaveFileWriter(outputFilePath, capture.WaveFormat);

                long MaxPosition = (capture.WaveFormat.AverageBytesPerSecond * Duration) / 1000;
                capture.DataAvailable += (s, a) =>
                {
                    writer.Write(a.Buffer, 0, a.BytesRecorded);
                    if (writer.Position > MaxPosition)
                    {
                        capture.StopRecording();
                    }
                };
                capture.RecordingStopped += (s, a) =>
                {
                    Console.WriteLine("Recording stopped...");
                    writer.Dispose();
                    writer = null;
                    capture.Dispose();
                };
                Console.WriteLine("Start Recording from loopback in file: " + outputFilePath + " during " + Duration.ToString() + " ms");
                capture.StartRecording();
                result = true;
            }
            return result;
        }
        static bool GetFormat(string Format, out int sampleRate, out int channels)
        {
            bool result = false;
            sampleRate = 0;
            channels = 0;
            if (!string.IsNullOrEmpty(Format))
            {
                char[] sep = { '_' };
                string[] array = Format.Split(sep);
                if ((array != null) && (array.Length == 2))
                {
                    int.TryParse(array[0], out sampleRate);
                    int.TryParse(array[0], out channels);
                    result = true;
                }
            }
            return result;
        }
        public static bool CaptureMicrophone(string Input, string DeviceID, string Output, string Format, long Duration)
        {
            bool result = false;
            WasapiCapture capture;
            MMDevice d;
            if ((!string.IsNullOrEmpty(DeviceID)) &&
               ((d = GetDevice(DeviceID)) != null))
                capture = new WasapiCapture(d);
            else
                capture = new WasapiCapture();

            if (capture != null)
            {
                var outputFilePath = GetNewFilePath(Output);

                int sampleRate = 0;
                int channels = 0;
                GetFormat(Format, out sampleRate, out channels);
                WAVStream writer = null;
                WaveFileWriter filewriter = null;
                WaveFormat format = new WaveFormat(sampleRate, channels);
                if (sampleRate > 0)
                    writer = new WAVStream(capture.WaveFormat);
                else
                    filewriter = new WaveFileWriter(outputFilePath, capture.WaveFormat);

                long MaxPosition = (capture.WaveFormat.AverageBytesPerSecond * Duration) / 1000;
                capture.DataAvailable += (s, a) =>
                {
                    if (sampleRate > 0)
                        writer.Write(a.Buffer, 0, a.BytesRecorded);
                    else
                        filewriter.Write(a.Buffer, 0, a.BytesRecorded);
                    if (((sampleRate > 0) && (writer.Length > MaxPosition)) ||
                        ((sampleRate == 0) && (filewriter.Length > MaxPosition)))
                    {
                        if (sampleRate > 0)
                        {
                            var resampler = new WdlResamplingSampleProvider(writer, sampleRate);
                            // convert our stereo ISampleProvider to mono
                            var mono = new StereoToMonoSampleProvider(resampler);
                            mono.LeftVolume = 1.0f; // keep the left channel
                            mono.RightVolume = 1.0f; // keep the right channel
                            WaveFileWriter.CreateWaveFile16(outputFilePath, mono);
                        }
                        capture.StopRecording();
                    }
                };
                capture.RecordingStopped += (s, a) =>
                {
                    Console.WriteLine("Recording stopped...");
                    writer.Dispose();
                    writer = null;
                    capture.Dispose();
                };

                Console.WriteLine("Start Recording from microphone in file: " + outputFilePath + " during " + Duration.ToString() + " ms");
                capture.StartRecording();
                result = true;
            }
            return result;
        }



        static bool Capture(String Input, String DeviceID, String Output, String Format, long Duration)
        {
            bool bResult = false;
            if (Input == InputLoopback)
            {
                CaptureLoopback(Input, DeviceID, Output, Format, Duration);
            }
            else if (Input == InputMicrophone)
            {
                CaptureMicrophone(Input, DeviceID, Output, Format, Duration);
            }
            return bResult;
        }



    }
}