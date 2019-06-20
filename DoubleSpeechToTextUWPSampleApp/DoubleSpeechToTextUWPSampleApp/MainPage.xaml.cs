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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Devices.Enumeration;

namespace DoubleSpeechToTextUWPSampleApp
{
    /// <summary>
    /// Main page for the application.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Windows.Media.Playback.MediaPlayer mediaPlayer;
        SpeechClient.SpeechClient speechClient1;
        SpeechClient.SpeechClient speechClient2;
        string lastSubscriptionKey;
        string lastHostname;
        string lastCustomEndpointID;
        DateTime lastTokenDate;
        UInt16 level = 300;
        UInt16 duration = 1000;
        bool isRecordingContinuously = false;
        bool bUseWebSocket = false;

        string[] TextToSpeechLanguageArray =
            {   "ar-EG",
                "ar-SA",
                "bg-BG",
                "ca-ES",
                "cs-CZ",
                "da-DK",
                "de-AT",
                "de-CH",
                "de-DE",
                "el-GR",
                "en-AU",
                "en-CA",
                "en-GB",
                "en-IE",
                "en-IN",
                "en-US",
                "es-ES",
                "es-MX",
                "fi-FI",
                "fr-CA",
                "fr-CH",
                "fr-FR",
                "he-IL",
                "hi-IN",
                "hr-HR",
                "hu-HU",
                "id-ID",
                "it-IT",
                "ja-JP",
                "ko-KR",
                "ms-MY",
                "nb-NO",
                "nl-NL",
                "pl-PL",
                "pt-BR",
                "pt-PT",
                "ro-RO",
                "ru-RU",
                "sk-SK",
                "sk-SI",
                "sv-SE",
                "ta-IN",
                "th-TH",
                "tr-TR",
                "vi-VN",
                "zh-CN",
                "zh-HK",
                "zh-TW"
                };

