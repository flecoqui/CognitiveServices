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

namespace SpeechToTextUWPSampleApp
{
    /// <summary>
    /// Main page for the application.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Windows.Media.Playback.MediaPlayer mediaPlayer;
        SpeechClient.SpeechClient speechClient;
        string lastSubscriptionKey;
        string lastHostname;
        string lastCustomEndpointID;
        DateTime lastTokenDate;
        UInt16 level = 300;
        UInt16 duration = 1000;
        bool isRecordingInMemory = false;
        bool isRecordingInFile = false;
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
            resultText.TextChanged += ResultText_TextChanged;

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

            textToSpeechLanguage.Items.Clear();
            foreach (var l in TextToSpeechLanguageArray)
                textToSpeechLanguage.Items.Add(l);
            textToSpeechLanguage.SelectedItem = "en-US";

            gender.Items.Add("Female");
            gender.Items.Add("Male");
            gender.SelectedItem = "Female";
            // Get Subscription ID from the local settings
            ReadSettingsAndState();
            
            // Update control and play first video
            UpdateControls();
            memoryRecordingButton.Focus(FocusState.Programmatic);

            
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
            await CreateSpeechClient();

        }
        const string defaultDeviceLabel = "Default Device";
        private async System.Threading.Tasks.Task<bool> FillComboRecordingDevices()
        {
            ComboDevice.Items.Add(defaultDeviceLabel);
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            foreach (var d in allVideoDevices)
            {
                ComboDevice.Items.Add(d.Name);
            }
            if (ComboDevice.Items.Count > 0)
            {
                ComboDevice.SelectedIndex = 0;
                return true;
            }
            return false;
        }
        private async System.Threading.Tasks.Task<string> GetCurrentRecordingDeviceId()
        {
            string result = string.Empty;
            if (ComboDevice.Items.Count > 0)
            {
                
                string name = ComboDevice.SelectedItem as string;
                if (!string.IsNullOrEmpty(name))
                {
                    if (string.Equals(name, defaultDeviceLabel))
                        return string.Empty;
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
        async System.Threading.Tasks.Task<bool> CreateSpeechClient()
        {
            bool result = false;
            if ((speechClient != null) &&
                (string.Equals(lastSubscriptionKey, subscriptionKey.Text.ToString())) &&
                (string.Equals(lastHostname, ComboHostname.SelectedItem.ToString())) &&
                (string.Equals(lastCustomEndpointID, customEndpointID.Text)) &&
                ((DateTime.Now - lastTokenDate) < TimeSpan.FromSeconds(60 * 5))
                )
                return true;
            // Cognitive Service SpeechToText GetToken 
            if (!string.IsNullOrEmpty(subscriptionKey.Text))
            {
                if(speechClient != null)
                {
                    await speechClient.Cleanup();
                }
                if (!string.IsNullOrEmpty(customEndpointID.Text))
                {
                    LogMessage("Creating Custom Speech Client: " + ComboHostname.SelectedItem.ToString() + " Key: " + subscriptionKey.Text.ToString() + " EndpointID: " + customEndpointID.Text);
                    speechClient = await SpeechClient.SpeechClient.CreateCustomSpeechClient(ComboHostname.SelectedItem.ToString(), subscriptionKey.Text, customEndpointID.Text);
                }
                else
                {
                    if (string.Equals("speech.platform.bing.com", ComboHostname.SelectedItem.ToString().ToLower()))
                    {
                        LogMessage("Creating Bing Speech Client: " + ComboHostname.SelectedItem.ToString() + " Key: " + subscriptionKey.Text.ToString());
                        speechClient = await SpeechClient.SpeechClient.CreateBingSpeechClient(subscriptionKey.Text);
                    }
                    else
                    {
                        LogMessage("Creating Speech Client: " + ComboHostname.SelectedItem.ToString() + " Key: " + subscriptionKey.Text.ToString());
                        speechClient = await SpeechClient.SpeechClient.CreateSpeechClient(ComboHostname.SelectedItem.ToString(), subscriptionKey.Text);
                    }
                }
                if (speechClient != null)
                {
                    LogMessage("Speech Client successfully created");
                    string s = speechClient.Token;
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

            if ((speechClient != null) && (speechClient.IsRecording()))
            {
                LogMessage("Stop Recording...");
                await speechClient.StopRecording();
                speechClient.AudioLevel -= Client_AudioLevel;
                speechClient.AudioCaptureError -= Client_AudioCaptureError;
                speechClient.WebSocketEvent -= Client_WebSocketEvent;

                isRecordingInFile = false;
                isRecordingInMemory = false;
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


            //SaveSettingsValue(keyHostname, ComboHostname.SelectedItem.ToString());
            //if(ComboHostname.SelectedItem.ToString()== defaultBingSpeechHostname)
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
                valueSpeechHostname = s;
            else
                valueSpeechHostname = defaultBingSpeechHostname;
            ComboHostname.SelectedItem = valueSpeechHostname;

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
                valueSpeechSubscription = s;

            s = ReadSettingsValue(keySpeechEndPointID) as string;
            if (!string.IsNullOrEmpty(s))
                valueSpeechEndPointID = s;

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
        void ResultText_TextChanged(object sender, TextChangedEventArgs e)
        {
            //  logs.Focus(FocusState.Programmatic);
            // logs.Select(logs.Text.Length, 0);
            var tbsv = GetFirstDescendantScrollViewer(resultText);
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

        /// <summary>
        /// StartPlay
        /// Start to play pictures, audio content or video content
        /// </summary>
        /// <param name="content">Url string of the content to play </param>
        /// <returns>true if success</returns>
        private async System.Threading.Tasks.Task<bool> StartPlay(string content)
        {

            try
            {

                bool result = false;
                if (string.IsNullOrEmpty(content))
                {
                    LogMessage("Empty Uri");
                    return result;
                }

                // Stop the current stream
                mediaPlayer.Source = null;
                mediaPlayerElement.PosterSource = null;
                mediaPlayer.AutoPlay = true;
                // if a picture will be displayed
                // display or not popup
                if (result == true)
                {
                    mediaPlayerElement.Visibility = Visibility.Collapsed;
                }
                else
                {
                    mediaPlayerElement.Visibility = Visibility.Visible;
                }
                // Audio or video
                Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(content);
                if (file != null)
                {
                    mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);
                    mediaPlayer.Play();
                    return true;
                }
                else
                    LogMessage("Failed to load media file: " + Content);
            }
            catch (Exception ex)
            {
                LogMessage("Exception Playing: " + ex.Message.ToString());
            }
            return false;
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

        /// <summary>
        /// This method prepare the MediaElement to play any content (video, audio, pictures): SMOOTH, DASH, HLS, MP4, WMV, MPEG2-TS, JPG, PNG,...
        /// </summary>
        private async void PlayCurrentUrl()
        {

            await StartPlay(mediaUri.Text);
            UpdateControls();
        }

        /// <summary>
        /// Play method which plays the video with the MediaElement from position 0
        /// </summary>
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("Start to play " + mediaUri.Text.ToString());
                PlayCurrentUrl();
            }
            catch (Exception ex)
            {
                LogMessage("Failed to play: " + mediaUri.Text + " Exception: " + ex.Message);
            }
        }

        /// <summary>
        /// TextToSpeech method which use TextToSpeech Cognitive Swervices 
        /// </summary>
        private async void TextToSpeech_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("Sending text to Cognitive Services " + resultText.Text.ToString());
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);
                await CreateSpeechClient();
                if (speechClient != null)
                {
                    valueSpeechHostname = ComboHostname.SelectedItem.ToString();
                    valueSpeechSubscription = subscriptionKey.Text;
                    valueSpeechEndPointID = customEndpointID.Text;
                    // Save subscription key
                    SaveSettingsAndState();

                    string locale = textToSpeechLanguage.SelectedItem.ToString();
                    string genderString = gender.SelectedItem.ToString();
                        
                    LogMessage("Sending text to TextToSpeech servcvice for language : " +locale);
                    Windows.Storage.Streams.IInputStream stream = await speechClient.TextToSpeech(resultText.Text, locale, genderString);
                    if (stream != null)
                    {
                        LogMessage("Playing the audio stream ");
                        //stream.ReadAsync(
                        MemoryStream localStream = new MemoryStream();
                        await stream.AsStreamForRead().CopyToAsync(localStream);
                        // Stop the current stream
                        mediaPlayer.Source = null;
                        mediaPlayerElement.PosterSource = null;
                        mediaPlayer.AutoPlay = true;
                        // if a picture will be displayed
                        // display or not popup
                        // Audio or video
                        mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStream(localStream.AsRandomAccessStream(), "audio/x-wav");
                        mediaPlayer.Play();
                    }
                    else
                        LogMessage("Error while reading speech buffer");

                }
                else
                    LogMessage("Authentication failed check your subscription Key: " + subscriptionKey.Text.ToString());
                UpdateControls();
            }
            catch (Exception ex)
            {
                LogMessage("Failed to convert Text to Speech: " + resultText.Text + " Exception: " + ex.Message);
            }
        
            finally
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            }

        }
        /// <summary>
        /// Stop method which stops the video currently played by the MediaElement
        /// </summary>
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(mediaUri.Text)) 
                {
                    LogMessage("Stop " + mediaUri.Text.ToString());
                    //          mediaPlayer.Stop();
                    mediaPlayer.Source = null;
                    UpdateControls();

                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to stop: " + mediaUri.Text + " Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Play method which plays the video currently paused by the MediaElement
        /// </summary>
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(mediaUri.Text))
                {
                    LogMessage("Play " + mediaUri.Text.ToString());
                    mediaPlayer.Play();
                    UpdateControls();

                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to play: " + mediaUri.Text + " Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// Pause method which pauses the video currently played by the MediaElement
        /// </summary>
        private void PausePlay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(mediaUri.Text))
                {
                    LogMessage("Pause " + mediaUri.Text.ToString());
                    mediaPlayer.Pause();
                    UpdateControls();
                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to play: " + mediaUri.Text + " Exception: " + ex.Message);
            }
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
                        DrawLevel(value,Windows.UI.Colors.Cyan);
                    });
            }
        }
        void DrawLevel(double value, Windows.UI.Color cr )
        {
            if ((bDrawingMessage == true)&&(cr==Windows.UI.Colors.Cyan))
                return;
            if (CanvasGraph.Children.Count > 0) CanvasGraph.Children.Clear();
            Windows.UI.Xaml.Shapes.Line line = new Windows.UI.Xaml.Shapes.Line() { X1 = 0, Y1 = CanvasGraph.Height / 2, X2 = ((value * CanvasGraph.Width) / maxValue), Y2 = CanvasGraph.Height / 2 };
            line.StrokeThickness = CanvasGraph.Height;
            line.Stroke = new SolidColorBrush(cr);
            CanvasGraph.Children.Add(line);
        }
        void DrawError()
        {
            bDrawingMessage = true;
            DrawLevel(maxValue, Windows.UI.Colors.Red);
            var t = System.Threading.Tasks.Task.Run(async delegate
            {
                await System.Threading.Tasks.Task.Delay(2000);
                ClearCanvas();
            });
        }
        void DrawOk()
        {
            bDrawingMessage = true;
            DrawLevel(maxValue, Windows.UI.Colors.GreenYellow);
            var t = System.Threading.Tasks.Task.Run(async delegate
            {
                await System.Threading.Tasks.Task.Delay(2000);
                ClearCanvas();
            });
        }
        void ClearCanvas()
        {
            bDrawingMessage = false;
            if (CanvasGraph.Children.Count > 0) CanvasGraph.Children.Clear();
        }
        private async void Client_AudioCaptureError(object sender, string message)
        {
            LogMessage("Audio Capture Error: " + message );
            LogMessage("Stop Recording...");
            await speechClient.StopRecording();
            isRecordingInMemory = false;
            speechClient.AudioLevel -= Client_AudioLevel;
            speechClient.AudioCaptureError -= Client_AudioCaptureError;
            speechClient.WebSocketEvent -= Client_WebSocketEvent;
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
                         textToSpeechButton.IsEnabled = true;
                         // If WebSocket API conversation enabled
                         if(SpeechApiType.IsOn == true)
                         {
                             if (ComboAPI.Items.Count != 3)
                             {
                                 string oldSelction = ComboAPI.SelectedItem.ToString();
                                 // Fill Combobox API
                                 ComboAPI.Items.Clear();
                                 ComboAPI.Items.Add("interactive");
                                 ComboAPI.Items.Add("conversation");
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
                             textToSpeechButton.IsEnabled = true;
                             textToSpeechLanguage.IsEnabled = true;
                             gender.IsEnabled = true;
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
                                 textToSpeechButton.IsEnabled = false;
                                 textToSpeechLanguage.IsEnabled = false;
                                 gender.IsEnabled = false;

                             }
                             else
                             {
                                 // New Speech Service
                                 speechToTextLanguage.IsEnabled = true;
                                 textToSpeechButton.IsEnabled = true;
                                 textToSpeechLanguage.IsEnabled = true;
                                 gender.IsEnabled = true;
                             }
                         }

                         if ((speechClient == null) || (!speechClient.IsRecording()))
                         {
                             memoryRecordingButton.IsEnabled = true;
                             fileRecordingButton.IsEnabled = true;
                             if ((bUseWebSocket) && (ComboAPI.SelectedItem.ToString() == "conversation"))
                                 continuousRecordingButton.IsEnabled = true;
                             else
                                 continuousRecordingButton.IsEnabled = false;

                             memoryRecordingButton.Content = "\xE717";
                             fileRecordingButton.Content = "\xE720";
                             continuousRecordingButton.Content = "\xE895";
                         }
                         else
                         {
                             if (isRecordingInMemory == true)
                                 memoryRecordingButton.IsEnabled = true;
                             else
                                 memoryRecordingButton.IsEnabled = false;
                             if (isRecordingInFile == true)
                                 fileRecordingButton.IsEnabled = true;
                             else
                                 fileRecordingButton.IsEnabled = false;
                             if (isRecordingContinuously == true)
                             { 
                                 if ((bUseWebSocket) && (ComboAPI.SelectedItem.ToString() == "conversation"))
                                     continuousRecordingButton.IsEnabled = true;
                                 else
                                     continuousRecordingButton.IsEnabled = false;
                             }
                             else
                                 continuousRecordingButton.IsEnabled = false;

                             memoryRecordingButton.Content = "\xE778";
                             fileRecordingButton.Content = "\xE78C";
                             continuousRecordingButton.Content = "\xE8D8";
                         }
                         openButton.IsEnabled = true;
                         mediaUri.IsEnabled = true;

                         if (!string.IsNullOrEmpty(mediaUri.Text))
                         {
                             convertWAVButton.IsEnabled = true;

                             playButton.IsEnabled = true;


                             muteButton.IsEnabled = true;
                             volumeDownButton.IsEnabled = true;
                             volumeUpButton.IsEnabled = true;

                             playPauseButton.IsEnabled = false;
                             pausePlayButton.IsEnabled = false;
                             stopButton.IsEnabled = false;


                             if (mediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Playing)
                             {
                                 //if (string.Equals(mediaUri.Text, CurrentMediaUrl))
                                 //{
                                 playPauseButton.IsEnabled = false;
                                 pausePlayButton.IsEnabled = true;
                                 stopButton.IsEnabled = true;
                                 //}
                             }
                             else if (mediaPlayer.PlaybackSession.PlaybackState == Windows.Media.Playback.MediaPlaybackState.Paused)
                             {
                                 playPauseButton.IsEnabled = true;
                                 stopButton.IsEnabled = true;
                             }
                         }
                         // Volume buttons control
                         if (mediaPlayer.IsMuted)
                             muteButton.Content = "\xE767";
                         else
                             muteButton.Content = "\xE74F";
                         if (mediaPlayer.Volume == 0)
                         {
                             volumeDownButton.IsEnabled = false;
                             volumeUpButton.IsEnabled = true;
                         }
                         else if (mediaPlayer.Volume >= 1)
                         {
                             volumeDownButton.IsEnabled = true;
                             volumeUpButton.IsEnabled = false;
                         }
                         else
                         {
                             volumeDownButton.IsEnabled = true;
                             volumeUpButton.IsEnabled = true;
                         }
                     }
                 });
        }
        bool bInProgress = false;
        /// <summary>
        /// sendAudioBuffer method which :
        /// - record audio sample in the buffer
        /// - send the buffer to SpeechToText REST API once the recording is done
        /// </summary>
        private async void MemoryRecording_Click(object sender, RoutedEventArgs e)
        {
            if (bInProgress == true)
                return;
            bInProgress = true;
            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);
                await CreateSpeechClient();
                if (speechClient != null)
                {
                    valueSpeechHostname = ComboHostname.SelectedItem.ToString();
                    valueSpeechSubscription = subscriptionKey.Text;
                    valueSpeechEndPointID = customEndpointID.Text;

                    SaveSettingsAndState();
                    if (speechClient.IsRecording() == false)
                    {
                        if (await speechClient.Cleanup())
                        {
                            if (await speechClient.StartRecordingInMemory(await GetCurrentRecordingDeviceId()))
                            {
                                ClearResult();
                                isRecordingInMemory = true;
                                speechClient.AudioLevel += Client_AudioLevel;
                                speechClient.AudioCaptureError += Client_AudioCaptureError;
                                LogMessage("Start Recording...");
                            }
                            else
                                LogMessage("Start Recording failed");
                        }
                        else
                            LogMessage("CleanupRecording failed");
                    }
                    else
                    {
                        LogMessage("Stop Recording...");
                        await speechClient.StopRecording();
                        isRecordingInMemory = false;
                        speechClient.AudioLevel -= Client_AudioLevel;
                        speechClient.AudioCaptureError -= Client_AudioCaptureError;
                        speechClient.WebSocketEvent -= Client_WebSocketEvent;

                        ClearCanvas();
                        string locale = speechToTextLanguage.SelectedItem.ToString();
                        string resulttype = ComboAPIResult.SelectedItem.ToString();
                        string speechAPI = ComboAPI.SelectedItem.ToString();
                        if (bUseWebSocket == true)
                        {
                            LogMessage("Sending Memory Buffer over WebSocket...");
                            speechClient.WebSocketEvent += Client_WebSocketEvent;
                            bool result = await speechClient.SendMemoryBufferOverWebSocket(speechAPI, locale, resulttype);
                            if (result == true)
                                LogMessage("Sending Memory Buffer over WebSocket successful");
                            else
                                LogMessage("Error while sending buffer");
                        }
                        else
                        {
                            LogMessage("Sending Memory Buffer over REST API...");
                            SpeechClient.SpeechToTextResponse result = await speechClient.SendMemoryBuffer(speechAPI, locale, resulttype);
                            if (result != null)
                            {
                                string httpError = result.GetHttpError();
                                if (!string.IsNullOrEmpty(httpError))
                                {
                                    AddResultError(httpError);
                                    LogMessage("Http Error: " + httpError.ToString());
                                }
                                else
                                {
                                    if (result.Status() == "error")
                                        AddResultError("error");
                                    else
                                        AddResultPhrase(result.Result());
                                    LogMessage("Result: " + result.ToString());
                                }
                            }
                            else
                                LogMessage("Error while sending buffer");

                        }
                    }

                }
                UpdateControls();
            }
            finally
            {
                bInProgress = false;
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            }

        }
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

                await CreateSpeechClient();
                if (speechClient != null)
                {
                    valueSpeechHostname = ComboHostname.SelectedItem.ToString();
                    valueSpeechSubscription = subscriptionKey.Text;
                    valueSpeechEndPointID = customEndpointID.Text;

                    SaveSettingsAndState();
                    if (speechClient.IsRecording() == false)
                    {
                        ClearCanvas();
                        if (await speechClient.Cleanup())
                        {
                            string speechAPI = ComboAPI.SelectedItem.ToString();
                            string language = speechToTextLanguage.SelectedItem.ToString();
                            string resultType = ComboAPIResult.SelectedItem.ToString();
                            if (await speechClient.StartRecording(speechAPI, language, resultType, await GetCurrentRecordingDeviceId()))
                            {

                                isRecordingContinuously = true;
                                speechClient.AudioLevel += Client_AudioLevel;
                                speechClient.AudioCaptureError += Client_AudioCaptureError;
                                speechClient.WebSocketEvent += Client_WebSocketEvent;
                                LogMessage("Start Recording...");
                                result = true;
                            }
                            else
                                LogMessage("Start Recording failed");
                        }
                        else
                            LogMessage("CleanupRecording failed");
                    }
                    else
                    {
                        LogMessage("Stop Recording...");
                        await speechClient.StopRecording();
                        isRecordingContinuously = false;
                        speechClient.AudioLevel -= Client_AudioLevel;
                        speechClient.AudioCaptureError -= Client_AudioCaptureError;
                        speechClient.WebSocketEvent -= Client_WebSocketEvent;
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
        private async void Client_WebSocketEvent(SpeechClient.SpeechClient sender, string Path, SpeechClient.SpeechToTextResponse response)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
           async () =>
           {
               if(response != null)
                    LogMessage("Received WebSocket: " + Path + " Message: " + response.ToString());
               else
                    LogMessage("Received WebSocket: " + Path );
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
                       AddResultPhrase(response.Result());
                       break;
                   case "speech.hypothesis":
                        AddResultHypothesis(response.Result());

                       break;
                   case "speech.startdetected":
                       break;
                   case "speech.fragment":
                       break;
                   default:
                       break;
               }
           });


        }
        int LastPhasePosition = 0;
        string ResultText = string.Empty;
        void ClearResult()
        {
            ResultText = "\r\nPhrase:\r\n";
            resultText.Text = ResultText;
            LastPhasePosition = ResultText.Length;
        }
        void AddResultPhrase(string phrase)
        {
            if (LastPhasePosition > 10000)
            {
                LastPhasePosition = 0;
                ResultText = string.Empty;
            }
            ResultText = ResultText.Substring(0, LastPhasePosition) + phrase + "\r\nPhrase:\r\n";
            LastPhasePosition = ResultText.Length;
            resultText.Text = ResultText;
        }
        void AddResultHypothesis(string hypothesis)
        {

            ResultText = ResultText.Substring(0, LastPhasePosition) + hypothesis;
            resultText.Text = ResultText;
        }
        void AddResultError(string error)
        {
            LastPhasePosition = 0;
            resultText.Text = error;
        }


        /// <summary>
        /// recordAudio method which :
        /// - record audio sample in the buffer
        /// - store the buffer in a storagefile on disk once the recording is done
        /// </summary>
        private async void FileRecording_Click(object sender, RoutedEventArgs e)
        {
            if (bInProgress == true)
                return;
            bInProgress = true;
            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);
                await CreateSpeechClient();
                if (speechClient != null)
                {
                    valueSpeechHostname = ComboHostname.SelectedItem.ToString();
                    valueSpeechSubscription = subscriptionKey.Text;
                    valueSpeechEndPointID = customEndpointID.Text;

                    SaveSettingsAndState();
                    if (speechClient.IsRecording() == false)
                    {
                        if (await speechClient.Cleanup())
                        {
                            if (await speechClient.StartRecordingInMemory(await GetCurrentRecordingDeviceId()))
                            {
                                isRecordingInFile = true;
                                speechClient.AudioLevel += Client_AudioLevel;
                                speechClient.AudioCaptureError += Client_AudioCaptureError;
                                LogMessage("Start Recording in memory...");
                            }
                            else
                                LogMessage("Start Recording failed");
                        }
                        else
                            LogMessage("CleanupRecording failed");
                    }
                    else
                    {
                        LogMessage("Stop Recording...");
                        await speechClient.StopRecording();
                        isRecordingInFile = false;
                        speechClient.AudioLevel -= Client_AudioLevel;
                        speechClient.AudioCaptureError -= Client_AudioCaptureError;
                        speechClient.WebSocketEvent -= Client_WebSocketEvent;
                        ClearCanvas();
                        if (speechClient.GetBufferLength() > 0)
                        {


                            var filePicker = new Windows.Storage.Pickers.FileSavePicker()
                            {
                                DefaultFileExtension = ".wav",
                                SuggestedFileName = "record.wav",
                                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary,
                                SettingsIdentifier = "WavPicker",
                                CommitButtonText = "Save buffer into a WAV File"
                            };
                            filePicker.FileTypeChoices.Add("WAV files", new List<string>() { ".wav" });
                            var wavFile = await filePicker.PickSaveFileAsync();
                            if (wavFile != null)
                            {
                                string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(wavFile);
                                if (await speechClient.SaveMemoryBuffer(wavFile))
                                {
                                    mediaUri.Text = "file://" + wavFile.Path;
                                    LogMessage("Record buffer saved in file: " + wavFile.Path.ToString());
                                    UpdateControls();
                                }
                                else
                                    LogMessage("Error while saving record buffer in file: " + wavFile.Path.ToString());
                            }
                        }
                        else
                            LogMessage("Buffer empty nothing to save");
                    }
                }

                UpdateControls();
            }
            finally
            {
                bInProgress = false;
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            }
        }



        /// <summary>
        /// open method which select a WAV file on disk
        /// </summary>
        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
                filePicker.FileTypeFilter.Add(".wav");
                filePicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                filePicker.SettingsIdentifier = "WavPicker";
                filePicker.CommitButtonText = "Open WAV File to Process";

                var wavFile = await filePicker.PickSingleFileAsync();
                if (wavFile != null)
                {
                    string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(wavFile);
                    mediaUri.Text = "file://" + wavFile.Path;
                    LogMessage("Selected file: " + mediaUri.Text);
                    UpdateControls();
                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to select WAV file: Exception: " + ex.Message);
            }
        }
        /// <summary>
        /// sendWAVFile method which :
        /// - sends the audio sample in a WAV file towards the SpeechToText REST API
        /// </summary>
        private async void SendWAVFile_Click(object sender, RoutedEventArgs e)
        {
            if (bInProgress == true)
                return;
            bInProgress = true;
            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);
                await CreateSpeechClient();
                if (speechClient != null)
                {
                    valueSpeechHostname = ComboHostname.SelectedItem.ToString();
                    valueSpeechSubscription = subscriptionKey.Text;
                    valueSpeechEndPointID = customEndpointID.Text;

                    SaveSettingsAndState();
                    string locale = speechToTextLanguage.SelectedItem.ToString();
                    string resulttype = ComboAPIResult.SelectedItem.ToString();
                    string apiType = ComboAPI.SelectedItem.ToString();
                    var file = await GetFileFromLocalPathUrl(mediaUri.Text);
                    if (file != null)
                    {
                        string convertedText = string.Empty;
                        ClearResult();
                        if (bUseWebSocket)
                        {
                            LogMessage("Sending StorageFile over WebSocket: " + file.Path.ToString());
                            speechClient.WebSocketEvent += Client_WebSocketEvent;
                            bool result = await speechClient.SendStorageFileOverWebSocket(file, ComboAPI.SelectedItem.ToString(), locale, resulttype);
                        }
                        else
                        {
                            SpeechClient.SpeechToTextResponse result = await speechClient.SendStorageFile(file, ComboAPI.SelectedItem.ToString(),locale, resulttype);
                            if (result != null)
                            {
                                string httpError = result.GetHttpError();
                                if (!string.IsNullOrEmpty(httpError))
                                {
                                    AddResultError(httpError);
                                    LogMessage("Http Error: " + httpError.ToString());
                                }
                                else
                                {
                                    if (result.Status() == "error")
                                        AddResultError("error");
                                    else
                                        AddResultPhrase(result.Result());
                                    LogMessage("Result: " + result.ToString());
                                }
                            }
                            else
                                LogMessage("Error while sending file");
                        }
                        

                    }
                }
            }
            finally
            {
                bInProgress = false;
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            }

        }



        private void SubscriptionKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (speechClient != null)
                speechClient.ClearToken();
        }



        private void ComboHostname_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (speechClient != null)
                speechClient.ClearToken();
            if (ComboHostname.SelectedItem.ToString() == valueSpeechHostname)
            {
                subscriptionKey.Text = valueSpeechSubscription;
                customEndpointID.Text = valueSpeechEndPointID;
            }
            else
            {
                subscriptionKey.Text = string.Empty;
                customEndpointID.Text = string.Empty;
            }
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
