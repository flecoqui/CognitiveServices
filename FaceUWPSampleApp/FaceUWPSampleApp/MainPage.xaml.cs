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
using FaceClient;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml.Documents;
using Windows.Data.Json;
using Newtonsoft.Json;
using Windows.Graphics.Imaging;
using Windows.Graphics.Display;
using Windows.UI;

namespace FaceUWPSampleApp
{
    /// <summary>
    /// Main page for the application.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;


        static public MainPage Current = null;
        FaceClient.FaceClient client;
        const string defaultFaceHostname = "northeurope.api.cognitive.microsoft.com";
        string FaceHostname = defaultFaceHostname;
        string FaceSubscriptionKey = string.Empty;
        string CustomFaceSubscriptionKey = string.Empty;

        bool isPreviewingVideo = false;
        // Object to manage access to camera devices
        private MediaCapturePreviewer _previewer = null;
        // Folder in which the captures will be stored (initialized in InitializeCameraButton_Click)
        private StorageFolder _captureFolder = null;
        // Current Picture path
        string currentPicturePath = null;
        string[] LanguageArray = 
            {"en","zh"  };
        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            _previewer = new MediaCapturePreviewer(PreviewControl, Dispatcher);
        }
        protected override  void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            StopCamera();
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
            
            // ComboResolution Changed Event
            ComboResolution.SelectionChanged += ComboResolution_Changed;

            backgroundVideo.SizeChanged += BackgroundVideo_SizeChanged;

            // Fill Combobox API
            ComboVisualFeatures.Items.Clear();
            ComboVisualFeatures.Items.Add("returnFaceId");
            ComboVisualFeatures.Items.Add("returnFaceLandmarks");
            ComboVisualFeatures.Items.Add("returnFaceAttributes");
            ComboVisualFeatures.Items.Add("returnRecognitionModel");
            ComboVisualFeatures.SelectedIndex = 0;



            // Get Subscription ID from the local settings
            ReadSettingsAndState();
            // Display Picture
            await SetPictureUrl(currentPicturePath);
            PreviewControl.Visibility = Visibility.Collapsed;
            pictureElement.Visibility = Visibility.Visible;

            ComboGroups.SelectionChanged += ComboGroups_SelectionChanged;
            Hostname.TextChanged += Text_TextChanged;
            subscriptionKey.TextChanged += Text_TextChanged;
            group.TextChanged += Text_TextChanged;
            person.TextChanged += Text_TextChanged;

            // Update control and play first video
            UpdateControls();

            
            // Register Suspend/Resume
            Application.Current.Suspending += Current_Suspending;
            Application.Current.Resuming += Current_Resuming;
            
            // Display OS, Device information
            LogMessage(FaceClient.SystemInformation.GetString());
            
            // Create Cognitive Service Vision Client
            client = new FaceClient.FaceClient();



        }

        private void Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateControls();
        }

        private async void ComboGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var group = ComboGroups.SelectedItem as Group;
            if(group!=null)
            {
                await fillComboPerson(group.personGroupId);
            }
            UpdateControls();
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
        async void Current_Resuming(object sender, object e)
        {
            LogMessage("Resuming");
            ReadSettingsAndState();
            // Display Picture
            await SetPictureUrl(currentPicturePath);
            PreviewControl.Visibility = Visibility.Collapsed;
            pictureElement.Visibility = Visibility.Visible;


            //Update Controls
            UpdateControls();
        }
        /// <summary>
        /// This method is called when the application is suspending
        /// </summary>
        void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            LogMessage("Suspending");
            var deferal = e.SuspendingOperation.GetDeferral();
            SaveSettingsAndState();
            if (isPreviewingVideo)
            {
                LogMessage("Stop Camera...");
                StopCamera();
                isPreviewingVideo = false;
            }
            deferal.Complete();
        }

        #region Settings
        const string keyFaceHostname = "FaceHostnameKey";
        const string keyCustomFaceHostname = "CustomFaceHostnameKey";
        const string keySubscriptionKey = "SubscriptionKey";
        const string keyCustomSubscriptionKey = "CustomSubscriptionKey";
        const string keyCurrentPicturePath = "CurrentPicturePath";
        const string keyIsCustom = "isCustomKey";
        const string keyVisionVisualFeatures = "VisionVisualFeaturesKey";
        const string keyVisionDetails = "VisionDetailsKey";
        const string keyProjectID = "ProjectIDKey";
        const string keyIterationID = "IterationIDKey";
        /// <summary>
        /// Function to save all the persistent attributes
        /// </summary>
        public bool SaveSettingsAndState()
        {
            FaceSubscriptionKey = subscriptionKey.Text;
            FaceHostname = Hostname.Text;
            SaveSettingsValue(keyCustomSubscriptionKey, CustomFaceSubscriptionKey);
            SaveSettingsValue(keyFaceHostname, FaceHostname);
            SaveSettingsValue(keySubscriptionKey, FaceSubscriptionKey);
            SaveSettingsValue(keyCurrentPicturePath, currentPicturePath);
            SaveSettingsValue(keyVisionVisualFeatures, (string) ComboVisualFeatures.SelectedItem);

            return true;
        }
        /// <summary>
        /// Function to read all the persistent attributes
        /// </summary>
        public bool ReadSettingsAndState()
        {
            string s = ReadSettingsValue(keyCustomSubscriptionKey) as string;
            if (!string.IsNullOrEmpty(s))
                CustomFaceSubscriptionKey = s;
            s = ReadSettingsValue(keySubscriptionKey) as string;
            if (!string.IsNullOrEmpty(s))
                FaceSubscriptionKey = s;


            s = ReadSettingsValue(keyCurrentPicturePath) as string;
            if (!string.IsNullOrEmpty(s))
                currentPicturePath = s;

            s = ReadSettingsValue(keyFaceHostname) as string;
            if (!string.IsNullOrEmpty(s))
                FaceHostname = s;
            else
                FaceHostname = defaultFaceHostname;


            bool bresult = false;
            s = ReadSettingsValue(keyIsCustom) as string;
            if (!string.IsNullOrEmpty(s))
                bool.TryParse(s, out bresult);
            Hostname.Text = FaceHostname;
            subscriptionKey.Text = FaceSubscriptionKey;



            s = ReadSettingsValue(keyVisionVisualFeatures) as string;
            if (!string.IsNullOrEmpty(s))
                ComboVisualFeatures.SelectedItem = s;
            else
                ComboVisualFeatures.SelectedItem = "returnFaceLandmaarks";


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
        public async void LogMessage(string Message)
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
        /// Populates the given combo box with all possible combinations of the given stream type settings returned by the camera driver
        /// </summary>
        /// <param name="streamType"></param>
        /// <param name="comboBox"></param>
        private void PopulateComboBox(MediaStreamType streamType, ComboBox comboBox, bool showFrameRate = true)
        {
            // Query all preview properties of the device 
            IEnumerable<StreamResolution> allStreamProperties = _previewer.MediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType).Select(x => new StreamResolution(x));
            // Order them by resolution then frame rate
            allStreamProperties = allStreamProperties.OrderByDescending(x => x.Height * x.Width).ThenByDescending(x => x.FrameRate);
            comboBox.Items.Clear();
            // Populate the combo box with the entries
            foreach (var property in allStreamProperties)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = property.GetFriendlyName(showFrameRate);
                comboBoxItem.Tag = property;
                comboBox.Items.Add(comboBoxItem);
            }
        }
        /// <summary>
        /// Event handler for Photo settings combo box. Updates stream resolution based on the selection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ComboResolution_Changed(object sender, RoutedEventArgs e)
        {
            if (_previewer.IsPreviewing)
            {
                var selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    var encodingProperties = (selectedItem.Tag as StreamResolution).EncodingProperties;
                    await _previewer.MediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, encodingProperties);

                }
            }
        }
        private async void StartCamera()
        {


            await _previewer.InitializeCameraAsync();

            if (_previewer.IsPreviewing)
            {

                
                PopulateComboBox(MediaStreamType.Photo, ComboResolution, false);

            }

            var picturesLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            // Fall back to the local app storage if the Pictures Library is not available
            _captureFolder = picturesLibrary.SaveFolder ?? ApplicationData.Current.LocalFolder;
        }
        private async void StopCamera()
        {
            
            await _previewer.CleanupCameraAsync();
        }
        private async System.Threading.Tasks.Task<string> CapturePhoto()
        {
            string path = string.Empty;
            if (_previewer.IsPreviewing)
            {

                var stream = new InMemoryRandomAccessStream();

                try
                {
                    // Take and save the photo
                    var file = await _captureFolder.CreateFileAsync("VisionPhoto.jpg", CreationCollisionOption.GenerateUniqueName);
                    await _previewer.MediaCapture.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), file);
                    LogMessage("Photo taken, saved to: " + file.Path);
                    path = file.Path;
                }
                catch (Exception ex)
                {
                    // File I/O errors are reported as exceptions.
                    LogMessage("Exception when taking a photo: " + ex.Message);
                }


            }
            return path;
        }
        async System.Threading.Tasks.Task<bool> fillComboGroup(string groupId = null, string groupName = null)
        {
            bool result = false;
            try
            {
                ComboGroups.Items.Clear();
                var response = await client.ListPersonGroup(subscriptionKey.Text.ToString(), Hostname.Text);
                if (response != null)
                {
                    string error = response.GetHttpError();
                    if (!string.IsNullOrEmpty(error))
                    {
                        LogMessage("Error from Cognitive Services: " + error);
                    }
                    else
                    {
                        SaveSettingsAndState();
                        LogMessage("Response from Cognitive Services: " + ParseString(response.Result()));

                        List<Group> list = JsonConvert.DeserializeObject<List<Group>>(response.Result());
                        if ((list != null) && (list.LongCount() > 0))
                        {
                            foreach (var g in list)
                            {
                                ComboGroups.Items.Add(g);
                                LogMessage("Add group: " + g.name + " id: " + g.personGroupId);
                            }
                            if (ComboGroups.Items.Count > 0)
                            {
                                if((string.IsNullOrEmpty(groupId))&& (string.IsNullOrEmpty(groupName)))
                                    ComboGroups.SelectedIndex = 0;
                                else
                                {
                                    int i = 0;
                                    for ( ; i <ComboGroups.Items.Count;i++)
                                    {
                                        var g = ComboGroups.Items[i] as Group;
                                        if(g != null)
                                        {
                                            if ((!string.IsNullOrEmpty(groupId)) && (g.personGroupId == groupId))
                                            {
                                                ComboGroups.SelectedIndex = i;
                                                break;
                                            }
                                            if ((!string.IsNullOrEmpty(groupName)) && (g.name == groupName))
                                            {
                                                ComboGroups.SelectedIndex = i;
                                                break;
                                            }
                                        }
                                    }
                                    if(i> ComboGroups.Items.Count)
                                        ComboGroups.SelectedIndex = 0;

                                }
                            }
                            result = true;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogMessage("Exeption while getting the list of groups: " + ex.Message);
            }
            finally
            {
            }
            return result;
        }
        async System.Threading.Tasks.Task<bool> fillComboPerson(string GroupId,string personId = null, string personName = null)
        {
            bool result = false;
            try
            {
                ComboPersons.Items.Clear();
                var response = await client.ListPersonGroupsPersons(subscriptionKey.Text.ToString(), Hostname.Text, GroupId);
                if (response != null)
                {
                    string error = response.GetHttpError();
                    if (!string.IsNullOrEmpty(error))
                    {
                        LogMessage("Error from Cognitive Services: " + error);
                    }
                    else
                    {
                        SaveSettingsAndState();
                        LogMessage("Response from Cognitive Services: " + ParseString(response.Result()));

                        List<Person> list = JsonConvert.DeserializeObject<List<Person>>(response.Result());
                        if ((list != null) && (list.LongCount() > 0))
                        {
                            foreach (var g in list)
                            {
                                ComboPersons.Items.Add(g);
                                LogMessage("Add person: " + g.name + " id: " + g.personId);
                            }

                            if (ComboPersons.Items.Count > 0)
                            {
                                if ((string.IsNullOrEmpty(personId))&&(string.IsNullOrEmpty(personName)))
                                    ComboPersons.SelectedIndex = 0;
                                else
                                {
                                    int i = 0;
                                    for (; i < ComboPersons.Items.Count; i++)
                                    {
                                        var g = ComboPersons.Items[i] as Person;
                                        if (g != null)
                                        {
                                            if ((!string.IsNullOrEmpty(personId)) && (g.personId == personId))
                                            {
                                                ComboPersons.SelectedIndex = i;
                                                break;
                                            }
                                            if ((!string.IsNullOrEmpty(personName)) && (g.name == personName))
                                            {
                                                ComboPersons.SelectedIndex = i;
                                                break;
                                            }
                                        }
                                    }
                                    if (i > ComboPersons.Items.Count)
                                        ComboPersons.SelectedIndex = 0;

                                }
                            }

                            result = true;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogMessage("Exeption while getting the list of groups: " + ex.Message);
            }
            finally
            {
            }
            return result;
        }

        private async void refresh_Click(object sender, RoutedEventArgs e)
        {
            await fillComboGroup();
            UpdateControls();
        }
        private async void trainGroup_Click(object sender, RoutedEventArgs e)
        {
            var group = ComboGroups.SelectedItem as Group;
            if (group != null)
            {
                string groupId = group.personGroupId;

                try
                {
                    var response = await client.TrainPersonGroup(subscriptionKey.Text.ToString(), Hostname.Text, groupId);
                    if (response != null)
                    {
                        string error = response.GetHttpError();
                        if (!string.IsNullOrEmpty(error))
                        {
                            LogMessage("Error from Cognitive Services calling TrainPersonGroup: " + error);
                        }
                        else
                        {
                            LogMessage("Response from Cognitive Services calling TrainPersonGroup success - Training launched: " + ParseString(response.Result()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("Exeption while calling TrainPersonGroup: " + ex.Message);
                }
                finally
                {
                }
            }
            UpdateControls();
        }
        private async void trainingGroup_Click(object sender, RoutedEventArgs e)
        {
            var group = ComboGroups.SelectedItem as Group;
            if (group != null)
            {
                string groupId = group.personGroupId;

                try
                {
                    var response = await client.TrainingPersonGroup(subscriptionKey.Text.ToString(), Hostname.Text, groupId);
                    if (response != null)
                    {
                        string error = response.GetHttpError();
                        if (!string.IsNullOrEmpty(error))
                        {
                            LogMessage("Error from Cognitive Services when calling TrainingPersonGroup: " + error);
                        }
                        else
                        {
                            LogMessage("Response from Cognitive Services when calling TrainingPersonGroup - Training successfully completed: " + ParseString(response.Result()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("Exeption while calling TrainingPersonGroup: " + ex.Message);
                }
                finally
                {
                }
            }
            UpdateControls();
        }
        private async void addGroup_Click(object sender, RoutedEventArgs e)
        {
            string newGroupName = group.Text;
            if (!string.IsNullOrEmpty(newGroupName))
            {

                try
                {
                    ComboPersons.Items.Clear();
                    var response = await client.AddPersonGroup(subscriptionKey.Text.ToString(), Hostname.Text, newGroupName);
                    if (response != null)
                    {
                        string error = response.GetHttpError();
                        if (!string.IsNullOrEmpty(error))
                        {
                            LogMessage("Error from Cognitive Services: " + error);
                        }
                        else
                        {
                            LogMessage("Response from Cognitive Services: " + ParseString(response.Result()));
                        }
                        await fillComboGroup( null, newGroupName);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("Exeption while getting the list of groups: " + ex.Message);
                }
                finally
                {
                }
            }
            UpdateControls();
        }
        private async void deletePerson_Click(object sender, RoutedEventArgs e)
        {
            var group = ComboGroups.SelectedItem as Group;
            if (group != null)
            {
                var person = ComboPersons.SelectedItem as Person;
                if (person != null)
                {
                    string groupId = group.personGroupId;
                    string personId = person.personId;

                    try
                    {
                        ComboPersons.Items.Clear();
                        var response = await client.DeletePersonGroupsPerson(subscriptionKey.Text.ToString(), Hostname.Text, groupId, personId);
                        if (response != null)
                        {
                            string error = response.GetHttpError();
                            if (!string.IsNullOrEmpty(error))
                            {
                                LogMessage("Error from Cognitive Services: " + error);
                            }
                            else
                            {
                                LogMessage("Response from Cognitive Services: " + ParseString(response.Result()));
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        LogMessage("Exeption while getting the list of groups: " + ex.Message);
                    }
                    finally
                    {
                    }
                    await fillComboPerson(group.personGroupId);
                }
            }
            UpdateControls();
        }
        private async void addPersonFace_Click(object sender, RoutedEventArgs e)
        {
            var group = ComboGroups.SelectedItem as Group;
            if (group != null)
            {
                var person = ComboPersons.SelectedItem as Person;
                if (person != null)
                {
                    string groupId = group.personGroupId;
                    string personId = person.personId;

                    if (!string.IsNullOrEmpty(currentPicturePath))
                    {
                        sendPictureButton.IsEnabled = false;
                        Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(currentPicturePath);
                        if (file != null)
                        {

                            try
                            {
                                ComboPersons.Items.Clear();
                                var response = await client.AddPersonGroupsPersonFace(subscriptionKey.Text.ToString(), Hostname.Text, groupId, personId, file);
                                if (response != null)
                                {
                                    string error = response.GetHttpError();
                                    if (!string.IsNullOrEmpty(error))
                                    {
                                        LogMessage("Error from Cognitive Services: " + error);
                                    }
                                    else
                                    {
                                        LogMessage("Response from Cognitive Services: " + ParseString(response.Result()));
                                    }
                                    await fillComboPerson(group.personGroupId, personId);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage("Exeption while getting the list of groups: " + ex.Message);
                            }
                            finally
                            {
                            }
                        }
                    }
                }
            }
            UpdateControls();
        }
        private async void addPerson_Click(object sender, RoutedEventArgs e)
        {
            var group = ComboGroups.SelectedItem as Group;
            if (group != null)
            {
                string newperson = person.Text ;
                if (!string.IsNullOrEmpty(newperson))
                {
                    string groupId = group.personGroupId;
                    string personName = newperson;

                    try
                    {
                        ComboPersons.Items.Clear();
                        var response = await client.AddPersonGroupsPerson(subscriptionKey.Text.ToString(), Hostname.Text, groupId, personName);
                        if (response != null)
                        {
                            string error = response.GetHttpError();
                            if (!string.IsNullOrEmpty(error))
                            {
                                LogMessage("Error from Cognitive Services: " + error);
                            }
                            else
                            {
                                LogMessage("Response from Cognitive Services: " + ParseString(response.Result()));
                            }
                            await fillComboPerson(group.personGroupId,null, personName);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Exeption while getting the list of groups: " + ex.Message);
                    }
                    finally
                    {
                    }
                }
            }
            UpdateControls();
        }
        private async void deleteGroup_Click(object sender, RoutedEventArgs e)
        {
            var group = ComboGroups.SelectedItem as Group;
            if (group != null)
            {
                    string groupId = group.personGroupId;

                    try
                    {
                        ComboPersons.Items.Clear();
                        var response = await client.DeletePersonGroup(subscriptionKey.Text.ToString(), Hostname.Text, groupId);
                        if (response != null)
                        {
                            string error = response.GetHttpError();
                            if (!string.IsNullOrEmpty(error))
                            {
                                LogMessage("Error from Cognitive Services DeletePersonGroup: " + error);
                            }
                            else
                            {
                                LogMessage("Response from Cognitive Services DeletePersonGroup: " + ParseString(response.Result()));
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        LogMessage("Exeption while getting the list of groups: " + ex.Message);
                    }
                    finally
                    {
                    }
                    await fillComboGroup();
            }
            UpdateControls();

        }

        /// <summary>
        /// Mute method 
        /// </summary>
        private async void startCamera_Click(object sender, RoutedEventArgs e)
        {
            isPreviewingVideo = !isPreviewingVideo;
            previewButton.IsEnabled = false;
            try

            {
                if (isPreviewingVideo)
                {
                    LogMessage("Start Camera");
                    StartCamera();
                    PreviewControl.Visibility = Visibility.Visible;
                    pictureElement.Visibility = Visibility.Collapsed;
                    SetPreviewSize();
                }
                else
                {
                    LogMessage("Capture Photo");
                    string path = await CapturePhoto();
                    if (!string.IsNullOrEmpty(path))
                    {
                        LogMessage("Display photo: " + path);
                        await SetPictureUrl("file://" + path);
                    }
                    LogMessage("StopCamera");

                    StopCamera();

                    PreviewControl.Visibility = Visibility.Collapsed;
                    pictureElement.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LogMessage("Exeption while taking photo: " + ex.Message);
            }
            finally
            {

                previewButton.IsEnabled = true;
            }
            UpdateControls();
        }
        /// <summary>
        /// open method which select a WAV file on disk
        /// </summary>
        private async void openPicture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
                filePicker.FileTypeFilter.Add(".jpg");
                filePicker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                filePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                filePicker.SettingsIdentifier = "JPGPicker";
                filePicker.CommitButtonText = "Open JPG File to Process";

                var jpgFile = await filePicker.PickSingleFileAsync();
                if (jpgFile != null)
                {
                    string fileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(jpgFile);

                    LogMessage("Selected file: " + jpgFile.Path);
                    await SetPictureUrl("file://" + jpgFile.Path);
                    PreviewControl.Visibility = Visibility.Collapsed;
                    pictureElement.Visibility = Visibility.Visible;
                    UpdateControls();
                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to select WAV file: Exception: " + ex.Message);
            }
        }
        private string ParseString(string jsonString)
        {
            return jsonString.Replace(",", ",\n").Replace("{", "{\n").Replace("}", "\n}");
        }
        /// <summary>
        /// sendPicture_Click method 
        /// </summary>
        private async void sendPicture_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Send current picture to Cognitive Services: " + currentPicturePath);
            if (!string.IsNullOrEmpty(currentPicturePath))
            {
                sendPictureButton.IsEnabled = false;
                try
                {
                    Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(currentPicturePath);
                    if (file != null)
                    {
                        // Cognitive Service Vision GetToken 
                        if (!string.IsNullOrEmpty(subscriptionKey.Text))
                        {
                            FaceResponse response = null;
                            string featureString = "?";
                            switch(ComboVisualFeatures.SelectedItem.ToString())
                            {
                                case "returnFaceId":
                                    featureString += "returnFaceId&detectionModel";
                                    break;
                                case "returnRecognitionModel":
                                    featureString += "returnRecognitionModel&recognitionModel";
                                    break;
                                default:
                                    featureString += ComboVisualFeatures.SelectedItem.ToString();
                                    break;
                            }


                            response = await client.DetectFace(subscriptionKey.Text.ToString(), Hostname.Text, featureString,  file);
                            if (response!=null)
                            {
                                string error = response.GetHttpError();
                                if (!string.IsNullOrEmpty(error))
                                {
                                    LogMessage("Error from Cognitive Services: " + error);
                                }
                                else
                                {
                                    SaveSettingsAndState();
                                    LogMessage("Response from Cognitive Services: " + ParseString(response.Result()));

                                    List<Face> list = JsonConvert.DeserializeObject<List<Face>>(response.Result());
                                    if ((list != null) && (list.LongCount() > 0))
                                    {
                                        var group = ComboGroups.SelectedItem as Group;
                                        if (group != null)
                                        {

                                            Identify ident = new Identify();
                                            ident.faceIds = new List<string>();
                                            foreach (var f in list)
                                            {
                                                LogMessage("Identifying face: " + f.faceId);
                                                ident.faceIds.Add(f.faceId);
                                            }
                                            ident.maxNumOfCandidatesReturned = 1;
                                            ident.confidenceThreshold = 0.5;
                                            ident.largePersonGroupId = group.personGroupId;
                                            response = await client.IdentifyFaces(subscriptionKey.Text.ToString(), Hostname.Text, ident.largePersonGroupId, ident.maxNumOfCandidatesReturned, ident.confidenceThreshold, ident.faceIds);
                                            if (response != null)
                                            {
                                                error = response.GetHttpError();
                                                if (!string.IsNullOrEmpty(error))
                                                {
                                                    LogMessage("Error from Cognitive Services Identify Face API: " + error);
                                                }
                                                else
                                                {
                                                    LogMessage("Response from Cognitive Services Identify Face API: " + ParseString(response.Result()));

                                                    List<IdentifyResult> listIdentify = JsonConvert.DeserializeObject<List<IdentifyResult>>(response.Result());
                                                    if (listIdentify != null)
                                                    {
                                                        List<PersonDetected> listpd = new List<PersonDetected>();
                                                        foreach (var f in listIdentify)
                                                        {
                                                            PersonDetected pd = new PersonDetected();

                                                            string name = "unknown";
                                                            if ((f.candidates != null) && (f.candidates.Count > 0))
                                                            {
                                                                LogMessage("FaceID: " + f.faceId + " PersonID: " + f.candidates[0].personId + " with confidence: " + f.candidates[0].confidence);
                                                                if ((ComboPersons.Items != null) && (ComboPersons.Items.Count > 0))
                                                                {
                                                                    for (int i = 0; i < ComboPersons.Items.Count; i++)
                                                                    {
                                                                        Person p = ComboPersons.Items[i] as Person;
                                                                        if ((p != null)&&(p.personId == f.candidates[0].personId))
                                                                        {
                                                                            name = p.name;
                                                                            break;
                                                                        }
                                                                        
                                                                    }
                                                                }
                                                            }
                                                            pd.name = name;
                                                            pd.personId = f.candidates[0].personId;
                                                            var frect = list.Where(x => x.faceId == f.faceId).FirstOrDefault();
                                                            pd.rect = new Rectangle();
                                                            pd.rect.height = frect.faceRectangle.height;
                                                            pd.rect.width = frect.faceRectangle.width;
                                                            pd.rect.left = frect.faceRectangle.left;
                                                            pd.rect.top = frect.faceRectangle.top;
                                                            listpd.Add(pd);
                                                            LogMessage("FaceID: " + f.faceId + " Person: " + name + " PersonID: " + f.candidates[0].personId + " with confidence: " + f.candidates[0].confidence);
                                                        }
                                                        HighlightDetectedFaces(listpd);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // File I/O errors are reported as exceptions.
                    LogMessage("Exception when Identifying person: " + ex.Message);
                }
                finally
                {
                    sendPictureButton.IsEnabled = true;
                }
            }
            else
            {
                LogMessage("Sending picture: Path not defined");
            }
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



        #endregion Media

        #region ui
        /// <summary>
        /// BackgroundVideo picture Resize Event
        /// </summary>        
        void BackgroundVideo_SizeChanged(System.Object sender, SizeChangedEventArgs e)
        {
            SetPictureElementSize();
            SetPreviewSize();
        }
        /// <summary>
        /// Set the source for the picture : windows  
        /// </summary>
        void SetPictureSource(Windows.UI.Xaml.Media.Imaging.BitmapImage b)
        {
            // Set picture source for windows element
            pictureElement.Source = b;
        }
        /// <summary>
        /// Resize the pictureElement to match with the BackgroundVideo size
        /// </summary>      
        void SetPictureElementSize()
        {
            Windows.UI.Xaml.Media.Imaging.BitmapImage b = pictureElement.Source as Windows.UI.Xaml.Media.Imaging.BitmapImage;
            if (b != null)
            {
                int nWidth;
                int nHeight;
                double ratioBackground = backgroundVideo.ActualWidth / backgroundVideo.ActualHeight;
                double ratioPicture = ((double)b.PixelWidth / (double)b.PixelHeight);
                if (ratioPicture > ratioBackground)
                {
                    nWidth = (int)backgroundVideo.ActualWidth;
                    nHeight = (int)(nWidth / ratioPicture);
                }
                else
                {
                    nHeight = (int)backgroundVideo.ActualHeight;
                    nWidth = (int)(nHeight * ratioPicture);

                }
                pictureElement.Width = nWidth;
                pictureElement.Height = nHeight;
            }
        }
        /// <summary>
        /// Resize the preview to match with the BackgroundVideo size
        /// </summary>      
        void SetPreviewSize()
        {
            if (isPreviewingVideo)
            {
                int nWidth;
                int nHeight;
                double ratioBackground = backgroundVideo.ActualWidth / backgroundVideo.ActualHeight;
                if (PreviewControl.ActualHeight == 0)
                {
                    PreviewControl.Width = (int)backgroundVideo.ActualWidth;
                    PreviewControl.Height = (int)backgroundVideo.ActualHeight;
                }
                else
                {
                    double ratioPicture = ((double)PreviewControl.ActualWidth / (double)PreviewControl.ActualHeight);
                    if (ratioPicture > ratioBackground)
                    {
                        nWidth = (int)backgroundVideo.ActualWidth;
                        nHeight = (int)(nWidth / ratioPicture);
                    }
                    else
                    {
                        nHeight = (int)backgroundVideo.ActualHeight;
                        nWidth = (int)(nHeight * ratioPicture);

                    }
                    PreviewControl.Width = nWidth;
                    PreviewControl.Height = nHeight;
                }

            }
        }
        private async System.Threading.Tasks.Task<bool> SetDefaultPicture()
        {
            var uri = new System.Uri("ms-appx:///Assets/Photo.png");
            Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
            if (file != null)
            {
                using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    if (fileStream != null)
                    {
                        Windows.UI.Xaml.Media.Imaging.BitmapImage b = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                        if (b != null)
                        {
                            b.SetSource(fileStream);
                            SetPictureSource(b);
                            SetPictureElementSize();
                            return true;
                        }
                    }
                }
            }
            return false;

        }
        /// <summary>
        /// This method set the poster source for the MediaElement 
        /// </summary>
        private async System.Threading.Tasks.Task<bool> SetPictureUrl(string PosterUrl)
        {
            try
            {

                currentPicturePath = PosterUrl;
                Windows.Storage.StorageFile file = await GetFileFromLocalPathUrl(PosterUrl);
                if (file != null)
                {
                    using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        if (fileStream != null)
                        {
                            Windows.UI.Xaml.Media.Imaging.BitmapImage b = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                            if (b != null)
                            {
                                b.SetSource(fileStream);
                                SetPictureSource(b);
                                SetPictureElementSize();

                                return true;
                            }
                        }
                    }
                }
                else
                {
                    await SetDefaultPicture();
                    LogMessage("Failed to load poster: " + PosterUrl);
                    return true;
                }

            }
            catch (Exception e)
            {
                LogMessage("Exception while loading poster: " + PosterUrl + " - " + e.Message);
            }

            return false;
        }

        /// <summary>
        /// UpdateControls Method which update the controls on the page  
        /// </summary>
        async void UpdateControls()
        {

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 () =>
                 {
                     if((string.IsNullOrEmpty(Hostname.Text))||(string.IsNullOrEmpty(subscriptionKey.Text)))
                     {
                         refreshButton.IsEnabled = false;
                         ComboGroups.IsEnabled = false;
                         previewButton.IsEnabled = false;
                         openPictureButton.IsEnabled = false;

                         VisionPanel.Visibility = Visibility.Visible;
                         if (isPreviewingVideo)
                         {
                             previewButton.Content = "\xE722";
                             ComboResolution.IsEnabled = false;
                             sendPictureButton.IsEnabled = false;
                             openPictureButton.IsEnabled = false;
                         }
                         else
                         {
                             previewButton.Content = "\xE714";
                             ComboResolution.IsEnabled = false;
                             openPictureButton.IsEnabled = false;
                            sendPictureButton.IsEnabled = false;
                         }
                         addGroupButton.IsEnabled = false;
                         deleteGroupButton.IsEnabled = false;
                         addPersonButton.IsEnabled = false;
                         deletePersonButton.IsEnabled = false;


                     }
                     else
                     {

                         if (!string.IsNullOrEmpty(group.Text))
                             addGroupButton.IsEnabled = true;
                         else
                             addGroupButton.IsEnabled = false;
                         if ((ComboGroups.Items != null) && (ComboGroups.Items.Count > 0))
                         {
                             deleteGroupButton.IsEnabled = true;
                             trainGroupButton.IsEnabled = true;
                             trainingGroupButton.IsEnabled = true;
                         }
                         else
                         {
                             deleteGroupButton.IsEnabled = false;
                             trainGroupButton.IsEnabled = false;
                             trainingGroupButton.IsEnabled = false;
                         }
                         if (!string.IsNullOrEmpty(person.Text))
                             addPersonButton.IsEnabled = true;
                         else
                             addPersonButton.IsEnabled = false;

                         if ((ComboPersons.Items != null) && (ComboPersons.Items.Count > 0))
                         {
                             addPersonFaceButton.IsEnabled = true;
                             deletePersonButton.IsEnabled = true;
                         }
                         else
                         {
                             addPersonFaceButton.IsEnabled = false;
                             deletePersonButton.IsEnabled = false;
                         }

                         refreshButton.IsEnabled = true;
                         ComboGroups.IsEnabled = true;
                         previewButton.IsEnabled = true;
                         openPictureButton.IsEnabled = true;
                         
                             VisionPanel.Visibility = Visibility.Visible;
                         if (isPreviewingVideo)
                         {
                             previewButton.Content = "\xE722";
                             ComboResolution.IsEnabled = true;
                             sendPictureButton.IsEnabled = false;
                             openPictureButton.IsEnabled = false;
                         }
                         else
                         {
                             previewButton.Content = "\xE714";
                             ComboResolution.IsEnabled = false;
                             openPictureButton.IsEnabled = true;
                             if (!string.IsNullOrEmpty(currentPicturePath))
                                sendPictureButton.IsEnabled = true;
                             else
                                sendPictureButton.IsEnabled = false;
                         }
                     }
                 });
        }

        /// <summary>
        /// Calculates the size and location of the rectangle that contains the preview stream within the preview control, when the scaling mode is Uniform
        /// </summary>
        /// <param name="previewResolution">The resolution at which the preview is running</param>
        /// <param name="previewControl">The control that is displaying the preview using Uniform as the scaling mode</param>
        /// <returns></returns>
        public Rect GetPreviewStreamRectInControl(Image previewControl)
        {
            var result = new Rect();
            /*
            // In case this function is called before everything is initialized correctly, return an empty result
            if (previewControl == null || previewControl.ActualHeight < 1 || previewControl.ActualWidth < 1 ||
                previewResolution == null || previewResolution.Height == 0 || previewResolution.Width == 0)
            {
                return result;
            }
            */
            var streamWidth = previewControl.Width;
            var streamHeight = previewControl.Height;

            // For portrait orientations, the width and height need to be swapped
            if (_displayOrientation == DisplayOrientations.Portrait || _displayOrientation == DisplayOrientations.PortraitFlipped)
            {
                streamWidth = previewControl.Height;
                streamHeight = previewControl.Width;
            }

            // Start by assuming the preview display area in the control spans the entire width and height both (this is corrected in the next if for the necessary dimension)
            result.Width = previewControl.ActualWidth;
            result.Height = previewControl.ActualHeight;

            // If UI is "wider" than preview, letterboxing will be on the sides
            if ((previewControl.ActualWidth / previewControl.ActualHeight > streamWidth / (double)streamHeight))
            {
                var scale = previewControl.ActualHeight / streamHeight;
                var scaledWidth = streamWidth * scale;

                result.X = (previewControl.ActualWidth - scaledWidth) / 2.0;
                result.Width = scaledWidth;
            }
            else // Preview stream is "wider" than UI, so letterboxing will be on the top+bottom
            {
                var scale = previewControl.ActualWidth / streamWidth;
                var scaledHeight = streamHeight * scale;

                result.Y = (previewControl.ActualHeight - scaledHeight) / 2.0;
                result.Height = scaledHeight;
            }

            return result;
        }

        /// <summary>
        /// Takes face information defined in preview coordinates and returns one in UI coordinates, taking
        /// into account the position and size of the preview control.
        /// </summary>
        /// <param name="faceBoxInPreviewCoordinates">Face coordinates as retried from the FaceBox property of a DetectedFace, in preview coordinates.</param>
        /// <returns>Rectangle in UI (CaptureElement) coordinates, to be used in a Canvas control.</returns>
        /// 
        /// 
        
        private Windows.UI.Xaml.Shapes.Rectangle ConvertPreviewToUiRectangle(BitmapBounds faceBoxInPreviewCoordinates)
        {
            var result = new Windows.UI.Xaml.Shapes.Rectangle();
            var previewStream = pictureElement;
            
            //._previewProperties as VideoEncodingProperties;

            // If there is no available information about the preview, return an empty rectangle, as re-scaling to the screen coordinates will be impossible
            if (previewStream == null) return result;

            // Similarly, if any of the dimensions is zero (which would only happen in an error case) return an empty rectangle
            if (previewStream.Width == 0 || previewStream.Height == 0) return result;

            double streamWidth = previewStream.Width;
            double streamHeight = previewStream.Height;

            // For portrait orientations, the width and height need to be swapped
            if (_displayOrientation == DisplayOrientations.Portrait || _displayOrientation == DisplayOrientations.PortraitFlipped)
            {
                streamHeight = previewStream.Width;
                streamWidth = previewStream.Height;
            }

            // Get the rectangle that is occupied by the actual video feed
            var previewInUI = GetPreviewStreamRectInControl(pictureElement);

            // Scale the width and height from preview stream coordinates to window coordinates
            result.Width = (faceBoxInPreviewCoordinates.Width / streamWidth) * previewInUI.Width;
            result.Height = (faceBoxInPreviewCoordinates.Height / streamHeight) * previewInUI.Height;

            // Scale the X and Y coordinates from preview stream coordinates to window coordinates
            var x = (faceBoxInPreviewCoordinates.X / streamWidth) * previewInUI.Width;
            var y = (faceBoxInPreviewCoordinates.Y / streamHeight) * previewInUI.Height;
            Canvas.SetLeft(result, x);
            Canvas.SetTop(result, y);

            return result;
        }
        /*
        /// <summary>
        /// Uses the current display orientation to calculate the rotation transformation to apply to the face detection bounding box canvas
        /// and mirrors it if the preview is being mirrored
        /// </summary>
        private void SetFacesCanvasRotation()
        {
            // Calculate how much to rotate the canvas
            int rotationDegrees = ConvertDisplayOrientationToDegrees(_displayOrientation);

            // The rotation direction needs to be inverted if the preview is being mirrored, just like in SetPreviewRotationAsync
            if (_mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }

            // Apply the rotation
            var transform = new RotateTransform { Angle = rotationDegrees };
            FacesCanvas.RenderTransform = transform;

            var previewArea = GetPreviewStreamRectInControl(_previewProperties as VideoEncodingProperties, PreviewControl);

            // For portrait mode orientations, swap the width and height of the canvas after the rotation, so the control continues to overlap the preview
            if (_displayOrientation == DisplayOrientations.Portrait || _displayOrientation == DisplayOrientations.PortraitFlipped)
            {
                FacesCanvas.Width = previewArea.Height;
                FacesCanvas.Height = previewArea.Width;

                // The position of the canvas also needs to be adjusted, as the size adjustment affects the centering of the control
                Canvas.SetLeft(FacesCanvas, previewArea.X - (previewArea.Height - previewArea.Width) / 2);
                Canvas.SetTop(FacesCanvas, previewArea.Y - (previewArea.Width - previewArea.Height) / 2);
            }
            else
            {
                FacesCanvas.Width = previewArea.Width;
                FacesCanvas.Height = previewArea.Height;

                Canvas.SetLeft(FacesCanvas, previewArea.X);
                Canvas.SetTop(FacesCanvas, previewArea.Y);
            }

            // Also mirror the canvas if the preview is being mirrored
            FacesCanvas.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }
        */
        /// <summary>
        /// Iterates over all detected faces, creating and adding Rectangles to the FacesCanvas as face bounding boxes
        /// </summary>
        /// <param name="faces">The list of detected faces from the FaceDetected event of the effect</param>
        private void HighlightDetectedFaces(IReadOnlyList<PersonDetected> faces)
        {
            // Remove any existing rectangles from previous events
            FacesCanvas.Children.Clear();

            // For each detected face
            for (int i = 0; i < faces.Count; i++)
            {
                // Face coordinate units are preview resolution pixels, which can be a different scale from our display resolution, so a conversion may be necessary


                Windows.UI.Xaml.Media.Imaging.BitmapImage b = pictureElement.Source as Windows.UI.Xaml.Media.Imaging.BitmapImage;
                if (b != null)
                {
                    int nWidth;
                    int nHeight;
                    double ratioBackground = backgroundVideo.ActualWidth / backgroundVideo.ActualHeight;
                    double ratioPicture = ((double)b.PixelWidth / (double)b.PixelHeight);
                    int xOffset = 0;
                    int yOffset = 0;
                    if (ratioPicture > ratioBackground)
                    {
                        nWidth = (int)backgroundVideo.ActualWidth;
                        nHeight = (int)(nWidth / ratioPicture);
                        xOffset = 0;
                        yOffset = (int) ((backgroundVideo.ActualHeight - nHeight)/ 2);
                    }
                    else
                    {
                        nHeight = (int)backgroundVideo.ActualHeight;
                        nWidth = (int)(nHeight * ratioPicture);
                        xOffset = (int) ((backgroundVideo.ActualWidth - nWidth) / 2);
                        yOffset = 0;

                    }
                    double WidthRatio = (double)nWidth/ (double)b.PixelWidth;
                    double HeightRatio = (double)nHeight / (double)b.PixelHeight;

                    Windows.UI.Xaml.Shapes.Rectangle faceBoundingBox  = new Windows.UI.Xaml.Shapes.Rectangle();
                    faceBoundingBox.Height = (uint)faces[i].rect.height*HeightRatio;
                    faceBoundingBox.Width = (uint)faces[i].rect.width*WidthRatio;
                    var X = (uint)((double)faces[i].rect.left * WidthRatio);
                    var Y = (uint)((double)faces[i].rect.top * HeightRatio);

                    Canvas.SetLeft(faceBoundingBox, X + xOffset);
                    Canvas.SetTop(faceBoundingBox, Y + yOffset);

                    // Highlight the first face in the set
                    faceBoundingBox.Stroke = (i == 0 ? new SolidColorBrush(Colors.Blue) : new SolidColorBrush(Colors.DeepSkyBlue));

                    // Add grid to canvas containing all face UI objects
                    FacesCanvas.Children.Add(faceBoundingBox);

                }

            }

            // Update the face detection bounding box canvas orientation
            //SetFacesCanvasRotation();
        }
        /// <summary>
        /// This event will fire when the page is rotated, when the DisplayInformation.AutoRotationPreferences value set in the SetupUiAsync() method cannot be not honored.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event data.</param>
        /// 

            /*
        private async void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            _displayOrientation = sender.CurrentOrientation;

            if (_previewProperties != null)
            {
                await SetPreviewRotationAsync();
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateButtonOrientation());
        }
        */
        #endregion ui


    }
}
