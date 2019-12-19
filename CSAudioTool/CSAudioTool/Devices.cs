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
        static bool ListDevices()
        {
            bool bResult = false;
            Console.WriteLine("Listing devices...");
            var enumerator = new MMDeviceEnumerator();
            MMDeviceCollection devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
            Console.WriteLine("Capture available devices:");
            if (devices.Count > 0)
                foreach (var d in devices)
                {
                    Console.WriteLine("Name: " + d.FriendlyName + " ID:" + d.ID);
                }
            else
                Console.WriteLine("No device found");
            Console.WriteLine("Capture default device:");
            Console.WriteLine("Name: " + device.FriendlyName + " ID:" + device.ID);

            devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            Console.WriteLine("Render available devices:");
            if (devices.Count > 0)
                foreach (var d in devices)
                {
                    Console.WriteLine("Name: " + d.FriendlyName + " ID:" + d.ID);
                }
            else
                Console.WriteLine("No device found");

            Console.WriteLine("Render default device:");
            Console.WriteLine("Name: " + device.FriendlyName + " ID:" + device.ID);
            return bResult;
        }

        static MMDevice GetDevice(string DeviceID)
        {

            var enumerator = new MMDeviceEnumerator();
            MMDeviceCollection devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            foreach (var d in devices)
            {
                if (d.ID == DeviceID)
                    return d;
            }
            return null;
        }
    }
}