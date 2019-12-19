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
        static bool bFirstHypothesis = true;
        static long lastTime = 0;
        static int lastCurX;
        static int lastCurY;
        static void DisplayTranscript(long StartTime, TimeSpan Duration, String Text, bool bCompleted)
        {
            if (bCompleted)
            {
                Console.CursorLeft = lastCurX;
                Console.CursorTop = lastCurY;
                bFirstHypothesis = true;
            }
            else
            {
                if (bFirstHypothesis == true)
                {
                    bFirstHypothesis = false;
                    lastTime = StartTime;
                    lastCurX = Console.CursorLeft;
                    lastCurY = Console.CursorTop;
                }
                else
                {
                    Console.CursorLeft = lastCurX;
                    Console.CursorTop = lastCurY;
                }
            }

            Console.WriteLine($"Start={TimeSpan.FromTicks(StartTime).TotalMilliseconds.ToString()}ms Duration={Duration.TotalMilliseconds.ToString()}ms                   ");
            if (bCompleted)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Text={Text}");
            Console.ResetColor();
            return;
        }
        static async Task<bool> TranscriptFromFile(String Input, String Region, String Key, string Lang)
        {
            bool bResult = false;

            if (File.Exists(Input))
            {
                var audioConfig = AudioConfig.FromWavFileInput(Input);

                var config = SpeechConfig.FromSubscription(Key, Region);
                var language = Lang;
                config.SpeechRecognitionLanguage = language;
                config.OutputFormat = OutputFormat.Simple;
                var stopRecognition = new TaskCompletionSource<int>();
                // Creates a speech recognizer using microphone as audio input.
                using (var recognizer = new SpeechRecognizer(config, audioConfig))
                {
                    // Starts recognizing.
                    Console.WriteLine("Starting Transcript from File...");
                    // Subscribes to events.
                    recognizer.Recognizing += (s, e) =>
                    {
                        DisplayTranscript(e.Result.OffsetInTicks, e.Result.Duration, e.Result.Text, false);
                    };

                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            if (!string.IsNullOrEmpty(e.Result.Text))
                            {
                                DisplayTranscript(e.Result.OffsetInTicks, e.Result.Duration, e.Result.Text, true);
                            }
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };


                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }

                        stopRecognition.TrySetResult(0);

                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                    //Console.WriteLine("\nTranscript Session started");
                };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\nTranscript from file Stopped");
                        stopRecognition.TrySetResult(0);

                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }
            else
            {
                Console.WriteLine("File " + Input + " doesn't exist");
                Console.WriteLine(string.Format(InformationCSAudioTool, VersionString));
            }

            return bResult;
        }


        static bool TranscriptFromWasapi(String Input, String DeviceID, String Region, String Key, string Lang)
        {

            bool bResult = false;
            bool bTerminate = false;
            // Loopback initialization
            WasapiCapture capture = null;
            MMDevice d;
            if ((!string.IsNullOrEmpty(DeviceID)) &&
               ((d = GetDevice(DeviceID)) != null))
            {
                if (Input == InputLoopback)
                    capture = new WasapiLoopbackCapture(d);
                else if (Input == InputMicrophone)
                    capture = new WasapiCapture(d);
            }
            else
            {
                if (Input == InputLoopback)
                    capture = new WasapiLoopbackCapture();
                else if (Input == InputMicrophone)
                    capture = new WasapiCapture();
            }
            int sampleRate = 16000;
            WAVStream writer = new WAVStream(capture.WaveFormat);
            var mono = new StereoToMonoSampleProvider(writer);
            var resampler = new WdlResamplingSampleProvider(mono, sampleRate);
            var sampleProvider = new SampleToWaveProvider16(resampler);
            WAVSpeechStreamFromWaveProvider reader = new WAVSpeechStreamFromWaveProvider(sampleProvider);

            capture.DataAvailable += (s, a) =>
            {
                //Console.WriteLine("Write: " + a.BytesRecorded + " bytes");
                writer.Write(a.Buffer, 0, a.BytesRecorded);
            };
            capture.RecordingStopped += (s, a) =>
            {
                Console.WriteLine("Recording stopped...");
                writer.Dispose();
                writer = null;
                capture.Dispose();
            };

            // Speech Task initialization
            byte channels = 1;
            byte bitsPerSample = 16;
            uint samplesPerSecond = 16000;
            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(samplesPerSecond, bitsPerSample, channels);
            var audioConfig = AudioConfig.FromStreamInput(reader, audioFormat);
            var config = SpeechConfig.FromSubscription(Key, Region);
            var language = Lang;
            config.SpeechRecognitionLanguage = language;
            config.OutputFormat = OutputFormat.Simple;
            var stopRecognition = new TaskCompletionSource<int>();
            var recognizer = new SpeechRecognizer(config, audioConfig);

            var KeyboardTask = new Task(async () =>
            {
                do
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                        bTerminate = true;
                    }
                    Thread.Sleep(50);
                } while (bTerminate == false);
                Console.WriteLine("Aborting Transcript ...");
                capture.StopRecording();
                if(recognizer!=null)                
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
            }
            );

            var LoopbackTask = new Task(() =>
            {
                capture.StartRecording();
            }
            );
            var SpeechTask = new Task(async () =>
            {
                if (recognizer != null)
                {
                    // Starts recognizing.
                    Console.WriteLine("Starting Transcript from Speaker...");
                    // Subscribes to events.
                    recognizer.Recognizing += (s, e) =>
                    {
                        DisplayTranscript(e.Result.OffsetInTicks, e.Result.Duration, e.Result.Text, false);
                    };

                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            if (!string.IsNullOrEmpty(e.Result.Text))
                            {
                                DisplayTranscript(e.Result.OffsetInTicks, e.Result.Duration, e.Result.Text, true);
                            }
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };


                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                        
                        stopRecognition.TrySetResult(0);
                        bTerminate = true;
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        //  Console.WriteLine("\nTranscript Session started");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\nTranscript from Spearker Stopped");
                        stopRecognition.TrySetResult(0);
                        // Terminate Keyboard Task
                        bTerminate = true;
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }
            );

            KeyboardTask.Start();
            LoopbackTask.Start();
            SpeechTask.Start();

            var tasks = new[] { KeyboardTask };
            Task.WaitAll(tasks);

            return bResult;


        }
        static bool Transcript(String Input, String DeviceID, String Region, String Key, string Lang)
        {
            bool bResult = false;
            if (Input == InputLoopback)
            {
                bResult = TranscriptFromWasapi(Input, DeviceID, Region, Key, Lang);
            }
            else if (Input == InputMicrophone)
            {
                bResult = TranscriptFromWasapi(Input, DeviceID, Region, Key, Lang);
            }
            else if (File.Exists(Input))
            {
                bResult = TranscriptFromFile(Input, Region, Key, Lang).Result;
            }
            else
            {
                string ErrorMessage = "Unexpected Transcript input parameter: " + Input;
                Console.WriteLine(ErrorMessage);
                Console.WriteLine(string.Format(InformationCSAudioTool, VersionString));
            }
            return bResult;
        }

    }
}