        string[] SpeechToTextLanguageArray =
            {   "ar-EG",
                "ca-ES",
                "da-DK",
                "de-DE",
                "en-AU",
                "en-CA",
                "en-GB",
                "en-IN",
                "en-NZ",
                "en-US",
                "es-ES",
                "es-MX",
                "fi-FI",
                "fr-CA",
                "fr-FR"
                };
        string[] SpeechToTextConversationLanguageArray =
            {   "ar-EG",
                "de-DE",
                "en-US",
                "es-ES",
                "fr-FR"
                };

        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            LogMessage("MainPage OnNavigatedTo");
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size
            {
                Height = 436,
                Width = 320
            });
            // Hide Systray on phone
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                // Hide Status bar
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                await statusBar.HideAsync();
            }

            // Logs event to refresh the TextBox
            logs.TextChanged += Logs_TextChanged;

            // resultText event to refresh the TextBox
            resultText1.TextChanged += ResultText1_TextChanged;
            resultText2.TextChanged += ResultText2_TextChanged;

            // Bind player to element
            mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            mediaPlayerElement.SetMediaPlayer(mediaPlayer);
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;

            // Fill ComboBox Hostname
            ComboHostname.Items.Clear();
            ComboHostname.Items.Add(defaultBingSpeechHostname);
            foreach (var r in RegionArray)
            {
                ComboHostname.Items.Add(r + ".stt.speech.microsoft.com");
            }
            ComboHostname.SelectedIndex = 0;

            // Fill Combobox API
            ComboAPI.Items.Clear();
            ComboAPI.Items.Add("conversation");
            ComboAPI.Items.Add("interactive");
            ComboAPI.Items.Add("dictation");
            ComboAPI.SelectedIndex = 0;

            ComboAPIResult.Items.Clear();
            ComboAPIResult.Items.Add("simple");
            ComboAPIResult.Items.Add("detailed");
            ComboAPIResult.SelectedIndex = 0;

            speechToTextLanguage.Items.Clear();
            foreach(var l in SpeechToTextLanguageArray)
                speechToTextLanguage.Items.Add(l);
            speechToTextLanguage.SelectedItem = "en-US";


            // Get Subscription ID from the local settings
            ReadSettingsAndState();
            
            // Update control and play first video
            UpdateControls();

            
            // Register Suspend/Resume
            Application.Current.Suspending += Current_Suspending;
            Application.Current.Resuming += Current_Resuming;
            
            // Display OS, Device information
            LogMessage(SystemInformation.GetString());

            await FillComboRecordingDevices();
            // If there is no device mounted on the desired panel, return the first device found.

            // Initialize level and duration
            //Level.Text = level.ToString();
            //Duration.Text = duration.ToString();
            //Level.TextChanged += Level_TextChanged;
            //Duration.TextChanged += Duration_TextChanged;

            // Create Speech Client if possible 
            await CreateSpeechClient1();
            await CreateSpeechClient2();

        }
        const string NoDeviceLabel = "No Device Selected";
        const string defaultDeviceLabel = "Default Device";
        private async System.Threading.Tasks.Task<bool> FillComboRecordingDevices()
        {
            ComboDevice1.Items.Add(NoDeviceLabel);
            ComboDevice1.Items.Add(defaultDeviceLabel);
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            foreach (var d in allVideoDevices)
            {
                ComboDevice1.Items.Add(d.Name);
            }
            if (ComboDevice1.Items.Count > 0)
            {
                ComboDevice1.SelectedIndex = 0;
            }
            ComboDevice2.Items.Add(NoDeviceLabel);
            ComboDevice2.Items.Add(defaultDeviceLabel);
            foreach (var d in allVideoDevices)
            {
                ComboDevice2.Items.Add(d.Name);
            }
            if (ComboDevice2.Items.Count > 0)
            {
                ComboDevice2.SelectedIndex = 0;
            }
            if ((ComboDevice1.Items.Count > 0) &&
                (ComboDevice2.Items.Count > 0))
                return true;
            return false;
        }
        private async System.Threading.Tasks.Task<string> GetCurrentRecordingDeviceId1()
        {
            string result = string.Empty;
            if (ComboDevice1.Items.Count > 0)
            {
                
                string name = ComboDevice1.SelectedItem as string;
                if (!string.IsNullOrEmpty(name))
                {
                    if (string.Equals(name, defaultDeviceLabel))
                        return string.Empty;
                    if (string.Equals(name, NoDeviceLabel))
                        return null;
                    var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
                    foreach (var d in allVideoDevices)
                    {
                        if (string.Equals(name, d.Name))
                        {
                            result = d.Id;
                            break;
                        }
                    }
                }
            }
            return result;
        }
        private async System.Threading.Tasks.Task<string> GetCurrentRecordingDeviceId2()
        {
            string result = string.Empty;
            if (ComboDevice2.Items.Count > 0)
            {

                string name = ComboDevice2.SelectedItem as string;
                if (!string.IsNullOrEmpty(name))
                {
                    if (string.Equals(name, defaultDeviceLabel))
                        return string.Empty;
                    if (string.Equals(name, NoDeviceLabel))
                        return null;
                    var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
                    foreach (var d in allVideoDevices)
                    {
                        if (string.Equals(name, d.Name))
                        {
                            result = d.Id;
                            break;
                        }
                    }
                }
            }
            return result;
        }
        async System.Threading.Tasks.Task<bool> CreateSpeechClient1()
        {
            bool result = false;
            if ((speechClient1 != null) &&
                (string.Equals(lastSubscriptionKey, subscriptionKey.Text.ToString())) &&
                (string.Equals(lastHostname, ComboHostname.SelectedItem.ToString())) &&
                (string.Equals(lastCustomEndpointID, customEndpointID.Text)) &&
                ((DateTime.Now - lastTokenDate) < TimeSpan.FromSeconds(60 * 5))
                )
                return true;
            // Cognitive Service SpeechToText GetToken 
            if (!string.IsNullOrEmpty(subscriptionKey.Text))
            {
                if(speechClient1 != null)
                {
                    await speechClient1.Cleanup();
                }
                if (!string.IsNullOrEmpty(customEndpointID.Text))
                {
                    LogMessage("Creating Custom Speech Client: " + ComboHostname.SelectedItem.ToString() + " Key: " + subscriptionKey.Text.ToString() + " EndpointID: " + customEndpointID.Text);
                    speechClient1 = await SpeechClient.SpeechClient.CreateCustomSpeechClient(ComboHostname.SelectedItem.ToString(), subscriptionKey.Text, customEndpointID.Text);
                }
                else
                {
                    if (string.Equals("speech.platform.bing.com", ComboHostname.SelectedItem.ToString().ToLower()))
                    {
                        LogMessage("Creating Bing Speech Client: " + ComboHostname.SelectedItem.ToString() + " Key: " + subscriptionKey.Text.ToString());
                        speechClient1 = await SpeechClient.SpeechClient.CreateBingSpeechClient(subscriptionKey.Text);
                    }
                    else
                    {
                        LogMessage("Creating Speech Client: " + ComboHostname.SelectedItem.ToString() + " Key: " + subscriptionKey.Text.ToString());
                        speechClient1 = await SpeechClient.SpeechClient.CreateSpeechClient(ComboHostname.SelectedItem.ToString(), subscriptionKey.Text);
                    }
                }
                if (speechClient1 != null)
                {
                    LogMessage("Speech Client successfully created");
                    string s = speechClient1.Token;
                    if (!string.IsNullOrEmpty(s))
                    {
                        lastSubscriptionKey = subscriptionKey.Text.ToString();
                        lastHostname = ComboHostname.SelectedItem.ToString();
                        lastCustomEndpointID = customEndpointID.Text;
                        lastTokenDate = DateTime.Now;
                        LogMessage("Getting Token successful Token: " + s.ToString());
                        result = true;
                    }
                    else
                        LogMessage("Getting Token failed for subscription Key: " + subscriptionKey.Text);

                }
                else
                    LogMessage("Error while creating the Speech Client");
            }
            else
                LogMessage("Subscription Key required to create the Speech Client");
            return result;
        }
        async System.Threading.Tasks.Task<bool> CreateSpeechClient2()
        {
            bool result = false;
            if ((speechClient2 != null) &&
                (string.Equals(lastSubscriptionKey, subscriptionKey.Text.ToString())) &&
                (string.Equals(lastHostname, ComboHostname.SelectedItem.ToString())) &&
                (string.Equals(lastCustomEndpointID, customEndpointID.Text)) &&
                ((DateTime.Now - lastTokenDate) < TimeSpan.FromSeconds(60 * 5))
                )
                return true;
            // Cognitive Service SpeechToText GetToken 
            if (!string.IsNullOrEmpty(subscriptionKey.Text))
            {
                if (speechClient2 != null)
                {
                    await speechClient2.Cleanup();
                }
                if (!string.IsNullOrEmpty(customEndpointID.Text))
                {
                    LogMessage("Creating Custom Speech Client: " + ComboHostname.SelectedItem.ToString() + " Key: " + subscriptionKey.Text.ToString() + " EndpointID: " + customEndpointID.Text);
                    speechClient2 = await SpeechClient.SpeechClient.CreateCustomSpeechClient(ComboHostname.SelectedItem.ToString(), subscriptionKey.Text, customEndpointID.Text);
                }
                else
                {
                    if (string.Equals("speech.platform.bing.com", ComboHostname.SelectedItem.ToString().ToLower()))
                    {
                        LogMessage("Creating Bing Speech Client: " + ComboHostname.SelectedItem.ToString() + " Key: " + subscriptionKey.Text.ToString());
                        speechClient2 = await SpeechClient.SpeechClient.CreateBingSpeechClient(subscriptionKey.Text);
                    }
                    else
                    {
                        LogMessage("Creating Speech Client: " + ComboHostname.SelectedItem.ToString() + " Key: " + subscriptionKey.Text.ToString());
                        speechClient2 = await SpeechClient.SpeechClient.CreateSpeechClient(ComboHostname.SelectedItem.ToString(), subscriptionKey.Text);
                    }
                }
                if (speechClient2 != null)
                {
                    LogMessage("Speech Client successfully created");
                    string s = speechClient2.Token;
                    if (!string.IsNullOrEmpty(s))
                    {
                        lastSubscriptionKey = subscriptionKey.Text.ToString();
                        lastHostname = ComboHostname.SelectedItem.ToString();
                        lastCustomEndpointID = customEndpointID.Text;
                        lastTokenDate = DateTime.Now;
                        LogMessage("Getting Token successful Token: " + s.ToString());
                        result = true;
                    }
                    else
                        LogMessage("Getting Token failed for subscription Key: " + subscriptionKey.Text);

                }
                else
                    LogMessage("Error while creating the Speech Client");
            }
            else
                LogMessage("Subscription Key required to create the Speech Client");
            return result;
        }
        /// <summary>
        /// This method is called when the EndpointID is changed
        /// </summary>
        private void CustomEndpointID_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateControls();
        }
        /// <summary>
        /// This method is called when the Duration TextBox changed  
        /// </summary>
        private void Duration_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(sender is TextBox tb)
            {

                if (!uint.TryParse(tb.Text, out uint n))
                {
                    tb.Text = duration.ToString();
                }
                else
                {
                    if ((n > 0) && (n < 65535))
                    {
                        duration = (ushort) n;
                    }
                    else
                        tb.Text = duration.ToString();
                }
            }
        }


        /// <summary>
        /// This method is called when the Level TextBox changed  
        /// </summary>
        private void Level_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (!uint.TryParse(tb.Text, out uint n))
                {
                    tb.Text = level.ToString();
                }
                else
                {
                    if((n>0)&&(n<65535))
                    {
                        level = (ushort)n;
                    }
                    else
                        tb.Text = level.ToString();
                }
            }
        }

        /// <summary>
        /// Method OnNavigatedFrom
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            LogMessage("MainPage OnNavigatedFrom");
            // Unregister Suspend/Resume
            Application.Current.Suspending -= Current_Suspending;
            Application.Current.Resuming -= Current_Resuming;
        }
        /// <summary>
        /// This method is called when the application is resuming
        /// </summary>
        void Current_Resuming(object sender, object e)
        {
            LogMessage("Resuming");
            ReadSettingsAndState();


            // Resotre Playback Rate
            if (mediaPlayer.PlaybackSession.PlaybackRate != 1)
                mediaPlayer.PlaybackSession.PlaybackRate = 1;

            //Update Controls
            UpdateControls();
        }
        /// <summary>
        /// This method is called when the application is suspending
        /// </summary>
        async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            LogMessage("Suspending");
            var deferal = e.SuspendingOperation.GetDeferral();

            if ((speechClient1 != null) && (speechClient1.IsRecording()))
            {
                LogMessage("Stop Recording for device 1...");
                await speechClient1.StopRecording();
                speechClient1.AudioLevel -= Client_AudioLevel;
                speechClient1.AudioCaptureError -= Client_AudioCaptureError;
                speechClient1.WebSocketEvent -= Client_WebSocketEvent;
                isRecordingContinuously = false;
            }
            if ((speechClient2 != null) && (speechClient2.IsRecording()))
            {
                LogMessage("Stop Recording for device 2...");
                await speechClient2.StopRecording();
                speechClient2.AudioLevel -= Client_AudioLevel;
                speechClient2.AudioCaptureError -= Client_AudioCaptureError;
                speechClient2.WebSocketEvent -= Client_WebSocketEvent;
                isRecordingContinuously = false;
            }
            SaveSettingsAndState();
            deferal.Complete();
        }

        #region Settings
        string[] RegionArray = {
"australiaeast",
"canadacentral",
"centralus",
"eastasia",
"eastus",
"eastus2",
"francecentral",
"centralindia",
"japaneast",
"koreacentral",
"northcentralus",
"northeurope",
"southcentralus",
"southeastasia",
"uksouth",
"westeurope",
"westus",
"westus2"};



        const string keyHostname = "hostnameKey";
        const string keySpeechSubscription = "SpeechSubscriptionKey";
        const string keySpeechEndPointID = "SpeechEndPointIDKey";
        //const string keyBingSpeechSubscription = "bingSpeechSubscriptionKey";
        //const string keyWestUsSpeechSubscription = "westUsSpeechSubscriptionKey";
        //const string keyEastAsiaSpeechSubscription = "eastAsiaSpeechSubscriptionKey";
        //const string keyNorthEuropeSpeechSubscription = "northEuropeSpeechSubscriptionKey";
        //const string keyWestUsEndPointID = "westUsEndPointIDKey";
        //const string keyEastAsiaEndPointID  = "eastAsiaEndPointIDKey";
        //const string keyNorthEuropeEndPointID = "northEuropeEndPointIDKey";
        const string keyLevel = "levelKey";
        const string keyDuration = "durationKey";
        const string keyWebSocket = "webSocketKey";

        const string defaultBingSpeechHostname = "speech.platform.bing.com";
        //const string defaultWestUSSpeechHostname = "westus.stt.speech.microsoft.com";
        //const string defaultEastAsiaSpeechHostname = "eastasia.stt.speech.microsoft.com";
        //const string defaultNorthEuropeSpeechHostname = "northeurope.stt.speech.microsoft.com";

        //string valueBingSpeechSubscription = string.Empty;
        //string valueWestUsSpeechSubscription = string.Empty;
        //string valueEastAsiaSpeechSubscription = string.Empty;
        //string valueNorthEuropeSpeechSubscription = string.Empty;
        //string valueWestUsEndPointID = string.Empty;
        //string valueEastAsiaEndPointID = string.Empty;
        //string valueNorthEuropeEndPointID = string.Empty;

        string valueSpeechHostname = string.Empty;
        string valueSpeechSubscription = string.Empty;
        string valueSpeechEndPointID = string.Empty;

        /// <summary>
        /// Function to save all the persistent attributes
        /// </summary>
        public bool SaveSettingsAndState()
        {

            SaveSettingsValue(keyHostname, valueSpeechHostname);
            SaveSettingsValue(keySpeechSubscription, valueSpeechSubscription);
            SaveSettingsValue(keySpeechEndPointID, valueSpeechEndPointID);

            //if (ComboHostname.SelectedItem.ToString()== defaultBingSpeechHostname)
            //{
            //    valueBingSpeechSubscription = subscriptionKey.Text;

            //}
            //else if (ComboHostname.SelectedItem.ToString() == defaultWestUSSpeechHostname)
            //{
            //    valueWestUsSpeechSubscription = subscriptionKey.Text;
            //    valueWestUsEndPointID = customEndpointID.Text;
            //}
            //else if (ComboHostname.SelectedItem.ToString() == defaultEastAsiaSpeechHostname)
            //{
            //    valueEastAsiaSpeechSubscription = subscriptionKey.Text;
            //    valueEastAsiaEndPointID = customEndpointID.Text;
            //}
            //else if (ComboHostname.SelectedItem.ToString() == defaultNorthEuropeSpeechHostname)
            //{
            //    valueNorthEuropeSpeechSubscription = subscriptionKey.Text;
            //    valueNorthEuropeEndPointID = customEndpointID.Text;
            //}
            //SaveSettingsValue(keyBingSpeechSubscription, valueBingSpeechSubscription);
            //SaveSettingsValue(keyWestUsSpeechSubscription, valueWestUsSpeechSubscription);
            //SaveSettingsValue(keyWestUsEndPointID, valueWestUsEndPointID);
            //SaveSettingsValue(keyEastAsiaSpeechSubscription, valueEastAsiaSpeechSubscription);
            //SaveSettingsValue(keyEastAsiaEndPointID, valueEastAsiaEndPointID);
            //SaveSettingsValue(keyNorthEuropeSpeechSubscription, valueNorthEuropeSpeechSubscription);
            //SaveSettingsValue(keyNorthEuropeEndPointID, valueNorthEuropeEndPointID);

            bUseWebSocket = SpeechApiType.IsOn;
            SaveSettingsValue(keyWebSocket, bUseWebSocket.ToString());
            SaveSettingsValue(keyLevel, level.ToString());
            SaveSettingsValue(keyDuration, duration.ToString());

            return true;
        }
        /// <summary>
        /// Function to read all the persistent attributes
        /// </summary>
        public bool ReadSettingsAndState()
        {
            string s = ReadSettingsValue(keyHostname) as string;
            if (!string.IsNullOrEmpty(s))
                ComboHostname.SelectedItem = s;
            else
                ComboHostname.SelectedItem = defaultBingSpeechHostname;

            //valueBingSpeechSubscription = ReadSettingsValue(keyBingSpeechSubscription) as string;
            //if (valueBingSpeechSubscription ==null) valueBingSpeechSubscription = string.Empty;

            //valueWestUsSpeechSubscription = ReadSettingsValue(keyWestUsSpeechSubscription) as string;
            //if (valueWestUsSpeechSubscription == null) valueWestUsSpeechSubscription = string.Empty;

            //valueEastAsiaSpeechSubscription = ReadSettingsValue(keyEastAsiaSpeechSubscription) as string;
            //if (valueEastAsiaSpeechSubscription == null) valueEastAsiaSpeechSubscription = string.Empty;

            //valueNorthEuropeSpeechSubscription = ReadSettingsValue(keyNorthEuropeSpeechSubscription) as string;
            //if (valueNorthEuropeSpeechSubscription == null) valueNorthEuropeSpeechSubscription = string.Empty;

            //valueWestUsEndPointID = ReadSettingsValue(keyWestUsEndPointID) as string;
            //if (valueWestUsEndPointID == null) valueWestUsEndPointID = string.Empty;

            //valueEastAsiaEndPointID = ReadSettingsValue(keyEastAsiaEndPointID) as string;
            //if (valueEastAsiaEndPointID == null) valueEastAsiaEndPointID = string.Empty;

            //valueNorthEuropeEndPointID = ReadSettingsValue(keyNorthEuropeEndPointID) as string;
            //if (valueNorthEuropeEndPointID == null) valueNorthEuropeEndPointID = string.Empty;

            //if (ComboHostname.SelectedItem.ToString() == defaultBingSpeechHostname)
            //{
            //    subscriptionKey.Text = valueBingSpeechSubscription;
            //    customEndpointID.Text = string.Empty;
            //}
            //else if (ComboHostname.SelectedItem.ToString() == defaultWestUSSpeechHostname)
            //{
            //    subscriptionKey.Text = valueWestUsSpeechSubscription;
            //    customEndpointID.Text = valueWestUsEndPointID;
            //}
            //else if (ComboHostname.SelectedItem.ToString() == defaultEastAsiaSpeechHostname)
            //{
            //    subscriptionKey.Text = valueEastAsiaSpeechSubscription;
            //    customEndpointID.Text = valueEastAsiaEndPointID;
            //}
            //else if (ComboHostname.SelectedItem.ToString() == defaultNorthEuropeSpeechHostname)
            //{
            //    subscriptionKey.Text = valueNorthEuropeSpeechSubscription;
            //    customEndpointID.Text = valueNorthEuropeEndPointID;
            //}
            s = ReadSettingsValue(keySpeechSubscription) as string;
            if (!string.IsNullOrEmpty(s))
                subscriptionKey.Text = s;

            s = ReadSettingsValue(keySpeechEndPointID) as string;
            if (!string.IsNullOrEmpty(s))
                customEndpointID.Text = s;

            s = ReadSettingsValue(keyLevel) as string;
            if (!string.IsNullOrEmpty(s))
                UInt16.TryParse(s, out level);
            s = ReadSettingsValue(keyDuration) as string;
            if (!string.IsNullOrEmpty(s))
                UInt16.TryParse(s, out duration);
            s = ReadSettingsValue(keyWebSocket) as string;
            if (!string.IsNullOrEmpty(s))
                bool.TryParse(s, out bUseWebSocket);
            SpeechApiType.IsOn = bUseWebSocket;
            return true;
        }
        /// <summary>
        /// Function to read a setting value and clear it after reading it
        /// </summary>
        public static object ReadSettingsValue(string key)
        {
            if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                return null;
            }
            else
            {
                var value = Windows.Storage.ApplicationData.Current.LocalSettings.Values[key];
                Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove(key);
                return value;
            }
        }

        /// <summary>
        /// Save a key value pair in settings. Create if it doesn't exist
        /// </summary>
        public static void SaveSettingsValue(string key, object value)
        {
            if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(key))
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values.Add(key, value);
            }
            else
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values[key] = value;
            }
        }
        #endregion Settings

        #region Logs
        void PushMessage(string Message)
        {
            if (Windows.UI.Xaml.Application.Current is App app)
                app.MessageList.Enqueue(Message);
        }
        bool PopMessage(out string Message)
        {
            Message = string.Empty;
            if(Windows.UI.Xaml.Application.Current is App app)
                return app.MessageList.TryDequeue(out Message);
            return false;
        }
        /// <summary>
        /// Display Message on the application page
        /// </summary>
        /// <param name="Message">String to display</param>
        async void LogMessage(string Message)
        {
            string Text = string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " " + Message + "\n";
            PushMessage(Text);
            System.Diagnostics.Debug.WriteLine(Text);
            await DisplayLogMessage();
        }
        /// <summary>
        /// Display Message on the application page
        /// </summary>
        /// <param name="Message">String to display</param>
        async System.Threading.Tasks.Task<bool> DisplayLogMessage()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {

                    while (PopMessage(out string result))
                    {
                        logs.Text += result;
                        if (logs.Text.Length > 16000)
                        {
                            string LocalString = logs.Text;
                            while (LocalString.Length > 12000)
                            {
                                int pos = LocalString.IndexOf('\n');
                                if (pos == -1)
                                    pos = LocalString.IndexOf('\r');


                                if ((pos >= 0) && (pos < LocalString.Length))
                                {
                                    LocalString = LocalString.Substring(pos + 1);
                                }
                                else
                                    break;
                            }
                            logs.Text = LocalString;
                        }
                    }
                }
            );
            return true;
        }
        /// <summary>
        /// This method is called when the content of the Logs TextBox changed  
        /// The method scroll to the bottom of the TextBox
        /// </summary>
        void Logs_TextChanged(object sender, TextChangedEventArgs e)
        {
            //  logs.Focus(FocusState.Programmatic);
            // logs.Select(logs.Text.Length, 0);
            var tbsv = GetFirstDescendantScrollViewer(logs);
            tbsv.ChangeView(null, tbsv.ScrollableHeight, null, true);
        }
        /// <summary>
        /// This method is called when the content of the resultText TextBox changed  
        /// The method scroll to the bottom of the TextBox
        /// </summary>
        void ResultText1_TextChanged(object sender, TextChangedEventArgs e)
        {
            //  logs.Focus(FocusState.Programmatic);
            // logs.Select(logs.Text.Length, 0);
            var tbsv = GetFirstDescendantScrollViewer(resultText1);
            tbsv.ChangeView(null, tbsv.ScrollableHeight, null, true);
        }
        void ResultText2_TextChanged(object sender, TextChangedEventArgs e)
        {
            //  logs.Focus(FocusState.Programmatic);
            // logs.Select(logs.Text.Length, 0);
            var tbsv = GetFirstDescendantScrollViewer(resultText2);
            tbsv.ChangeView(null, tbsv.ScrollableHeight, null, true);
        }
        /// <summary>
        /// Retrieve the ScrollViewer associated with a control  
        /// </summary>
        ScrollViewer GetFirstDescendantScrollViewer(DependencyObject parent)
        {
            var c = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < c; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if(child is ScrollViewer sv)
                    return sv;
                sv = GetFirstDescendantScrollViewer(child);
                if (sv != null)
                    return sv;
            }

            return null;
        }
        #endregion

        #region Media
        /// <summary>
        /// Mute method 
        /// </summary>
        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Toggle Mute");
            mediaPlayer.IsMuted = !mediaPlayer.IsMuted;
            UpdateControls();
        }
        /// <summary>
        /// Volume Up method 
        /// </summary>
        private void VolumeUp_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Volume Up");
            mediaPlayer.Volume = (mediaPlayer.Volume + 0.10 <= 1 ? mediaPlayer.Volume + 0.10 : 1);
            UpdateControls();
        }
        /// <summary>
        /// Volume Down method 
        /// </summary>
        private void VolumeDown_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Volume Down");
            mediaPlayer.Volume = (mediaPlayer.Volume - 0.10 >= 0 ? mediaPlayer.Volume - 0.10 : 0);
            UpdateControls();
        }

        /// <summary>
        /// GetFileFromLocalPathUrl
        /// Return the StorageFile associated with the url  
        /// </summary>
        /// <param name="PosterUrl">Url string of the content</param>
        /// <returns>StorageFile</returns>
        private async System.Threading.Tasks.Task<Windows.Storage.StorageFile> GetFileFromLocalPathUrl(string PosterUrl)
        {
            string path = null;
            Windows.Storage.StorageFolder folder = null;
            if (PosterUrl.ToLower().StartsWith("picture://"))
            {
                folder = Windows.Storage.KnownFolders.PicturesLibrary;
                path = PosterUrl.Replace("picture://", "");
            }
            else if (PosterUrl.ToLower().StartsWith("music://"))
            {
                folder = Windows.Storage.KnownFolders.MusicLibrary;
                path = PosterUrl.Replace("music://", "");
            }
            else if (PosterUrl.ToLower().StartsWith("video://"))
            {
                folder = Windows.Storage.KnownFolders.VideosLibrary;
                path = PosterUrl.Replace("video://", "");
            }
            else if (PosterUrl.ToLower().StartsWith("file://"))
            {
                path = PosterUrl.Replace("file://", "");
            }
            Windows.Storage.StorageFile file = null;
            try
            {
                if (folder != null)
                {
                    string ext = System.IO.Path.GetExtension(path);
                    string filename = System.IO.Path.GetFileName(path);
                    string directory = System.IO.Path.GetDirectoryName(path);
                    while (!string.IsNullOrEmpty(directory))
                    {

                        string subdirectory = directory;
                        int pos = -1;
                        if ((pos = directory.IndexOf('\\')) > 0)
                            subdirectory = directory.Substring(0, pos);
                        folder = await folder.GetFolderAsync(subdirectory);
                        if (folder != null)
                        {
                            if (pos > 0)
                                directory = directory.Substring(pos + 1);
                            else
                                directory = string.Empty;
                        }
                    }
                    if (folder != null)
                        file = await folder.GetFileAsync(filename);
                }
                else
                    file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
            }
            catch (Exception e)
            {
                LogMessage("Exception while opening file: " + PosterUrl + " exception: " + e.Message);
            }
            return file;
        }


        private void MediaPlayer_MediaOpened(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            LogMessage("MediaPlayer media opened event");
            UpdateControls();

        }

        private void MediaPlayer_MediaFailed(Windows.Media.Playback.MediaPlayer sender, Windows.Media.Playback.MediaPlayerFailedEventArgs args)
        {
            LogMessage("MediaPlayer media failed event");
            UpdateControls();
        }

        private void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            LogMessage("MediaPlayer media ended event" );
            UpdateControls();
        }




        #endregion Media

        #region LevelAndError
        DateTime LastLevelTime = DateTime.Now;
        double maxValue = 0;
        bool bDrawingMessage = false;
        async void Client_AudioLevel(object sender, double reading)
        {
            if ((DateTime.Now - LastLevelTime).TotalMilliseconds > 200)
            {
                LastLevelTime = DateTime.Now;
                if (maxValue == 0)
                {
                    maxValue = 1;
                    return;
                }
                //LogMessage("Amplitude: " + reading.ToString());

                double value = reading > 32768 ? 32768 : reading;
                if (value > maxValue)
                    maxValue = value;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        if(sender ==speechClient1)
                            DrawLevel1(value,Windows.UI.Colors.Cyan);
                        else if (sender == speechClient2)
                            DrawLevel2(value, Windows.UI.Colors.Cyan);

                    });
            }
        }
        void DrawLevel1(double value, Windows.UI.Color cr )
        {
            if ((bDrawingMessage == true)&&(cr==Windows.UI.Colors.Cyan))
                return;
            if (CanvasGraph1.Children.Count > 0) CanvasGraph1.Children.Clear();
            Windows.UI.Xaml.Shapes.Line line = new Windows.UI.Xaml.Shapes.Line() { X1 = 0, Y1 = CanvasGraph1.Height / 2, X2 = ((value * CanvasGraph1.Width) / maxValue), Y2 = CanvasGraph1.Height / 2 };
            line.StrokeThickness = CanvasGraph1.Height;
            line.Stroke = new SolidColorBrush(cr);
            CanvasGraph1.Children.Add(line);
        }
        void DrawLevel2(double value, Windows.UI.Color cr)
        {
            if ((bDrawingMessage == true) && (cr == Windows.UI.Colors.Cyan))
                return;
            if (CanvasGraph2.Children.Count > 0) CanvasGraph2.Children.Clear();
            Windows.UI.Xaml.Shapes.Line line = new Windows.UI.Xaml.Shapes.Line() { X1 = 0, Y1 = CanvasGraph2.Height / 2, X2 = ((value * CanvasGraph2.Width) / maxValue), Y2 = CanvasGraph2.Height / 2 };
            line.StrokeThickness = CanvasGraph2.Height;
            line.Stroke = new SolidColorBrush(cr);
            CanvasGraph2.Children.Add(line);
        }
        void DrawError1()
        {
            bDrawingMessage = true;
            DrawLevel1(maxValue, Windows.UI.Colors.Red);
            var t = System.Threading.Tasks.Task.Run(async delegate
            {
                await System.Threading.Tasks.Task.Delay(2000);
                ClearCanvas();
            });
        }
        void DrawError2()
        {
            bDrawingMessage = true;
            DrawLevel2(maxValue, Windows.UI.Colors.Red);
            var t = System.Threading.Tasks.Task.Run(async delegate
            {
                await System.Threading.Tasks.Task.Delay(2000);
                ClearCanvas();
            });
        }
        void DrawOk1()
        {
            bDrawingMessage = true;
            DrawLevel1(maxValue, Windows.UI.Colors.GreenYellow);
            var t = System.Threading.Tasks.Task.Run(async delegate
            {
                await System.Threading.Tasks.Task.Delay(2000);
                ClearCanvas();
            });
        }
        void DrawOk2()
        {
            bDrawingMessage = true;
            DrawLevel2(maxValue, Windows.UI.Colors.GreenYellow);
            var t = System.Threading.Tasks.Task.Run(async delegate
            {
                await System.Threading.Tasks.Task.Delay(2000);
                ClearCanvas();
            });
        }
        void ClearCanvas()
        {
            bDrawingMessage = false;
            if (CanvasGraph1.Children.Count > 0) CanvasGraph1.Children.Clear();
            if (CanvasGraph2.Children.Count > 0) CanvasGraph2.Children.Clear();
        }
        private async void Client_AudioCaptureError(SpeechClient.SpeechClient sender, string message)
        {
            if (sender == speechClient1)
            {
                LogMessage("Audio Capture Error for device 1: " + message);
                LogMessage("Stop Recording for device 1...");
            }
            if (sender == speechClient2)
            {
                LogMessage("Audio Capture Error for device 2: " + message);
                LogMessage("Stop Recording for device 2...");
            }
            await sender.StopRecording();
            sender.AudioLevel -= Client_AudioLevel;
            sender.AudioCaptureError -= Client_AudioCaptureError;
            sender.WebSocketEvent -= Client_WebSocketEvent;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    ClearCanvas();
                    UpdateControls();
                });
        }
        #endregion LevelAndError

        #region ui
        /// <summary>
        /// UpdateControls Method which update the controls on the page  
        /// </summary>
        async void UpdateControls()
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 () =>
                 {
                     {
                         // If WebSocket API conversation enabled
                         if(SpeechApiType.IsOn == true)
                         {
                             if (ComboAPI.Items.Count != 3)
                             {
                                 string oldSelction = ComboAPI.SelectedItem.ToString();
                                 // Fill Combobox API
                                 ComboAPI.Items.Clear();
                                 ComboAPI.Items.Add("conversation");
                                 ComboAPI.Items.Add("interactive");
                                 ComboAPI.Items.Add("dictation");
                                 if (ComboAPI.Items.Contains(oldSelction))
                                     ComboAPI.SelectedItem = oldSelction;
                                 else
                                     ComboAPI.SelectedIndex = 0;
                             }
                         }
                         // REST API
                         else
                         {
                             if (ComboAPI.Items.Count != 2)
                             {
                                 string oldSelction = ComboAPI.SelectedItem.ToString();
                                 // Fill Combobox API
                                 ComboAPI.Items.Clear();
                                 ComboAPI.Items.Add("interactive");
                                 ComboAPI.Items.Add("dictation");
                                 if (ComboAPI.Items.Contains(oldSelction))
                                     ComboAPI.SelectedItem = oldSelction;
                                 else
                                     ComboAPI.SelectedIndex = 0;
                             }
                         }
                         // if hostname is bing speech hostname, user can change language,
                         // if hostname is different, it's custom speech user can't change the language
                         if (string.Equals(ComboHostname.SelectedItem.ToString(), defaultBingSpeechHostname))
                         {
                             speechToTextLanguage.IsEnabled = true;
                             if (ComboAPI.SelectedItem.ToString() == "conversation")
                             {
                                 if(speechToTextLanguage.Items.Count != SpeechToTextConversationLanguageArray.Count())
                                 {
                                     string oldSelection = speechToTextLanguage.SelectedItem.ToString();
                                     speechToTextLanguage.Items.Clear();
                                     foreach (var l in SpeechToTextConversationLanguageArray)
                                         speechToTextLanguage.Items.Add(l);
                                     if(speechToTextLanguage.Items.Contains(oldSelection))
                                         speechToTextLanguage.SelectedItem = oldSelection;
                                     else
                                        speechToTextLanguage.SelectedItem = "en-US";
                                 }
                             }
                             else
                             {
                                 if (speechToTextLanguage.Items.Count != SpeechToTextLanguageArray.Count())
                                 {
                                     string oldSelection = speechToTextLanguage.SelectedItem.ToString();
                                     speechToTextLanguage.Items.Clear();
                                     foreach (var l in SpeechToTextLanguageArray)
                                         speechToTextLanguage.Items.Add(l);
                                     if (speechToTextLanguage.Items.Contains(oldSelection))
                                         speechToTextLanguage.SelectedItem = oldSelection;
                                     else
                                         speechToTextLanguage.SelectedItem = "en-US";
                                 }
                             }
                             speechToTextLanguage.IsEnabled = true;
                             customEndpointID.IsEnabled = false;
                         }
                         else
                         {
                             // Custom Speech URL or new Speech Service URL selected 
                             customEndpointID.IsEnabled = true;
                             if (!string.IsNullOrEmpty(customEndpointID.Text))
                             {
                                 // Custom Speech Service
                                 speechToTextLanguage.IsEnabled = false;

                             }
                             else
                             {
                                 // New Speech Service
                                 speechToTextLanguage.IsEnabled = true;
                             }
                         }

                         if (((speechClient1 == null) || (!speechClient1.IsRecording())) &&
                             ((speechClient2 == null) || (!speechClient2.IsRecording())))
                         {
                             if ((bUseWebSocket) && (ComboAPI.SelectedItem.ToString() == "conversation"))
                                 continuousRecordingButton.IsEnabled = true;
                             else
                                 continuousRecordingButton.IsEnabled = false;

                             continuousRecordingButton.Content = "\xE895";
                         }
                         else
                         {
                             if (isRecordingContinuously == true)
                             { 
                                 if ((bUseWebSocket) && (ComboAPI.SelectedItem.ToString() == "conversation"))
                                     continuousRecordingButton.IsEnabled = true;
                                 else
                                     continuousRecordingButton.IsEnabled = false;
                             }
                             else
                                 continuousRecordingButton.IsEnabled = false;

                             continuousRecordingButton.Content = "\xE8D8";
                         }

                     }
                 });
        }
        bool bInProgress = false;
        /// <summary>
        /// sendContinuousAudioBuffer method which :
        /// - record audio sample permanently in the buffer
        /// - send the buffer to SpeechToText REST API once the recording is done
        /// </summary>
        private async void ContinuousRecording_Click(object sender, RoutedEventArgs e)
        {
            ClearResult();
            await LaunchContinuousRecording();
        }
        private async System.Threading.Tasks.Task<bool> LaunchContinuousRecording()
        {
            bool result = false;
            if (bInProgress == true)
                return result;
            bInProgress = true;
            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);

                await CreateSpeechClient1();
                await CreateSpeechClient2();
                if ((speechClient1 != null)&& (speechClient2 != null))
                {
                    valueSpeechHostname = ComboHostname.SelectedItem.ToString();
                    valueSpeechSubscription = subscriptionKey.Text;
                    valueSpeechEndPointID = customEndpointID.Text;

                    SaveSettingsAndState();
                    if ((speechClient1.IsRecording() == false)&&
                        (speechClient2.IsRecording() == false))
                    {
                        ClearCanvas();
                        if (await speechClient1.Cleanup())
                        {
                            string speechAPI = ComboAPI.SelectedItem.ToString();
                            string language = speechToTextLanguage.SelectedItem.ToString();
                            string resultType = ComboAPIResult.SelectedItem.ToString();
                            string recordResult = await GetCurrentRecordingDeviceId1();
                            if (recordResult != null)
                            {
                                if (await speechClient1.StartRecording(speechAPI, language, resultType, recordResult))
                                {

                                    isRecordingContinuously = true;
                                    speechClient1.AudioLevel += Client_AudioLevel;
                                    speechClient1.AudioCaptureError += Client_AudioCaptureError;
                                    speechClient1.WebSocketEvent += Client_WebSocketEvent;
                                    LogMessage("Start Recording for device 1...");
                                    result = true;
                                }
                                else
                                    LogMessage("Start Recording failed for device 1");
                            }
                            else
                                LogMessage("No record for device 1");
                        }
                        else
                            LogMessage("CleanupRecording failed for device 1");
                        
                        if (await speechClient2.Cleanup())
                        {
                            string speechAPI = ComboAPI.SelectedItem.ToString();
                            string language = speechToTextLanguage.SelectedItem.ToString();
                            string resultType = ComboAPIResult.SelectedItem.ToString();
                            string recordResult = await GetCurrentRecordingDeviceId2();
                            if (recordResult != null)
                            {
                                if (await speechClient2.StartRecording(speechAPI, language, resultType, recordResult))
                                {

                                    isRecordingContinuously = true;
                                    speechClient2.AudioLevel += Client_AudioLevel;
                                    speechClient2.AudioCaptureError += Client_AudioCaptureError;
                                    speechClient2.WebSocketEvent += Client_WebSocketEvent;
                                    LogMessage("Start Recording for device 2...");
                                    result = true;
                                }
                                else
                                    LogMessage("Start Recording failed for device 2");
                            }
                            else
                                LogMessage("No record for device 2");
                        }
                        else
                            LogMessage("CleanupRecording failed for device 2");
                            
                    }
                    else
                    {
                        LogMessage("Stop Recording for all devices...");
                        await speechClient1.StopRecording();
                        isRecordingContinuously = false;
                        speechClient1.AudioLevel -= Client_AudioLevel;
                        speechClient1.AudioCaptureError -= Client_AudioCaptureError;
                        speechClient1.WebSocketEvent -= Client_WebSocketEvent;
                        await speechClient2.StopRecording();
                        speechClient2.AudioLevel -= Client_AudioLevel;
                        speechClient2.AudioCaptureError -= Client_AudioCaptureError;
                        speechClient2.WebSocketEvent -= Client_WebSocketEvent;
                        ClearCanvas();
                    }
                }
                UpdateControls();
            }
            finally
            {
                bInProgress = false;
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            }
            return result;

        }

        string GetPhrase(string Body)
        {
            string result = string.Empty;
            if(!string.IsNullOrEmpty(Body))
            {
                int pos = Body.IndexOf("\"DisplayText\"");
                if(pos>0)
                {
                    pos = Body.IndexOf(":\"", pos + 13);
                    if (pos > 0)
                    {
                        int end = Body.IndexOf("\"", pos + 2);
                        if(end>0)
                            result = Body.Substring(pos + 2, end - pos - 2);
                    }
                }
            }
            return result;
        }
        string GetHypothesis(string Body)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(Body))
            {
                int pos = Body.IndexOf("\"Text\"");
                if (pos > 0)
                {
                    pos = Body.IndexOf(":\"", pos + 6);
                    if (pos > 0)
                    {
                        int end = Body.IndexOf("\"", pos + 2);
                        if (end > 0)
                            result = Body.Substring(pos + 2, end - pos - 2);
                    }
                }
            }
            return result;
        }
        private async void Client_WebSocketEvent(SpeechClient.SpeechClient speechClient, string Path, SpeechClient.SpeechToTextResponse response)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
           async () =>
           {
               string client = string.Empty;
               if (speechClient == speechClient1)
                   client = "Device 1";
               else
                   client = "Device 2";
               if (response != null)
                    LogMessage("Received WebSocket for " + client + ": " + Path + " Message: " + response.ToString());
               else
                    LogMessage("Received WebSocket for " + client + ": " + Path );
               switch (Path.ToLower())
               {
                   case "turn.start":
                       break;
                   case "speech.maxdurationreached":
                   case "turn.end":
                       speechClient.WebSocketEvent -= Client_WebSocketEvent;
                       if (isRecordingContinuously == true)
                           await LaunchContinuousRecording();
                       break;
                   case "speech.enddetected":
                       break;
                   case "speech.phrase":
                       if(speechClient == speechClient1)
                        AddResult1Phrase(response.Result());
                       else if(speechClient == speechClient2)
                        AddResult2Phrase(response.Result());
                       break;
                   case "speech.hypothesis":
                       if (speechClient == speechClient1)
                           AddResult1Hypothesis(response.Result());
                       else if(speechClient == speechClient2)
                           AddResult2Hypothesis(response.Result());
                       break;
                   case "speech.startdetected":
                       break;
                   case "speech.fragment":
                       break;
                   case "speech.websocketclosed":
                       break;
                   default:
                       break;
               }
           });


        }
        int LastPhasePosition1 = 0;
        int LastPhasePosition2 = 0;
        string ResultText1 = string.Empty;
        string ResultText2 = string.Empty;
        void ClearResult()
        {
            ResultText1 = "\r\nPhrase:\r\n";
            resultText1.Text = ResultText1;
            ResultText2 = "\r\nPhrase:\r\n";
            resultText2.Text = ResultText2;
            LastPhasePosition1 = ResultText1.Length;
            LastPhasePosition2 = ResultText2.Length;
        }
        void AddResult1Phrase(string phrase)
        {
            if (LastPhasePosition1 > 10000)
            {
                LastPhasePosition1 = 0;
                ResultText1 = string.Empty;
            }

            ResultText1 = ResultText1.Substring(0, LastPhasePosition1) + phrase + "\r\nPhrase:\r\n";
            LastPhasePosition1 = ResultText1.Length;
            resultText1.Text = ResultText1;
        }
        void AddResult2Phrase(string phrase)
        {
            if (LastPhasePosition2 > 10000)
            {
                LastPhasePosition2 = 0;
                ResultText2 = string.Empty;
            }
            ResultText2 = ResultText2.Substring(0, LastPhasePosition2)  + phrase + "\r\nPhrase:\r\n";
            LastPhasePosition2 = ResultText2.Length;
            resultText2.Text = ResultText2;
        }
        void AddResult1Hypothesis(string hypothesis)
        {

            ResultText1 = ResultText1.Substring(0, LastPhasePosition1) + hypothesis;
            resultText1.Text = ResultText1;
        }
        void AddResult2Hypothesis(string hypothesis)
        {

            ResultText2 = ResultText2.Substring(0, LastPhasePosition2) + hypothesis;
            resultText2.Text = ResultText2;
        }
        void AddResult1Error(string error)
        {
            LastPhasePosition1 = 0;
            resultText1.Text = error;
        }
        void AddResult2Error(string error)
        {
            LastPhasePosition2 = 0;
            resultText2.Text = error;
        }





        private void SubscriptionKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (speechClient1 != null)
                speechClient1.ClearToken();
            if (speechClient2 != null)
                speechClient2.ClearToken();
        }



        private void ComboHostname_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (speechClient1 != null)
                speechClient1.ClearToken();
            if (speechClient2 != null)
                speechClient2.ClearToken();
            //if (ComboHostname.SelectedItem.ToString() == defaultBingSpeechHostname)
            //{
            //    subscriptionKey.Text = valueBingSpeechSubscription;
            //    customEndpointID.Text = string.Empty;
            //}
            //else if (ComboHostname.SelectedItem.ToString() == defaultWestUSSpeechHostname)
            //{
            //    subscriptionKey.Text = valueWestUsSpeechSubscription;
            //    customEndpointID.Text = valueWestUsEndPointID;
            //}
            //else if (ComboHostname.SelectedItem.ToString() == defaultEastAsiaSpeechHostname)
            //{
            //    subscriptionKey.Text = valueEastAsiaSpeechSubscription;
            //    customEndpointID.Text = valueEastAsiaEndPointID;
            //}
            //else if (ComboHostname.SelectedItem.ToString() == defaultNorthEuropeSpeechHostname)
            //{
            //    subscriptionKey.Text = valueNorthEuropeSpeechSubscription;
            //    customEndpointID.Text = valueNorthEuropeEndPointID;
            //}
            UpdateControls();
        }

        private void SpeechApiType_Toggled(object sender, RoutedEventArgs e)
        {
            bUseWebSocket = SpeechApiType.IsOn;
            UpdateControls();
        }

        private void ComboAPI_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateControls();
        }

        #endregion ui
    }
}
