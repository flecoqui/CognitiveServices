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
        private static string VersionString = "1.0.0.0";
        private static string InformationCSAudioTool = "CSAudioTool:\r\n" +
            "Cognitive Service Command Line Audio Tool Version: {0} \r\n" + "Syntax:\r\n" +
            "csaudiotool --capture    --input <AudioSource> --output <WAV Audio File path> --format <WAVFormat>\r\n" +
            "                         --duration <DurationMs> --device <CaptureDeviceID>\r\n" +
            "csaudiotool --convert    --input <WAV Audio File path> --output <WAV Audio File path> --format <WAVFormat>\r\n" + 
            "                         --start <timeMs> --duration <DurationMs>\r\n" +
            "csaudiotool --transcript --input <WAV Audio File path> --region <AzureRegion> --key <AccountKey> \r\n" +
            "                         --language <Language> \r\n" +               
            "csaudiotool --transcript --input <AudioSource> --region <AzureRegion> --key <AccountKey>\r\n" +
            "                         --language <Language> --device <CaptureDeviceID>\r\n" +
            "csaudiotool --listdevices \r\n" +
            "csaudiotool --play --input <WAV Audio File path> --device <RenderDeviceID>\r\n" +
            "csaudiotool --parse --input <WAV Audio File path> \r\n" +
            "csaudiotool --help \r\n" +
            "where:\r\n" +
            "      AudioSource: loopback   Default loopback input by default\r\n" +
            "                   microphone Default Microphone by default\r\n" +
            "      WAVFormat:   16000_1   16Khz 1 Channel 16 bits \r\n" +
            "      Language:    for instance  de-DE, en-US, fr-FR\r\n";
        //
        //
        // --capture --input defaultmicrophone --output c:\temp\outpath.wav --duration 20000
        //--capture --input defaultmicrophone --output "c:\temp\micro16khz.wav" --duration 10000 --format 16000_1
        //--transcript --input "c:\temp\micro16khz.wav" --region northeurope --key 14f47d6474644eacabf62e73e604fa17 --language fr-FR


        private const string ActionCapture = "capture";
        private const string ActionConvert = "convert";
        private const string ActionPlay = "play";
        private const string ActionParse = "parse";
        private const string ActionList = "list";
        private const string ActionTranscript = "transcript";
        private const string InputLoopback = "loopback";
        private const string InputMicrophone = "microphone";
        
        static bool ParseCommandLine(string[] args,
            out string Action,
            out string Input,
            out string Output,
            out string Format,
            out long Start,
            out long Duration,
            out string DeviceID,
            out string Region,
            out string Key,
            out string Lang
            )

        {
            
            bool result = false;
            bool bHelp = false;
            string ErrorMessage = string.Empty;
            Action = string.Empty;
            Input = string.Empty;
            Output = string.Empty;
            Format = string.Empty;
            Region = string.Empty;
            DeviceID = string.Empty;
            Key = string.Empty;
            Lang = "en-US";
            Start = 0;
            Duration = 60000;

            if ((args == null) || (args.Length == 0))
            {
                ErrorMessage = "No parameter in the command line";
            }
            else
            {
                int i = 0;
                while ((i < args.Length) && (string.IsNullOrEmpty(ErrorMessage)))
                {

                    switch (args[i++])
                    {

                        case "--help":
                            bHelp = true;
                            break;
                        case "--capture":
                            Action = ActionCapture;
                            break;
                        case "--convert":
                            Action = ActionConvert;
                            break;
                        case "--play":
                            Action = ActionPlay;
                            break;
                        case "--parse":
                            Action = ActionParse;
                            break;
                        case "--listdevices":
                            Action = ActionList;
                            break;
                        case "--transcript":
                            Action = ActionTranscript;
                            break;
                        case "--input":
                            if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                Input = args[i++];
                            else
                                ErrorMessage = "Input not set";
                            break;

                        case "--output":
                            if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                Output = args[i++];
                            else
                                ErrorMessage = "Output not set";
                            break;

                        case "--format":
                            if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                Format = args[i++];
                            else
                                ErrorMessage = "Format not set";
                            break;
                        case "--duration":
                            if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                long.TryParse(args[i++],out Duration);
                            else
                                ErrorMessage = "Duration not set";
                            break;
                        case "--start":
                            if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                long.TryParse(args[i++], out Start);
                            else
                                ErrorMessage = "Start Time not set";
                            break;
                        case "--region":
                            if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                Region = args[i++];
                            else
                                ErrorMessage = "Region not set";
                            break;
                        case "--device":
                            if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                DeviceID = args[i++];
                            else
                                ErrorMessage = "Device ID  not set";
                            break;
                        case "--key":
                            if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                Key = args[i++];
                            else
                                ErrorMessage = "Key not set";
                            break;
                        case "--language":
                            if ((i < args.Length) && (!string.IsNullOrEmpty(args[i])))
                                Lang = args[i++];
                            else
                                ErrorMessage = "Language not set";
                            break;
                        default:

                            if ((args[i - 1].ToLower() == "dotnet") ||
                                (args[i - 1].ToLower() == "csaudiotool.dll") ||
                                (args[i - 1].ToLower() == "csaudiotool.exe"))
                                break;

                            ErrorMessage = "wrong parameter: " + args[i - 1];
                            break;
                    }

                }
            }
            if (((!string.IsNullOrEmpty(Action)) &&
                (!string.IsNullOrEmpty(Input)) &&
                (!string.IsNullOrEmpty(Output))) ||
                ((!string.IsNullOrEmpty(Action)) &&
                (Action == ActionPlay) &&
                (!string.IsNullOrEmpty(Input))) ||
                ((!string.IsNullOrEmpty(Action)) &&
                (Action == ActionTranscript) &&
                (!string.IsNullOrEmpty(Input)) &&
                (!string.IsNullOrEmpty(Region)) &&
                (!string.IsNullOrEmpty(Key))
                ) ||
                ((!string.IsNullOrEmpty(Action)) &&
                (Action == ActionList)
                ) ||
                ((!string.IsNullOrEmpty(Action)) &&
                (Action == ActionParse) &&
                (!string.IsNullOrEmpty(Input))))
                result = true;
            else
                ErrorMessage = "Missing parameter(s) to launch the features";

            if (!string.IsNullOrEmpty(DeviceID))
            {
                MMDevice d = GetDevice(DeviceID);
                if (d == null)
                {
                    result = false;
                    ErrorMessage = "Unknown Device, Device ID: " + DeviceID;
                }
            }


            if ((bHelp) || (!string.IsNullOrEmpty(ErrorMessage)))
            {
                if (!string.IsNullOrEmpty(ErrorMessage))
                    Console.WriteLine(ErrorMessage);
                Console.WriteLine(string.Format(InformationCSAudioTool, VersionString));
            }

            return result;
        }



          static void Main(string[] args)
        {
            string Action;
            string Input;
            string Output;
            string Format;
            long Start;
            long Duration;
            string DeviceID = string.Empty;
            string Region;
            string Key;
            string Lang;
            if (ParseCommandLine(args, out Action, out Input, out Output, out Format, out Start, out Duration, out DeviceID, out Region, out Key, out Lang))
            {
                if(Action == ActionConvert)
                {
                    Convert(Input, Output, Format, Start, Duration);
                }
                else if (Action == ActionCapture)
                {
                    Capture(Input, DeviceID, Output, Format, Duration);
                }
                else if (Action == ActionPlay)
                {
                    Play(Input);
                }
                else if (Action == ActionParse)
                {
                    Parse(Input);
                }
                else if (Action == ActionList)
                {
                    ListDevices();
                }
                else if (Action == ActionTranscript)
                {
                    Transcript(Input,DeviceID,Region, Key, Lang);
                }
            }
        }
    }
}