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
using SpeechToTextClient;

namespace SpeechToTextUWPSampleApp
{
    /// <summary>
    /// Main page for the application.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Windows.Media.Playback.MediaPlayer mediaPlayer;
        SpeechToTextClient.SpeechToTextClient client;
        ulong maxSize = 3840000;
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

            // Bind player to element
            mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            mediaPlayerElement.SetMediaPlayer(mediaPlayer);
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;

            // Fill ComboBox Hostname
            ComboHostname.Items.Clear();
            ComboHostname.Items.Add(defaultBingSpeechHostname);
            ComboHostname.Items.Add(defaultWestUSSpeechHostname);
            ComboHostname.Items.Add(defaultEastAsiaSpeechHostname);
            ComboHostname.Items.Add(defaultNorthEuropeSpeechHostname);
            ComboHostname.SelectedIndex = 0;

            // Fill Combobox API
            ComboAPI.Items.Clear();
            ComboAPI.Items.Add("interactive");
            ComboAPI.Items.Add("conversation");
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
            LogMessage(SpeechToTextClient.SystemInformation.GetString());
            
            // Create Cognitive Service SpeechToText Client
            client = new SpeechToTextClient.SpeechToTextClient();

            // Initialize level and duration
            Level.Text = level.ToString();
            Duration.Text = duration.ToString();
            Level.TextChanged += Level_TextChanged;
            Duration.TextChanged += Duration_TextChanged;


