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
        static bool Parse(String Input)
        {
            bool bResult = false;

            if (File.Exists(Input))
            {
                Console.WriteLine("Start parsing file: " + Input);
                using (var inputReader = new WaveFileReader(Input))
                {
                    Console.WriteLine("Sample Rate:              " + inputReader.WaveFormat.SampleRate.ToString());
                    Console.WriteLine("Channels:                 " + inputReader.WaveFormat.Channels.ToString());
                    Console.WriteLine("BitsPerSample:            " + inputReader.WaveFormat.BitsPerSample.ToString());
                    Console.WriteLine("Average Bytes Per Second: " + inputReader.WaveFormat.AverageBytesPerSecond.ToString());
                    Console.WriteLine("Block Align:              " + inputReader.WaveFormat.BlockAlign.ToString());
                    Console.WriteLine("Encoding:                 " + inputReader.WaveFormat.Encoding.ToString());
                    Console.WriteLine("Length:                   " + inputReader.Length.ToString());
                }
                Console.WriteLine("Parsing done");
                bResult = true;
            }
            else
            {
                Console.WriteLine("File " + Input + " doesn't exist");
                Console.WriteLine(string.Format(InformationCSAudioTool, VersionString));
            }

            return bResult;
        }
    }
}