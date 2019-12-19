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
        static bool Convert(String Input, String Output, String Format, long Start, long Duration)
        {
            bool bResult = false;

            if (File.Exists(Input))
            {
                var outputFilePath = GetNewFilePath(Output);
                int sampleRate = 16000;
                int channels = 1;
                GetFormat(Format, out sampleRate, out channels);
                Console.WriteLine("Start converting file: " + Input);
                WaveFormat format = new WaveFormat(sampleRate, channels);
                WdlResamplingSampleProvider resampler;
                using (var inputReader = new AudioFileReader(Input))
                {
                    if (inputReader.WaveFormat.Channels == 2)
                    {
                        var mono = new StereoToMonoSampleProvider(inputReader);
                        resampler = new WdlResamplingSampleProvider(mono, sampleRate);
                    }
                    else
                        resampler = new WdlResamplingSampleProvider(inputReader, sampleRate);
                    WaveFileWriter.CreateWaveFile16(outputFilePath, resampler);

                    /*
                     * 
                     * StereoToMonoSampleProvider
                     * WdlResamplingSampleProvider
                     * SampleToWaveProvider16
                     * 
                     * */
                }
                Console.WriteLine("Converting done");
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