            // Cognitive Service SpeechToText GetToken 
            if (!string.IsNullOrEmpty(subscriptionKey.Text))
            {
                LogMessage("Getting Token for subscription key: " + subscriptionKey.Text.ToString());
                client.SetAPI(ComboHostname.SelectedItem.ToString(), customEndpointID.Text, (string)ComboAPI.SelectedItem, bUseWebSocket);
                string s = await client.GetToken(subscriptionKey.Text);
                if (!string.IsNullOrEmpty(s))
                    LogMessage("Getting Token successful Token: " + s.ToString());
                else
                    LogMessage("Getting Token failed for subscription Key: " + subscriptionKey.Text);
            }

        }
        /// <summary>
        /// This method is called when the EndpointID is changed
        /// </summary>
        private void customEndpointID_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateControls();
        }
        /// <summary>
        /// This method is called when the Duration TextBox changed  
        /// </summary>
        private void Duration_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                uint n;
                if (!uint.TryParse(tb.Text, out n))
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
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                uint n;
                if (!uint.TryParse(tb.Text, out n))
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

            // if the application was continously recording
            // restart recording
            if (isRecordingContinuously == true)
                ContinuousRecording_Click(null, null);

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
            SaveSettingsAndState();
            if (client.IsRecording())
            {
                LogMessage("Stop Recording...");
                await client.StopRecording();
                isRecordingInFile = false;
                isRecordingInMemory = false;
                isRecordingContinuously = false;
            }
            deferal.Complete();
        }

        #region Settings
        const string keyHostname = "hostnameKey";
        const string keyBingSpeechSubscription = "bingSpeechSubscriptionKey";
        const string keyWestUsSpeechSubscription = "westUsSpeechSubscriptionKey";
        const string keyEastAsiaSpeechSubscription = "eastAsiaSpeechSubscriptionKey";
        const string keyNorthEuropeSpeechSubscription = "northEuropeSpeechSubscriptionKey";
        const string keyWestUsEndPointID = "westUsEndPointIDKey";
        const string keyEastAsiaEndPointID  = "eastAsiaEndPointIDKey";
        const string keyNorthEuropeEndPointID = "northEuropeEndPointIDKey";
        const string keyLevel = "levelKey";
        const string keyDuration = "durationKey";
        const string keyWebSocket = "webSocketKey";
        const string keyIsRecordingContinuously = "isRecordingContinuouslyKey";
        const string defaultBingSpeechHostname = "speech.platform.bing.com";
        const string defaultWestUSSpeechHostname = "westus.stt.speech.microsoft.com";
        const string defaultEastAsiaSpeechHostname = "eastasia.stt.speech.microsoft.com";
        const string defaultNorthEuropeSpeechHostname = "northeurope.stt.speech.microsoft.com";

        string valueBingSpeechSubscription = string.Empty;
        string valueWestUsSpeechSubscription = string.Empty;
        string valueEastAsiaSpeechSubscription = string.Empty;
        string valueNorthEuropeSpeechSubscription = string.Empty;
        string valueWestUsEndPointID = string.Empty;
        string valueEastAsiaEndPointID = string.Empty;
        string valueNorthEuropeEndPointID = string.Empty;
        /// <summary>
        /// Function to save all the persistent attributes
        /// </summary>
        public bool SaveSettingsAndState()
        {
            SaveSettingsValue(keyHostname, ComboHostname.SelectedItem.ToString());
            if(ComboHostname.SelectedItem.ToString()== defaultBingSpeechHostname)
            {
                valueBingSpeechSubscription = subscriptionKey.Text;

            }
            else if (ComboHostname.SelectedItem.ToString() == defaultWestUSSpeechHostname)
            {
                valueWestUsSpeechSubscription = subscriptionKey.Text;
                valueWestUsEndPointID = customEndpointID.Text;
            }
            else if (ComboHostname.SelectedItem.ToString() == defaultEastAsiaSpeechHostname)
            {
                valueEastAsiaSpeechSubscription = subscriptionKey.Text;
                valueEastAsiaEndPointID = customEndpointID.Text;
            }
            else if (ComboHostname.SelectedItem.ToString() == defaultNorthEuropeSpeechHostname)
            {
                valueNorthEuropeSpeechSubscription = subscriptionKey.Text;
                valueNorthEuropeEndPointID = customEndpointID.Text;
            }
            SaveSettingsValue(keyBingSpeechSubscription, valueBingSpeechSubscription);
            SaveSettingsValue(keyWestUsSpeechSubscription, valueWestUsSpeechSubscription);
            SaveSettingsValue(keyWestUsEndPointID, valueWestUsEndPointID);
            SaveSettingsValue(keyEastAsiaSpeechSubscription, valueEastAsiaSpeechSubscription);
            SaveSettingsValue(keyEastAsiaEndPointID, valueEastAsiaEndPointID);
            SaveSettingsValue(keyNorthEuropeSpeechSubscription, valueNorthEuropeSpeechSubscription);
            SaveSettingsValue(keyNorthEuropeEndPointID, valueNorthEuropeEndPointID);

            bUseWebSocket = SpeechApiType.IsOn;
            SaveSettingsValue(keyWebSocket, bUseWebSocket.ToString());
            SaveSettingsValue(keyLevel, level.ToString());
            SaveSettingsValue(keyDuration, duration.ToString());
            SaveSettingsValue(keyIsRecordingContinuously,isRecordingContinuously.ToString());
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

            valueBingSpeechSubscription = ReadSettingsValue(keyBingSpeechSubscription) as string;
            if (valueBingSpeechSubscription ==null) valueBingSpeechSubscription = string.Empty;

            valueWestUsSpeechSubscription = ReadSettingsValue(keyWestUsSpeechSubscription) as string;
            if (valueWestUsSpeechSubscription == null) valueWestUsSpeechSubscription = string.Empty;

            valueEastAsiaSpeechSubscription = ReadSettingsValue(keyEastAsiaSpeechSubscription) as string;
            if (valueEastAsiaSpeechSubscription == null) valueEastAsiaSpeechSubscription = string.Empty;

            valueNorthEuropeSpeechSubscription = ReadSettingsValue(keyNorthEuropeSpeechSubscription) as string;
            if (valueNorthEuropeSpeechSubscription == null) valueNorthEuropeSpeechSubscription = string.Empty;

            valueWestUsEndPointID = ReadSettingsValue(keyWestUsEndPointID) as string;
            if (valueWestUsEndPointID == null) valueWestUsEndPointID = string.Empty;

            valueEastAsiaEndPointID = ReadSettingsValue(keyEastAsiaEndPointID) as string;
            if (valueEastAsiaEndPointID == null) valueEastAsiaEndPointID = string.Empty;

            valueNorthEuropeEndPointID = ReadSettingsValue(keyNorthEuropeEndPointID) as string;
            if (valueNorthEuropeEndPointID == null) valueNorthEuropeEndPointID = string.Empty;

            if (ComboHostname.SelectedItem.ToString() == defaultBingSpeechHostname)
            {
                subscriptionKey.Text = valueBingSpeechSubscription;
                customEndpointID.Text = string.Empty;
            }
            else if (ComboHostname.SelectedItem.ToString() == defaultWestUSSpeechHostname)
            {
                subscriptionKey.Text = valueWestUsSpeechSubscription;
                customEndpointID.Text = valueWestUsEndPointID;
            }
            else if (ComboHostname.SelectedItem.ToString() == defaultEastAsiaSpeechHostname)
            {
                subscriptionKey.Text = valueEastAsiaSpeechSubscription;
                customEndpointID.Text = valueEastAsiaEndPointID;
            }
            else if (ComboHostname.SelectedItem.ToString() == defaultNorthEuropeSpeechHostname)
            {
                subscriptionKey.Text = valueNorthEuropeSpeechSubscription;
                customEndpointID.Text = valueNorthEuropeEndPointID;
            }

            s = ReadSettingsValue(keyLevel) as string;
            if (!string.IsNullOrEmpty(s))
                UInt16.TryParse(s, out level);
            s = ReadSettingsValue(keyDuration) as string;
            if (!string.IsNullOrEmpty(s))
                UInt16.TryParse(s, out duration);
            s = ReadSettingsValue(keyIsRecordingContinuously) as string;
            if (!string.IsNullOrEmpty(s))
                bool.TryParse(s, out isRecordingContinuously);
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
            App app = Windows.UI.Xaml.Application.Current as App;
            if (app != null)
                app.MessageList.Enqueue(Message);
        }
        bool PopMessage(out string Message)
        {
            Message = string.Empty;
            App app = Windows.UI.Xaml.Application.Current as App;
            if (app != null)
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

                    string result;
                    while (PopMessage(out result))
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
        /// Retrieve the ScrollViewer associated with a control  
        /// </summary>
        ScrollViewer GetFirstDescendantScrollViewer(DependencyObject parent)
        {
            var c = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < c; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var sv = child as ScrollViewer;
                if (sv != null)
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
        private void mute_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Toggle Mute");
            mediaPlayer.IsMuted = !mediaPlayer.IsMuted;
            UpdateControls();
        }
        /// <summary>
        /// Volume Up method 
        /// </summary>
        private void volumeUp_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Volume Up");
            mediaPlayer.Volume = (mediaPlayer.Volume + 0.10 <= 1 ? mediaPlayer.Volume + 0.10 : 1);
            UpdateControls();
        }
        /// <summary>
        /// Volume Down method 
        /// </summary>
        private void volumeDown_Click(object sender, RoutedEventArgs e)
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
        private void play_Click(object sender, RoutedEventArgs e)
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
        private async void textToSpeech_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogMessage("Sending text to Cognitive Services " + resultText.Text.ToString());
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);

                if (client != null)
                {
                    client.SetAPI(ComboHostname.SelectedItem.ToString(), customEndpointID.Text, (string)ComboAPI.SelectedItem, bUseWebSocket);
                    if ((!client.HasToken()) && (!string.IsNullOrEmpty(subscriptionKey.Text)))
                    {
                        LogMessage("Getting Token for subscription key: " + subscriptionKey.Text.ToString());
                        string token = await client.GetToken(subscriptionKey.Text);
                        if (!string.IsNullOrEmpty(token))
                        {
                            LogMessage("Getting Token successful Token: " + token.ToString());
                            // Save subscription key
                            SaveSettingsAndState();
                        }
                    }
                    if (client.HasToken())
                    {

                        string locale = textToSpeechLanguage.SelectedItem.ToString();
                        string genderString = gender.SelectedItem.ToString();
                        
                        LogMessage("Sending text to TextToSpeech servcvice for language : " +locale);
                        Windows.Storage.Streams.IInputStream stream = await client.TextToSpeech(resultText.Text, locale, genderString);
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
                            LogMessage("Error while readding speech buffer");

                    }
                    else
                        LogMessage("Authentication failed check your subscription Key: " + subscriptionKey.Text.ToString());
                }
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
        private void stop_Click(object sender, RoutedEventArgs e)
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
        private void playPause_Click(object sender, RoutedEventArgs e)
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
        private void pausePlay_Click(object sender, RoutedEventArgs e)
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
            await client.StopRecording();
            isRecordingInMemory = false;
            client.AudioLevel -= Client_AudioLevel;
            client.AudioCaptureError -= Client_AudioCaptureError;
            ClearCanvas();
            UpdateControls();
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

                         if ((client == null) || (!client.IsRecording()))
                         {
                             memoryRecordingButton.IsEnabled = true;
                             fileRecordingButton.IsEnabled = true;
                             continuousRecordingButton.IsEnabled = true;

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
                                 continuousRecordingButton.IsEnabled = true;
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

                if (client != null)
                {
                    client.SetAPI(ComboHostname.SelectedItem.ToString(), customEndpointID.Text, (string)ComboAPI.SelectedItem, bUseWebSocket);
                    if ((!client.HasToken()) && (!string.IsNullOrEmpty(subscriptionKey.Text)))
                    {
                        LogMessage("Getting Token for subscription key: " + subscriptionKey.Text.ToString());
                        string token = await client.GetToken(subscriptionKey.Text);
                        if (!string.IsNullOrEmpty(token))
                        {
                            LogMessage("Getting Token successful Token: " + token.ToString());
                            // Save subscription key
                            SaveSettingsAndState();
                        }
                    }
                    if (client.HasToken())
                    {
                        if (client.IsRecording() == false)
                        {
                            if (await client.CleanupRecording())
                            {
                                if (await client.StartRecording(0))
                                {
                                    isRecordingInMemory = true;
                                    client.AudioLevel += Client_AudioLevel;
                                    client.AudioCaptureError += Client_AudioCaptureError;
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
                            await client.StopRecording();
                            isRecordingInMemory = false;
                            client.AudioLevel -= Client_AudioLevel;
                            client.AudioCaptureError -= Client_AudioCaptureError;
                            ClearCanvas();
                            string locale = speechToTextLanguage.SelectedItem.ToString();
                            string resulttype = ComboAPIResult.SelectedItem.ToString();
                            LogMessage("Sending Memory Buffer...");
                            SpeechToTextResponse result = await client.SendBuffer(locale, resulttype);
                            if (result != null)
                            {
                                string httpError = result.GetHttpError();
                                if (!string.IsNullOrEmpty(httpError))
                                {
                                    resultText.Text = httpError;
                                    LogMessage("Http Error: " + httpError.ToString());
                                }
                                else
                                {
                                    if (result.Status() == "error")
                                        resultText.Text = "error";
                                    else
                                        resultText.Text = result.Result();
                                    LogMessage("Result: " + result.ToString());
                                }
                            }
                            else
                                LogMessage("Error while sending buffer");

                        }
                    }
                    else
                        LogMessage("Authentication failed check your subscription Key: " + subscriptionKey.Text.ToString());
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
            if (bInProgress == true)
                return;
            bInProgress = true;
            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);

                if (client != null)
                {
                    client.SetAPI(ComboHostname.SelectedItem.ToString(), customEndpointID.Text, (string)ComboAPI.SelectedItem,bUseWebSocket);

                    if ((!client.HasToken()) && (!string.IsNullOrEmpty(subscriptionKey.Text)))
                    {
                        LogMessage("Getting Token for subscription key: " + subscriptionKey.Text.ToString());
                        string token = await client.GetToken(subscriptionKey.Text);
                        if (!string.IsNullOrEmpty(token))
                        {
                            LogMessage("Getting Token successful Token: " + token.ToString());
                            // Save subscription key
                            SaveSettingsAndState();
                        }
                    }
                    if (client.HasToken())
                    {
                        if (client.IsRecording() == false)
                        {
                            if (await client.CleanupRecording())
                            {

                                if (await client.StartContinuousRecording(maxSize, duration, level))
                                {
                                    isRecordingContinuously = true;
                                    client.BufferReady += Client_BufferReady;
                                    client.AudioLevel += Client_AudioLevel;
                                    client.AudioCaptureError += Client_AudioCaptureError;
                                    client.WebSocketEvent += Client_WebSocketEvent;
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
                            await client.StopRecording();
                            isRecordingContinuously = false;
                            client.BufferReady -= Client_BufferReady;
                            client.AudioLevel -= Client_AudioLevel;
                            client.AudioCaptureError -= Client_AudioCaptureError;
                            client.WebSocketEvent -= Client_WebSocketEvent;
                            ClearCanvas();
                        }
                    }
                    else
                        LogMessage("Authentication failed check your subscription Key: " + subscriptionKey.Text.ToString());
                }
                UpdateControls();
            }
            finally
            {
                bInProgress = false;
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            }

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
        private async void Client_WebSocketEvent(SpeechToTextClient.SpeechToTextClient sender, string Path, string Body)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
           () =>
           {
               LogMessage("Received WebSocket: " + Path + " Message: " + Body);

               switch (Path.ToLower())
               {
                   case "turn.start":
                       break;
                   case "turn.end":
                       client.WebSocketEvent -= Client_WebSocketEvent;
                       break;
                   case "speech.enddetected":
                       break;
                   case "speech.phrase":
                       resultText.Text += GetPhrase(Body) + "\r\n";
                       break;
                   case "speech.hypothesis":
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

        private async void Client_BufferReady(object sender)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
           async () =>
            {
                SpeechToTextAudioStream stream;
                while ((stream = client.GetAudioStream()) !=null)
                {
                    string locale = speechToTextLanguage.SelectedItem.ToString();
                    string resulttype = ComboAPIResult.SelectedItem.ToString();
                    double start = stream.startTime.TotalSeconds;
                    double end = stream.endTime.TotalSeconds;
                    LogMessage("Sending Sub-Buffer: " + stream.Size.ToString() + " bytes for buffer from: " + start.ToString() + " seconds to: " + end.ToString() + " seconds");
                    SpeechToTextResponse result = await client.SendAudioStream(locale, resulttype, stream);
                    if (result != null)
                    {
                        string httpError = result.GetHttpError();
                        if (!string.IsNullOrEmpty(httpError))
                        {
                            resultText.Text = httpError;
                            LogMessage("Http Error: " + httpError.ToString());
                            DrawError();
                        }
                        else
                        {
                            if (result.Status() == "error")
                            {
                                resultText.Text = "error";
                                DrawError();
                            }
                            else
                            {
                                resultText.Text = result.Result();
                                DrawOk();
                            }
                            LogMessage("Result for buffer from: " + start.ToString() + " seconds to: " + end.ToString() + " seconds duration : " + (end-start).ToString() + " seconds \r\n" + result.ToString());
                        }
                    }
                    else
                        LogMessage("Error while sending buffer");
                }
            });
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
                if (client != null)
                {
                    client.SetAPI(ComboHostname.SelectedItem.ToString(), customEndpointID.Text, (string)ComboAPI.SelectedItem, bUseWebSocket);

                    if (client.IsRecording() == false)
                    {
                        if (await client.CleanupRecording())
                        {
                            if (await client.StartRecording(0))
                            {
                                isRecordingInFile = true;
                                client.AudioLevel += Client_AudioLevel;
                                client.AudioCaptureError += Client_AudioCaptureError;
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
                        await client.StopRecording();
                        isRecordingInFile = false;
                        client.AudioLevel -= Client_AudioLevel;
                        client.AudioCaptureError -= Client_AudioCaptureError;
                        ClearCanvas();
                        if (client.GetBufferLength() > 0)
                        {
                            var filePicker = new Windows.Storage.Pickers.FileSavePicker();
                            filePicker.DefaultFileExtension = ".wav";
                            filePicker.SuggestedFileName = "record.wav";
                            filePicker.FileTypeChoices.Add("WAV files", new List<string>() { ".wav" });
                            filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.VideosLibrary;
                            filePicker.SettingsIdentifier = "WavPicker";
                            filePicker.CommitButtonText = "Save buffer into a WAV File";

                            var wavFile = await filePicker.PickSaveFileAsync();
                            if (wavFile != null)
                            {
                                string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(wavFile);
                                if (await client.SaveBuffer(wavFile,0,0))
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
        private async void open_Click(object sender, RoutedEventArgs e)
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
        private async void sendWAVFile_Click(object sender, RoutedEventArgs e)
        {
            if (bInProgress == true)
                return;
            bInProgress = true;
            try
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Wait, 1);
                if (client != null)
                {
                    client.SetAPI(ComboHostname.SelectedItem.ToString(), customEndpointID.Text, (string)ComboAPI.SelectedItem, bUseWebSocket);
                    if ((!client.HasToken()) && (!string.IsNullOrEmpty(subscriptionKey.Text)))
                    {
                        LogMessage("Getting Token for subscription key: " + subscriptionKey.Text.ToString());
                        string token = await client.GetToken(subscriptionKey.Text);
                        if (!string.IsNullOrEmpty(token))
                        {
                            LogMessage("Getting Token successful Token: " + token.ToString());
                            // Save subscription key
                            SaveSettingsAndState();
                        }
                    }

                    if (client.HasToken())
                    {
                        string locale = speechToTextLanguage.SelectedItem.ToString();
                        string resulttype = ComboAPIResult.SelectedItem.ToString();

                        var file = await GetFileFromLocalPathUrl(mediaUri.Text);
                        if (file != null)
                        {
                            string convertedText = string.Empty;
                            LogMessage("Sending StorageFile: " + file.Path.ToString());
                            if(bUseWebSocket)
                                client.WebSocketEvent += Client_WebSocketEvent;
                            resultText.Text = string.Empty;
                            SpeechToTextResponse result = await client.SendStorageFile(file, locale,resulttype);
                            if (result != null)
                            {
                                string httpError = result.GetHttpError();
                                if (!string.IsNullOrEmpty(httpError))
                                {
                                    resultText.Text = httpError;
                                    LogMessage("Http Error: " + httpError.ToString());
                                }
                                else
                                {
                                    if (result.Status() == "error")
                                        resultText.Text = "error";
                                    else
                                        resultText.Text = result.Result();
                                    LogMessage("Result: " + result.ToString());
                                }
                            }
                            else
                                LogMessage("Error while sending file");
                        }
                    }
                    else
                        LogMessage("Authentication failed check your subscription Key: " + subscriptionKey.Text.ToString());
                }
            }
            finally
            {
                bInProgress = false;
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
            }

        }



        private void subscriptionKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (client != null)
                client.ClearToken();
        }



        private void ComboHostname_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (client != null)
                client.ClearToken();
            if (ComboHostname.SelectedItem.ToString() == defaultBingSpeechHostname)
            {
                subscriptionKey.Text = valueBingSpeechSubscription;
                customEndpointID.Text = string.Empty;
            }
            else if (ComboHostname.SelectedItem.ToString() == defaultWestUSSpeechHostname)
            {
                subscriptionKey.Text = valueWestUsSpeechSubscription;
                customEndpointID.Text = valueWestUsEndPointID;
            }
            else if (ComboHostname.SelectedItem.ToString() == defaultEastAsiaSpeechHostname)
            {
                subscriptionKey.Text = valueEastAsiaSpeechSubscription;
                customEndpointID.Text = valueEastAsiaEndPointID;
            }
            else if (ComboHostname.SelectedItem.ToString() == defaultNorthEuropeSpeechHostname)
            {
                subscriptionKey.Text = valueNorthEuropeSpeechSubscription;
                customEndpointID.Text = valueNorthEuropeEndPointID;
            }
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
