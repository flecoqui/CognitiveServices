//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//********************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using System.IO;

namespace SpeechClient
{
    /// <summary>
    /// Event which returns the position of the buffer ready to be sent 
    /// This event is fired with continuous recording
    /// </summary>
    public delegate void WebSocketEventHandler(SpeechClient sender, string Path, SpeechToTextResponse response);


    /// <summary>
    /// Event which returns the Audio Level of the audio samples
    /// being stored in the audio buffer
    /// </summary>
    public delegate void AudioLevelEventHandler(SpeechClient sender, double level);


    /// <summary>
    /// Event which returns the Audio Capture Errors while 
    /// a recording is in progress
    /// </summary>
    /// <returns>true if successful</returns>
    public delegate void AudioCaptureErrorEventHandler(SpeechClient sender, string message);
    public sealed class SpeechClient
    {
        public string Hostname { get; set; }
        public string AuthUrl { get; set; }
        public string HttpUrl { get; set; }
        public string WebSocketUrl { get; set; }
        public string SubscriptionKey { get; set; }
        public string CustomEndPointID { get; set; }
        public string Token { get; set; }

        private readonly TimeSpan GetTokenPeriod = TimeSpan.FromSeconds(60);
        private Windows.System.Threading.ThreadPoolTimer GetTokenPeriodicTimer;

        private bool bWebSocketReady = false;
        private string WebSocketRequestID;
        private Windows.Networking.Sockets.MessageWebSocket webSocket;
        private System.Threading.AutoResetEvent WebSocketInitializedEvent;

        private bool isRecordingInitialized;
        private bool isRecording;
        private System.Threading.CancellationTokenSource RecordingTokenSource = null;
        private System.Threading.CancellationToken RecordingToken;
        private Windows.Storage.Streams.IRandomAccessStream randomAudioStream;

        private Windows.Media.Capture.MediaCapture mediaCapture;

        private const string BingSpeechMaskAuthUrl = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";
        private const string SpeechMaskAuthUrl = "https://{0}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
        private const string SpeechMaskUrl = "https://{0}/speech/recognition/{1}/cognitiveservices/v1";
        private const string CustomSpeechMaskUrl = "https://{0}/speech/recognition/{1}/cognitiveservices/v1?cid={2}";
        private const string SpeechMaskWebSocketUrl = "wss://{0}/speech/recognition/{1}/cognitiveservices/v1";
        private const string CustomSpeechMaskWebSocketUrl = "wss://{0}/speech/recognition/{1}/cognitiveservices/v1?cid={2}";
        private readonly string apiSynthesizeString = "synthesize";
        public SpeechClient()
        {
            Hostname = string.Empty;
            SubscriptionKey = string.Empty;
            CustomEndPointID = string.Empty;
            AuthUrl = string.Empty;
            HttpUrl = string.Empty;
            WebSocketUrl = string.Empty;
            Token = string.Empty;
        }
        public SpeechClient(
            string hostname,
            string subscriptionKey,
            string customEndPointID)
        {
            Hostname = hostname;
            SubscriptionKey = subscriptionKey;
            CustomEndPointID = customEndPointID;
            if (string.Equals(hostname, "speech.platform.bing.com"))
                AuthUrl = BingSpeechMaskAuthUrl;
            else
                AuthUrl = string.Format(SpeechMaskAuthUrl, GetAzureRegion(hostname));

            HttpUrl = string.Empty;
            WebSocketUrl = string.Empty;
            Token = string.Empty;
        }
        public static IAsyncOperation<SpeechClient> CreateBingSpeechClient(string subscriptionKey)
        {
            
            return Task.Run<SpeechClient>(async () =>
            {

                if (string.IsNullOrEmpty(subscriptionKey))
                    return null;
                SpeechClient client = new SpeechClient("speech.platform.bing.com",subscriptionKey,string.Empty);

                try
                {
                    string token = await client.GetToken();
                    if (!string.IsNullOrEmpty(token))                    
                        return client;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while getting the token: " + ex.Message);
                }
                return null;
            }).AsAsyncOperation<SpeechClient>();

        }
        public static IAsyncOperation<SpeechClient> CreateSpeechClient(string Hostname, string subscriptionKey)
        {

            return Task.Run<SpeechClient>(async () =>
            {

                if ((string.IsNullOrEmpty(subscriptionKey)) || (string.IsNullOrEmpty(Hostname)))
                    return null;
                SpeechClient client = new SpeechClient(Hostname, subscriptionKey, string.Empty);

                try
                {
                    string token = await client.GetToken();
                    if (!string.IsNullOrEmpty(token))
                        return client;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while getting the token: " + ex.Message);
                }
                return null;
            }).AsAsyncOperation<SpeechClient>();

        }
        public static IAsyncOperation<SpeechClient> CreateCustomSpeechClient(string Hostname, string subscriptionKey, string CustomEndpointID )
        {

            return Task.Run<SpeechClient>(async () =>
            {

                if ((string.IsNullOrEmpty(subscriptionKey)) || (string.IsNullOrEmpty(Hostname)) || (string.IsNullOrEmpty(CustomEndpointID)))
                    return null;
                SpeechClient client = new SpeechClient(Hostname, subscriptionKey, CustomEndpointID);

                try
                {
                    string token = await client.GetToken();
                    if (!string.IsNullOrEmpty(token))
                        return client;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while getting the token: " + ex.Message);
                }
                return null;
            }).AsAsyncOperation<SpeechClient>();

        }
        public IAsyncOperation<string> GetToken()
        {
            return Task.Run<string>(async () =>
            {
                try
                {
                    Token = string.Empty;
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", SubscriptionKey);
                    Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent(String.Empty);
                    System.Diagnostics.Debug.WriteLine("Getting Token from UriTo: "+ AuthUrl);
                    Windows.Web.Http.HttpResponseMessage hrm = await hc.PostAsync(new Uri(AuthUrl), content);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                var b = await hrm.Content.ReadAsBufferAsync();
                                string result = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());
                                if (!string.IsNullOrEmpty(result))
                                {
                                    System.Diagnostics.Debug.WriteLine("Getting Token Successful from Uri: " + AuthUrl);
                                    if (GetTokenPeriodicTimer == null)
                                    {
                                        GetTokenPeriodicTimer = Windows.System.Threading.ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
                                        {
                                            string s = await GetToken();
                                        }, GetTokenPeriod);
                                    }
                                    Token = "Bearer " + result;
                                    return Token;
                                }
                                break;

                            default:
                                System.Diagnostics.Debug.WriteLine("Getting Token failed Http Response Error:" + hrm.StatusCode.ToString() + " reason: " + hrm.ReasonPhrase.ToString());
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while getting the token: " + ex.Message);
                }
                return string.Empty;
            }).AsAsyncOperation<string>();

        }


        /// <summary>
        /// Event which returns the event from the WebSocket
        /// This event is fired with WebSocket event
        /// </summary>
        public event WebSocketEventHandler WebSocketEvent;


        /// <summary>
        /// Event which returns the Audio Level of the audio samples
        /// being stored in the audio buffer
        /// </summary>
        public event AudioLevelEventHandler AudioLevel;

        /// <summary>
        /// Event which returns the Audio Capture Errors while 
        /// a recording is in progress
        /// </summary>
        /// <returns>true if successful</returns>
        public event AudioCaptureErrorEventHandler AudioCaptureError;
        /// <summary>
        /// SendStorageFile method
        /// </summary>
        /// <param name="wavFile">StorageFile associated with the audio file which 
        /// will be sent to the SpeechToText Services.
        /// </param>
        /// <param name="speechAPI">API mode : conversation, interactive, dictation .
        /// </param>        
        /// <param name="language">language associated with the current buffer/recording.
        /// for instance en-US, fr-FR, pt-BR, ...
        /// </param>
        /// <param name="resulttype">Result type: simple or detailed.
        /// </param>        
        /// <return>The result of the SpeechToText REST API.
        /// </return>
        public IAsyncOperation<SpeechToTextResponse> SendStorageFile(Windows.Storage.StorageFile wavFile, string speechAPI, string language, string resulttype)
        {
            return Task.Run<SpeechToTextResponse>(async () =>
            {
                SpeechToTextResponse r = null;
                int loop = 1;

                while (loop-- > 0)
                {
                    try
                    {

                        string speechUrl = string.Empty;
                        if (string.Equals(Hostname, "speech.platform.bing.com") || (string.IsNullOrEmpty(CustomEndPointID)))
                            speechUrl = string.Format(SpeechMaskUrl, Hostname, speechAPI) + "?language=" + language + "&format=" + resulttype;
                        else
                            speechUrl = string.Format(CustomSpeechMaskUrl, Hostname, speechAPI, CustomEndPointID) + "&language=" + language + "&format=" + resulttype;
                        Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                        hc.DefaultRequestHeaders.TryAppendWithoutValidation("Authorization", Token);
                        hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "audio/wav; codec=\"audio/pcm\"; samplerate=16000");
                        hc.DefaultRequestHeaders.TryAppendWithoutValidation("Accept", "application/json;text/xml");
                        hc.DefaultRequestHeaders.TryAppendWithoutValidation("Transfer-Encoding", "chunked");
                        hc.DefaultRequestHeaders.TryAppendWithoutValidation("Expect", "100-continue");
                        Windows.Web.Http.HttpResponseMessage hrm = null;

                        Windows.Storage.StorageFile file = wavFile;
                        if (file != null)
                        {
                            using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                            {
                                if (fileStream != null)
                                {

                                    Windows.Web.Http.HttpStreamContent content = new Windows.Web.Http.HttpStreamContent(fileStream.GetInputStreamAt(0));
                                    //                content.Headers.ContentLength = STTStream.GetLength();
                                    //              System.Diagnostics.Debug.WriteLine("REST API Post Content Length: " + content.Headers.ContentLength.ToString() + " bytes");
                                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                                    hrm = await hc.PostAsync(new Uri(speechUrl), content).AsTask(cts.Token, progress);

                                }
                            }
                        }
                        if (hrm != null)
                        {
                            switch (hrm.StatusCode)
                            {
                                case Windows.Web.Http.HttpStatusCode.Ok:
                                    var b = await hrm.Content.ReadAsBufferAsync();
                                    string result = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());
                                    if (!string.IsNullOrEmpty(result))
                                        r = new SpeechToTextResponse(result, null);
                                    break;

                                case Windows.Web.Http.HttpStatusCode.Forbidden:
                                    string token = await GetToken();
                                    if (string.IsNullOrEmpty(token))
                                    {
                                        loop++;
                                    }
                                    break;

                                default:
                                    int code = (int)hrm.StatusCode;
                                    string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                    System.Diagnostics.Debug.WriteLine(HttpError);
                                    r = new SpeechToTextResponse(string.Empty, HttpError);
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                    }
                }
                return r;
            }).AsAsyncOperation<SpeechToTextResponse>();
        }
        /// <summary>
        /// SendStorageFileOverWebSocket method
        /// </summary>
        /// <param name="wavFile">StorageFile associated with the audio file which 
        /// will be sent to the SpeechToText Services.
        /// </param>
        /// <param name="speechAPI">API mode : conversation, interactive, dictation .
        /// </param>        
        /// <param name="language">language associated with the current buffer/recording.
        /// for instance en-US, fr-FR, pt-BR, ...
        /// </param>
        /// <param name="resulttype">Result type: simple or detailed.
        /// </param>        
        /// <return>The result of the SpeechToText REST API.
        /// </return>
        public IAsyncOperation<bool> SendStorageFileOverWebSocket(Windows.Storage.StorageFile wavFile, string speechAPI, string language, string resulttype)
        {
            return Task.Run<bool>(async () =>
            {
                try
                {                    
                    string speechUrl = string.Empty;
                    if (string.Equals(Hostname, "speech.platform.bing.com") || (string.IsNullOrEmpty(CustomEndPointID)))
                        speechUrl = string.Format(SpeechMaskWebSocketUrl, Hostname, speechAPI) + "?language=" + language + "&format=" + resulttype;
                    else
                        speechUrl = string.Format(CustomSpeechMaskWebSocketUrl, Hostname, speechAPI, CustomEndPointID) + "&language=" + language + "&format=" + resulttype;
                    CreateWebSocket();
                    if (webSocket != null)
                    {
                        webSocket.SetRequestHeader("Authorization", Token);
                        webSocket.SetRequestHeader("X-ConnectionId", Guid.NewGuid().ToString("N"));
                        await webSocket.ConnectAsync(new Uri(speechUrl));
                        // ResetEvent for synchronization
                        if (WebSocketInitializedEvent == null)
                            WebSocketInitializedEvent = new System.Threading.AutoResetEvent(false);
                        else
                            WebSocketInitializedEvent.Reset();
                        System.Diagnostics.Debug.WriteLine("Sending Speech Config");
                        bWebSocketReady = true;
                        if (await SendSpeechConfig() == true)
                        {
                            System.Diagnostics.Debug.WriteLine("Sending Speech Header");
                            if (await SendSpeechFileHeader(wavFile) == true)
                            {
                                await System.Threading.Tasks.Task.Factory.StartNew(async () =>
                                {
                               // wait for the reception of Start message before sending the Wav file
                               if (WebSocketInitializedEvent.WaitOne(5000))
                                    {
                                        System.Diagnostics.Debug.WriteLine("Sending Speech Stream");
                                        await SendSpeechFile(wavFile);
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("Message Start not received after 5000 ms");
                                    }
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending Speech Config and WAV file: " + ex.Message);
                }
                return false;
            }).AsAsyncOperation<bool>();
        }
        /// <summary>
        /// StartRecording method
        /// </summary>
        /// <param>
        /// Start to record audio .
        /// The audio stream is still in stored in memory
        /// </param>
        /// <return>return true if successful.
        /// </return>
        public IAsyncOperation<bool> StartRecording(string speechAPI, string language, string resulttype)
        {
            return Task.Run<bool>(async () =>
            {
                bool bResult = false;
                if (isRecordingInitialized != true)
                    await InitializeRecording();
                if (randomAudioStream != null)
                {
                    randomAudioStream.Dispose();
                    randomAudioStream = null;
                }
                randomAudioStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                if ((randomAudioStream != null) && (isRecordingInitialized == true))
                {
                    try
                    {
                        Windows.Media.MediaProperties.MediaEncodingProfile MEP = Windows.Media.MediaProperties.MediaEncodingProfile.CreateWav(Windows.Media.MediaProperties.AudioEncodingQuality.Auto);
                        if (MEP != null)
                        {
                            if (MEP.Audio != null)
                            {
                                uint framerate = 16000;
                                uint bitsPerSample = 16;
                                uint numChannels = 1;
                                uint bytespersecond = 32000;
                                MEP.Audio.Properties[WAVAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND] = framerate;
                                MEP.Audio.Properties[WAVAttributes.MF_MT_AUDIO_NUM_CHANNELS] = numChannels;
                                MEP.Audio.Properties[WAVAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE] = bitsPerSample;
                                MEP.Audio.Properties[WAVAttributes.MF_MT_AUDIO_AVG_BYTES_PER_SECOND] = bytespersecond;
                                foreach (var Property in MEP.Audio.Properties)
                                {
                                    System.Diagnostics.Debug.WriteLine("Property: " + Property.Key.ToString());
                                    System.Diagnostics.Debug.WriteLine("Value: " + Property.Value.ToString());
                                    if (Property.Key == new Guid("5faeeae7-0290-4c31-9e8a-c534f68d9dba"))
                                        framerate = (uint)Property.Value;
                                    if (Property.Key == new Guid("f2deb57f-40fa-4764-aa33-ed4f2d1ff669"))
                                        bitsPerSample = (uint)Property.Value;
                                    if (Property.Key == new Guid("37e48bf5-645e-4c5b-89de-ada9e29b696a"))
                                        numChannels = (uint)Property.Value;

                                }
                            }
                            if (MEP.Container != null)
                            {
                                foreach (var Property in MEP.Container.Properties)
                                {
                                    System.Diagnostics.Debug.WriteLine("Property: " + Property.Key.ToString());
                                    System.Diagnostics.Debug.WriteLine("Value: " + Property.Value.ToString());
                                }
                            }
                        }
                        await mediaCapture.StartRecordToStreamAsync(MEP, randomAudioStream);
                        bResult = true;
                        isRecording = true;
                        if (RecordingTokenSource != null)
                            RecordingTokenSource.Cancel();
                        RecordingTokenSource = new System.Threading.CancellationTokenSource();
                        RecordingToken = RecordingTokenSource.Token;

                        var t = Task.Run(async () =>
                        {
                            try
                            {
                                string speechUrl = string.Empty;
                                if (string.Equals(Hostname, "speech.platform.bing.com") || (string.IsNullOrEmpty(CustomEndPointID)))
                                    speechUrl = string.Format(SpeechMaskWebSocketUrl, Hostname, speechAPI) + "?language=" + language + "&format=" + resulttype;
                                else
                                    speechUrl = string.Format(CustomSpeechMaskWebSocketUrl, Hostname, speechAPI, CustomEndPointID) + "&language=" + language + "&format=" + resulttype;
                                CreateWebSocket();

                                webSocket.SetRequestHeader("Authorization", Token);
                                webSocket.SetRequestHeader("X-ConnectionId", Guid.NewGuid().ToString("N"));
                                await webSocket.ConnectAsync(new Uri(speechUrl));
                                // ResetEvent for synchronization
                                if (WebSocketInitializedEvent == null)
                                    WebSocketInitializedEvent = new System.Threading.AutoResetEvent(false);
                                else
                                    WebSocketInitializedEvent.Reset();
                                System.Diagnostics.Debug.WriteLine("Sending Speech Config");
                                bWebSocketReady = true;
                                if (await SendSpeechConfig() == true)
                                {
                                    System.Diagnostics.Debug.WriteLine("Sending Speech Header");
                                    if (await SendSpeechStreamHeader(randomAudioStream) == true)
                                    {
                                        // wait for the reception of Start message before sending the Wav file
                                        if (WebSocketInitializedEvent.WaitOne(5000))
                                        {
                                            System.Diagnostics.Debug.WriteLine("Sending Speech File");
                                            await SendSpeechStream(randomAudioStream, RecordingToken);
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine("Message Start not received after 5000 ms");
                                            return;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Exception while sending Speech Config and WAV file: " + ex.Message);
                            }
                        }
                        , RecordingToken);
                        System.Diagnostics.Debug.WriteLine("Recording in audio stream...");
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception while recording in audio stream:" + e.Message);
                    }
                }
                return bResult;
            }).AsAsyncOperation<bool>();
        }
        /// <summary>
        /// StartRecordingInMemory method
        /// </summary>
        /// <param>
        /// Start to record audio in memory .
        /// The audio stream is still in stored in memory
        /// </param>
        /// <return>return true if successful.
        /// </return>
        public IAsyncOperation<bool> StartRecordingInMemory()
        {
            return Task.Run<bool>(async () =>
            {
                bool bResult = false;
                if (isRecordingInitialized != true)
                    await InitializeRecording();
                if (randomAudioStream != null)
                {
                    randomAudioStream.Dispose();
                    randomAudioStream = null;
                }
                randomAudioStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                if ((randomAudioStream != null) && (isRecordingInitialized == true))
                {
                    try
                    {
                        Windows.Media.MediaProperties.MediaEncodingProfile MEP = Windows.Media.MediaProperties.MediaEncodingProfile.CreateWav(Windows.Media.MediaProperties.AudioEncodingQuality.Auto);
                        if (MEP != null)
                        {
                            if (MEP.Audio != null)
                            {
                                uint framerate = 16000;
                                uint bitsPerSample = 16;
                                uint numChannels = 1;
                                uint bytespersecond = 32000;
                                MEP.Audio.Properties[WAVAttributes.MF_MT_AUDIO_SAMPLES_PER_SECOND] = framerate;
                                MEP.Audio.Properties[WAVAttributes.MF_MT_AUDIO_NUM_CHANNELS] = numChannels;
                                MEP.Audio.Properties[WAVAttributes.MF_MT_AUDIO_BITS_PER_SAMPLE] = bitsPerSample;
                                MEP.Audio.Properties[WAVAttributes.MF_MT_AUDIO_AVG_BYTES_PER_SECOND] = bytespersecond;
                                foreach (var Property in MEP.Audio.Properties)
                                {
                                    System.Diagnostics.Debug.WriteLine("Property: " + Property.Key.ToString());
                                    System.Diagnostics.Debug.WriteLine("Value: " + Property.Value.ToString());
                                    if (Property.Key == new Guid("5faeeae7-0290-4c31-9e8a-c534f68d9dba"))
                                        framerate = (uint)Property.Value;
                                    if (Property.Key == new Guid("f2deb57f-40fa-4764-aa33-ed4f2d1ff669"))
                                        bitsPerSample = (uint)Property.Value;
                                    if (Property.Key == new Guid("37e48bf5-645e-4c5b-89de-ada9e29b696a"))
                                        numChannels = (uint)Property.Value;

                                }
                            }
                            if (MEP.Container != null)
                            {
                                foreach (var Property in MEP.Container.Properties)
                                {
                                    System.Diagnostics.Debug.WriteLine("Property: " + Property.Key.ToString());
                                    System.Diagnostics.Debug.WriteLine("Value: " + Property.Value.ToString());
                                }
                            }
                        }
                        await mediaCapture.StartRecordToStreamAsync(MEP, randomAudioStream);
                        var t = Task.Run(async () =>
                        {
                            try
                            {
                                await AnalysingSpeechStream(randomAudioStream, RecordingToken);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Exception while analysing Speech Config and WAV file: " + ex.Message);
                            }
                        }, RecordingToken);
                        bResult = true;
                        isRecording = true;
                        System.Diagnostics.Debug.WriteLine("Recording in audio stream...");
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception while recording in audio stream:" + e.Message);
                    }
                }
                return bResult;
            }).AsAsyncOperation<bool>();
        }

        /// <summary>
        /// SendMemoryBuffer method
        /// This method sends the current audio buffer towards Cognitive Services REST API
        /// </summary>
        /// <param name="locale">language associated with the current buffer/recording.
        /// for instance en-US, fr-FR, pt-BR, ...
        /// </param>
        /// <return>The result of the SpeechToText REST API.
        /// </return>
        public IAsyncOperation<SpeechToTextResponse> SendMemoryBuffer(string speechAPI, string language, string resulttype)
        {
            return Task.Run<SpeechToTextResponse>(async () =>
            {
                SpeechToTextResponse r = null;
                int loop = 1;

                while (loop-- > 0)
                {
                    try
                    {
                        string speechUrl = string.Empty;
                        if (string.Equals(Hostname, "speech.platform.bing.com") || (string.IsNullOrEmpty(CustomEndPointID)))
                            speechUrl = string.Format(SpeechMaskUrl, Hostname, speechAPI) + "?language=" + language + "&format=" + resulttype;
                        else
                            speechUrl = string.Format(CustomSpeechMaskUrl, Hostname, speechAPI, CustomEndPointID) + "&language=" + language + "&format=" + resulttype;

                           Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();
                        System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                        hc.DefaultRequestHeaders.TryAppendWithoutValidation("Authorization", Token);
                        hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "audio/wav; codec=\"audio/pcm\"; samplerate=16000");
                        hc.DefaultRequestHeaders.TryAppendWithoutValidation("Accept", "application/json;text/xml");
                        hc.DefaultRequestHeaders.TryAppendWithoutValidation("Transfer-Encoding", "chunked");
                        hc.DefaultRequestHeaders.TryAppendWithoutValidation("Expect", "100-continue");

                        Windows.Web.Http.HttpResponseMessage hrm = null;
                        Windows.Web.Http.HttpStreamContent content = null;
                        if (randomAudioStream != null)
                        {
                            var InputStream = randomAudioStream.GetInputStreamAt(0);
                            content = new Windows.Web.Http.HttpStreamContent(InputStream);
                            //content.Headers.ContentLength = STTStream.GetLength();
                            //System.Diagnostics.Debug.WriteLine("REST API Post Content Length: " + content.Headers.ContentLength.ToString());
                            content.Headers.TryAppendWithoutValidation("ContentType", "audio/wav; codec=\"audio/pcm\"; samplerate=16000");
                            IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                            hrm = await hc.PostAsync(new Uri(speechUrl), content).AsTask(cts.Token, progress);
                        }
                        if (hrm != null)
                        {
                            switch (hrm.StatusCode)
                            {
                                case Windows.Web.Http.HttpStatusCode.Ok:
                                    var b = await hrm.Content.ReadAsBufferAsync();
                                    string result = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());
                                    if (!string.IsNullOrEmpty(result))
                                        r = new SpeechToTextResponse(result, null);
                                    break;

                                case Windows.Web.Http.HttpStatusCode.Forbidden:
                                    string token = await GetToken();
                                    if (string.IsNullOrEmpty(token))
                                    {
                                        loop++;
                                    }
                                    break;

                                default:
                                    int code = (int)hrm.StatusCode;
                                    string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                    System.Diagnostics.Debug.WriteLine(HttpError);
                                    r = new SpeechToTextResponse(string.Empty, HttpError);
                                    break;
                            }
                        }
                    }
                    catch (System.Threading.Tasks.TaskCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine("http POST canceled");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("http POST exception: " + ex.Message);
                    }
                    finally
                    {
                        System.Diagnostics.Debug.WriteLine("http POST done");
                    }
                }
                return r;
            }).AsAsyncOperation<SpeechToTextResponse>();
        }
        /// <summary>
        /// SendMemoryBufferOverWebSocket method
        /// </summary>
        /// <param name="speechAPI">API mode : conversation, interactive, dictation .
        /// </param>        
        /// <param name="language">language associated with the current buffer/recording.
        /// for instance en-US, fr-FR, pt-BR, ...
        /// </param>
        /// <param name="resulttype">Result type: simple or detailed.
        /// </param>        
        /// <return>The result of the SpeechToText REST API.
        /// </return>
        public IAsyncOperation<bool> SendMemoryBufferOverWebSocket(string speechAPI, string language, string resulttype)
        {
            return Task.Run<bool>(async () =>
            {
                try
                {
                    string speechUrl = string.Empty;
                    if (string.Equals(Hostname, "speech.platform.bing.com") || (string.IsNullOrEmpty(CustomEndPointID)))
                        speechUrl = string.Format(SpeechMaskWebSocketUrl, Hostname, speechAPI) + "?language=" + language + "&format=" + resulttype;
                    else
                        speechUrl = string.Format(CustomSpeechMaskWebSocketUrl, Hostname, speechAPI, CustomEndPointID) + "&language=" + language + "&format=" + resulttype;
                    CreateWebSocket();
                    if (webSocket != null)
                    {
                        webSocket.SetRequestHeader("Authorization", Token);
                        webSocket.SetRequestHeader("X-ConnectionId", Guid.NewGuid().ToString("N"));
                        await webSocket.ConnectAsync(new Uri(speechUrl));
                        // ResetEvent for synchronization
                        if (WebSocketInitializedEvent == null)
                            WebSocketInitializedEvent = new System.Threading.AutoResetEvent(false);
                        else
                            WebSocketInitializedEvent.Reset();
                        System.Diagnostics.Debug.WriteLine("Sending Speech Config");
                        bWebSocketReady = true;
                        if (RecordingTokenSource != null)
                            RecordingTokenSource.Cancel();
                        RecordingTokenSource = new System.Threading.CancellationTokenSource();
                        RecordingToken = RecordingTokenSource.Token;

                        if (await SendSpeechConfig() == true)
                        {
                            System.Diagnostics.Debug.WriteLine("Sending Speech Header");
                            if (await SendSpeechStreamHeader(randomAudioStream) == true)
                            {
                                await System.Threading.Tasks.Task.Factory.StartNew(async () =>
                                {
                                    // wait for the reception of Start message before sending the Wav file
                                    if (WebSocketInitializedEvent.WaitOne(5000))
                                    {
                                        System.Diagnostics.Debug.WriteLine("Sending Speech Stream");
                                        await SendSpeechStream(randomAudioStream, RecordingToken);
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine("Message Start not received after 5000 ms");
                                    }
                                });
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending Speech Config and WAV file: " + ex.Message);
                }
                return false;
            }).AsAsyncOperation<bool>();
        }

        /// <summary>
        /// StopRecording method
        /// </summary>
        /// <param>
        /// Stop to record audio .
        /// The audio stream is still in stored in memory
        /// </param>
        /// <return>return true if successful.
        /// </return>
        public IAsyncOperation<bool> StopRecording()
        {
            return Task.Run<bool>(async () =>
            {
                // Stop recording and dispose resources
                if (mediaCapture != null)
                {
                    if (isRecording == true)
                    {
                        try
                        {
                            await mediaCapture.StopRecordAsync();
                            if (RecordingTokenSource != null)
                                RecordingTokenSource.Cancel();
                        }
                        catch (Exception)
                        {

                        }
                        isRecording = false;
                    }
                }
                return true;
            }).AsAsyncOperation<bool>();
        }
        /// <summary>
        /// IsRecording method
        /// </summary>
        /// <return>Return true if the Client is currently recording
        /// </return>
        public bool IsRecording()
        {
            return isRecording;
        }
        /// <summary>
        /// Cleans up the microphone resources, Web Socket, PeriodicTimer and the stream and unregisters from MediaCapture events
        /// </summary>
        /// <returns>true if successful</returns>
        public IAsyncOperation<bool> Cleanup()
        {
            return Task.Run<bool>(async () =>
            {
                try
                {
                    if (isRecordingInitialized)
                    {
                        // If a recording is in progress during cleanup, stop it to save the recording
                        if (isRecording)
                        {
                            await StopRecording();
                        }
                        isRecordingInitialized = false;
                    }

                    if (mediaCapture != null)
                    {
                        mediaCapture.RecordLimitationExceeded -= MediaCapture_RecordLimitationExceeded;
                        mediaCapture.Failed -= MediaCapture_Failed;
                        mediaCapture.Dispose();
                        mediaCapture = null;
                    }
                    if (GetTokenPeriodicTimer != null)
                    {
                        GetTokenPeriodicTimer.Cancel();
                        GetTokenPeriodicTimer = null;
                    }
                    if (webSocket != null)
                        CleanupWebSocket();
                    if (randomAudioStream != null)
                    {
                        randomAudioStream.Dispose();
                        randomAudioStream = null;
                    }
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while cleaning up the SpeechClient: " + ex.Message);
                }
                return true;
            }).AsAsyncOperation<bool>();
        }
        /// <summary>
        /// SaveBuffer method
        /// </summary>
        /// <param name="wavFile">StorageFile where the audio buffer 
        /// will be stored.
        /// </param>
        /// <param name="start">the position in the buffer of the first byte to save in a file. 
        /// by default the value is 0.
        /// </param>
        /// <param name="end">the position in the buffer of the last byte to save in a file
        /// by default the value is 0, if the value is 0 the whole buffer will be stored in a a file
        /// </param>
        /// <return>true if successful.
        /// </return>
        public IAsyncOperation<bool> SaveMemoryBuffer(Windows.Storage.StorageFile wavFile)
        {
            return Task.Run<bool>(async () =>
            {
                bool bResult = false;
                if (wavFile != null)
                {
                    try
                    {
                        using (System.IO.Stream stream = await wavFile.OpenStreamForWriteAsync())
                        {
                            if ((stream != null) && (randomAudioStream != null))
                            {
                                randomAudioStream.Seek(0);
                                stream.SetLength(0);
                                await randomAudioStream.AsStream().CopyToAsync(stream);
                                System.Diagnostics.Debug.WriteLine("Audio Stream stored in: " + wavFile.Path);
                                bResult = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Exception while saving the Audio Stream stored in: " + wavFile.Path + " Exception: " + ex.Message);

                    }
                }
                return bResult;
            }).AsAsyncOperation<bool>();
        }
        /// <summary>
        /// GetBufferLength method
        /// Return the length of the current audio buffer
        /// </summary>
        /// <return>the length of the WAV buffer in ulong.
        /// </return>
        public ulong GetBufferLength()
        {
            if (randomAudioStream != null)
            {
                return randomAudioStream.Size;
            }
            return 0;
        }
        /// <summary>
        /// ClearToken method
        /// </summary>
        /// <return>true.
        /// </return>
        public bool ClearToken()
        {
            Token = String.Empty;
            return true;
        }

        /// <summary>
        /// TextToSpeech method
        /// </summary>
        /// <param>
        /// text to convert to speech
        /// lang language of the text ("en-us", "fr-fr")
        /// gender for the voice ('female" or "male")
        /// </param>
        /// <return>The audio stream
        /// </return>
        public IAsyncOperation<Windows.Storage.Streams.IInputStream> TextToSpeech(string text, string lang, string gender)
        {
            return Task.Run<Windows.Storage.Streams.IInputStream>(async () =>
            {
                if (string.IsNullOrEmpty(Token))
                    return null;
                try
                {
                    Windows.Web.Http.Filters.HttpBaseProtocolFilter httpBaseProtocolFilter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
                    httpBaseProtocolFilter.CacheControl.ReadBehavior = Windows.Web.Http.Filters.HttpCacheReadBehavior.MostRecent;
                    httpBaseProtocolFilter.CacheControl.WriteBehavior = Windows.Web.Http.Filters.HttpCacheWriteBehavior.NoCache;

                    httpBaseProtocolFilter.CookieUsageBehavior = Windows.Web.Http.Filters.HttpCookieUsageBehavior.NoCookies;
                    httpBaseProtocolFilter.AutomaticDecompression = false;


                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient(httpBaseProtocolFilter);


                    string speechUrl = string.Empty;
                    if (string.Equals(Hostname, "speech.platform.bing.com"))
                        speechUrl = "https://" + Hostname + "/" + apiSynthesizeString;
                    else
                        speechUrl = "https://" + Hostname.Replace("stt", "tts") + "/cognitiveservices/v1";
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    hc.DefaultRequestHeaders.Clear();
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Authorization", Token);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("X-Search-AppId", "07D3234E49CE426DAA29772419F436CA");
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("X-Search-ClientID", "1ECFAE91408841A480F00935DC390960");
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("User-Agent", "TTSClient");
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Expect", "100-continue");

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("X-Microsoft-OutputFormat", "riff-16khz-16bit-mono-pcm");
                    hc.DefaultRequestHeaders.Connection.Clear();



                    // string TextToSpeechContent = "<speak version=\"1.0\" xml:lang=\"{0}\"><voice xml:lang=\"{1}\" xml:gender=\"{2}\" name=\"Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)\">{3}</voice></speak>";
                    //  string contentString = String.Format(TextToSpeechContent, lang, lang, gender, text);
                    string contentString = GenerateSsml(lang, gender, GetVoiceName(lang, gender), text);
                    Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent(contentString);
                    content.Headers.Clear();
                    content.Headers.Remove("Content-Type");
                    content.Headers.TryAppendWithoutValidation("Content-Type", "application/ssml+xml");



                    Windows.Web.Http.HttpResponseMessage hrm = await hc.PostAsync(new Uri(speechUrl), content);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                return await hrm.Content.ReadAsInputStreamAsync();
                            default:
                                System.Diagnostics.Debug.WriteLine("Http Response Error:" + hrm.StatusCode.ToString() + " reason: " + hrm.ReasonPhrase.ToString());
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while getting speech from text: " + ex.Message);
                }
                return null;
            }).AsAsyncOperation<Windows.Storage.Streams.IInputStream>();
        }
        #region TextToSpeech
        /// <summary>
        /// Generates SSML.
        /// </summary>
        /// <param name="locale">The locale.</param>
        /// <param name="gender">The gender.</param>
        /// <param name="name">The voice name.</param>
        /// <param name="text">The text input.</param>
        private string GenerateSsml(string locale, string gender, string name, string text)
        {
            var ssmlDoc = new XDocument(
                              new XElement("speak",
                                  new XAttribute("version", "1.0"),
                                  new XAttribute(XNamespace.Xml + "lang", "en-US"),
                                  new XElement("voice",
                                      new XAttribute(XNamespace.Xml + "lang", locale),
                                      new XAttribute(XNamespace.Xml + "gender", gender),
                                      new XAttribute("name", name),
                                      text)));
            return ssmlDoc.ToString();
        }

        string GetVoiceName(string lang, string gender)
        {
            string voiceName = "Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)";

            switch (lang.ToLower())
            {
                case "ar-eg":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (ar-EG, Hoda)";
                    break;
                case "ar-sa":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (ar-SA, Naayf)";
                    break;
                case "bg-bg":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (bg-BG, Ivan)";
                    break;
                case "ca-es":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (ca-ES, HerenaRUS)";
                    break;
                case "cs-cz":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (cs-CZ, Jakub)";
                    break;
                case "da-dk":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (da-DK, HelleRUS)";
                    break;
                case "de-at":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (de-AT, Michael)";
                    break;
                case "de-ch":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (de-CH, Karsten)";
                    break;

                case "de-de":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (de-DE, Hedda)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (de-DE, Stefan, Apollo)";
                    break;
                case "el-gr":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (el-GR, Stefanos)";
                    break;
                case "en-au":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (en-AU, Catherine)";
                    break;
                case "en-ca":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (en-CA, Linda)";
                    break;
                case "en-gb":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (en-GB, Susan, Apollo)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (en-GB, George, Apollo)";
                    break;
                case "en-ie":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (en-IE, Sean)";
                    break;


                case "en-in":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (en-IN, Ravi, Apollo)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (en-IN, Ravi, Apollo)";
                    break;
                case "en-us":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)";
                    break;
                case "es-es":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (es-ES, Pablo, Apollo)";
                    break;
                case "es-mx":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (es-MX, HildaRUS)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (es-MX, Raul, Apollo)";
                    break;
                case "fi-fi":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (fi-FI, HeidiRUS)";
                    break;

                case "fr-ca":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (fr-CA, Caroline)";
                    break;
                case "fr-ch":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (fr-CH, Guillaume)";
                    break;
                case "fr-fr":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (fr-FR, Julie, Apollo)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (fr-FR, Paul, Apollo)";
                    break;
                case "he-il":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (he-IL, Asaf)";
                    break;
                case "hi-in":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (hi-IN, Kalpana, Apollo)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (hi-IN, Hemant)";
                    break;
                case "hr-hr":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (hr-HR, Matej)";
                    break;
                case "hu-hu":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (hu-HU, Szabolcs)";
                    break;
                case "id-id":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (id-ID, Andika)";
                    break;

                case "it-it":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (it-IT, Cosimo, Apollo)";
                    break;
                case "ja-jp":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (ja-JP, Ayumi, Apollo)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (ja-JP, Ichiro, Apollo)";
                    break;
                case "ko-kr":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (ko-KR, HeamiRUS)";
                    break;
                case "ms-my":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (ms-MY, Rizwan)";
                    break;
                case "nb-no":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (nb-NO, HuldaRUS)";
                    break;
                case "nl-nl":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (nl-NL, HannaRUS)";
                    break;
                case "ro-ro":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (ro-RO, Andrei)";
                    break;

                case "pt-br":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (pt-BR, HeloisaRUS)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (pt-BR, Daniel, Apollo)";
                    break;
                case "ru-ru":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (ru-RU, Irina, Apollo)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (ru-RU, Pavel, Apollo)";
                    break;
                case "sk-sk":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (sk-SK, Filip)";
                    break;
                case "sl-si":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (sl-SI, Lado)";
                    break;
                case "sv-se":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (sv-SE, HedvigRUS)";
                    break;
                case "ta-in":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (ta-IN, Valluvar)";
                    break;
                case "th-th":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (th-TH, Pattara)";
                    break;
                case "tr-tr":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (tr-TR, SedaRUS)";
                    break;
                case "vi-vn":
                    voiceName = "Microsoft Server Speech Text to Speech Voice (vi-VN, An)";
                    break;

                case "zh-cn":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (zh-CN, HuihuiRUS)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (zh-CN, Kangkang, Apollo)";
                    break;
                case "zh-hk":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (zh-HK, Tracy, Apollo)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (zh-HK, Danny, Apollo)";
                    break;
                case "zh-tw":
                    if (gender == "Female")
                        voiceName = "Microsoft Server Speech Text to Speech Voice (zh-TW, Yating, Apollo)";
                    else
                        voiceName = "Microsoft Server Speech Text to Speech Voice (zh-TW, Zhiwei, Apollo)";
                    break;
            }
            return voiceName;

        }

        #endregion
        #region WebSocket
        async System.Threading.Tasks.Task<bool> SendSpeechConfig()
        {
            bool result = false;

            try
            {


                //Create a request id that is unique for this 
                webSocket.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Utf8;

                // RequestID for the session
                WebSocketRequestID = Guid.NewGuid().ToString("N");

                //Send the first message after connecting to the websocket with required headers
                string payload =
                 "Path: speech.config" + Environment.NewLine +
                 "x-timestamp: " + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK") + Environment.NewLine +
                 "content-type: application/json; charset=utf-8" + Environment.NewLine + Environment.NewLine +
                 "{ \"context\":{ \"system\":{ \"version\":\"1.0.00000\"},\"os\":{ \"platform\":\"Unity\",\"name\":\"Unity\",\"version\":\"\"},\"device\":{ \"manufacturer\":\"SpeechSample\",\"model\":\"UnitySpeechSample\",\"version\":\"1.0.00000\"} } }";

                //"{\"context\": {\"system\": { \"version\":  \"2.0.12341\"},\"os\": {\"platform\": \"Windows\",\"name\": \"Windows 10\",\"version\": \"10.0.0.0\" },\"device\": {\"manufacturer\": \"Contoso\",\"model\": \"Fabrikan\",\"version\": \"7.341\"}}}}";

                System.Diagnostics.Debug.Write(payload);
                using (var dataWriter = new Windows.Storage.Streams.DataWriter(webSocket.OutputStream))
                {
                    dataWriter.WriteString(payload);
                    await dataWriter.StoreAsync();
                    dataWriter.DetachStream();
                }
                result = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending Speech Config: " + ex.Message);
            }
            return result;
        }
        byte[] CreateAudioWebSocketHeader()
        {
            var outputBuilder = new System.Text.StringBuilder();
            outputBuilder.Append("path:audio\r\n");
            outputBuilder.Append("x-requestid:" + WebSocketRequestID + "\r\n");
            outputBuilder.Append("x-timestamp:" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK") + "\r\n");
            outputBuilder.Append("content-type:audio/x-wav\r\n");

            return System.Text.Encoding.ASCII.GetBytes(outputBuilder.ToString());
        }
        byte[] CreateAudioWebSocketHeaderLength(byte[] header)
        {
            var str = "0x" + (header.Length).ToString("X");
            var headerHeadBytes = BitConverter.GetBytes((UInt16)header.Length);
            var isBigEndian = !BitConverter.IsLittleEndian;
            return !isBigEndian ? new byte[] { headerHeadBytes[1], headerHeadBytes[0] } : new byte[] { headerHeadBytes[0], headerHeadBytes[1] };
        }
        async System.Threading.Tasks.Task<bool> SendSpeechFile(Windows.Storage.StorageFile wavFile)
        {
            bool result = false;
            try
            {
                using (var fileStream = await wavFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    int Len = GetWAVHeaderLength(fileStream);
                    if (Len > 0)
                    {
                        byte[] byteArray = new byte[fileStream.Size - (ulong)Len];
                        if (byteArray != null)
                        {

                            fileStream.Seek((ulong)Len);
                            fileStream.ReadAsync(byteArray.AsBuffer(), (uint)(fileStream.Size - (ulong)Len), Windows.Storage.Streams.InputStreamOptions.Partial).AsTask().Wait();

                            int index = 0;
                            while (index < (int)(fileStream.Size - (ulong)Len))
                            {
                                var headerBytes = CreateAudioWebSocketHeader();
                                var headerHead = CreateAudioWebSocketHeaderLength(headerBytes);

                                var length = Math.Min(4096 * 2 - headerBytes.Length - 8, byteArray.Length - index); //8bytes for the chunk header

                                var chunkHeader = System.Text.Encoding.ASCII.GetBytes("data").Concat(BitConverter.GetBytes((UInt32)length)).ToArray();

                                byte[] dataArray = new byte[length];
                                Array.Copy(byteArray, index, dataArray, 0, length);

                                index += length;

                                var arr = headerHead.Concat(headerBytes).Concat(chunkHeader).Concat(dataArray).ToArray();


                                if ((webSocket != null) && (bWebSocketReady))
                                {
                                    //Create a request id that is unique for this 
                                    webSocket.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Binary;
                                    using (var dataWriter = new Windows.Storage.Streams.DataWriter(webSocket.OutputStream))
                                    {
                                        //  System.Diagnostics.Debug.Write(DumpHex(arr));
                                        dataWriter.WriteBytes(arr);
                                        await dataWriter.StoreAsync();
                                        await dataWriter.FlushAsync();
                                        dataWriter.DetachStream();
                                    }
                                }
                                else
                                    break;
                            }
                        }
                    }
                }
                {
                    var headerBytes = CreateAudioWebSocketHeader();
                    var headerHead = CreateAudioWebSocketHeaderLength(headerBytes);
                    var arr = headerHead.Concat(headerBytes).ToArray();

                    if ((webSocket != null) && (bWebSocketReady))
                    {
                        //Create a request id that is unique for this 
                        webSocket.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Binary;
                        using (var dataWriter = new Windows.Storage.Streams.DataWriter(webSocket.OutputStream))
                        {
                            dataWriter.WriteBytes(arr);
                            await dataWriter.StoreAsync();
                            await dataWriter.FlushAsync();
                            dataWriter.DetachStream();
                        }
                    }
                }
                result = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending Speech Config: " + ex.Message);
            }
            return result;
        }
        private System.Collections.Generic.IEnumerable<Int16> Decode(byte[] byteArray)
        {
            for (var i = 0; i < byteArray.Length - 1; i += 2)
            {
                yield return (BitConverter.ToInt16(byteArray, i));
            }
        }
        async System.Threading.Tasks.Task<bool> SendSpeechStream(Windows.Storage.Streams.IRandomAccessStream wavStream, System.Threading.CancellationToken token)
        {
            bool result = false;
            try
            {
                if (wavStream != null)
                {
                    int Len = GetWAVHeaderLength(wavStream);
                    if (Len > 0)
                    {
                        ulong index = (ulong)Len;

                        while (!token.IsCancellationRequested)
                        {
                            Windows.Storage.Streams.IInputStream iStream = wavStream.GetInputStreamAt(index);
                            if (iStream != null)
                            {
                                var headerBytes = CreateAudioWebSocketHeader();
                                var headerHead = CreateAudioWebSocketHeaderLength(headerBytes);
                                uint length = (uint)(4096 * 2 - headerBytes.Length - 8);
                                byte[] dataArray = new byte[length];
                                if (wavStream.Size > index + length)
                                {
                                    iStream.ReadAsync(dataArray.AsBuffer(), length, Windows.Storage.Streams.InputStreamOptions.Partial).AsTask().Wait();
                                    var chunkHeader = System.Text.Encoding.ASCII.GetBytes("data").Concat(BitConverter.GetBytes((UInt32)length)).ToArray();
                                    index += length;
                                    System.Diagnostics.Debug.WriteLine("AudioStream read Index: " + index.ToString());

                                    var arr = headerHead.Concat(headerBytes).Concat(chunkHeader).Concat(dataArray).ToArray();


                                    if ((webSocket != null) && (bWebSocketReady))
                                    {
                                        //Create a request id that is unique for this 
                                        webSocket.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Binary;
                                        using (var dataWriter = new Windows.Storage.Streams.DataWriter(webSocket.OutputStream))
                                        {
                                            //  System.Diagnostics.Debug.Write(DumpHex(arr));
                                            dataWriter.WriteBytes(arr);
                                            await dataWriter.StoreAsync();
                                            await dataWriter.FlushAsync();
                                            dataWriter.DetachStream();
                                        }
                                    }
                                    else
                                        break;
                                    var amplitude = Decode(dataArray).Select(Math.Abs).Average(x => x);
                                    if (AudioLevel != null)
                                        this.AudioLevel(this, amplitude);
                                }
                                else
                                    await System.Threading.Tasks.Task.Delay(10);
                            }
                        }
                    }
                }
                {
                    var headerBytes = CreateAudioWebSocketHeader();
                    var headerHead = CreateAudioWebSocketHeaderLength(headerBytes);
                    var arr = headerHead.Concat(headerBytes).ToArray();

                    if ((webSocket != null) && (bWebSocketReady))
                    {
                        //Create a request id that is unique for this 
                        webSocket.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Binary;
                        using (var dataWriter = new Windows.Storage.Streams.DataWriter(webSocket.OutputStream))
                        {
                            dataWriter.WriteBytes(arr);
                            await dataWriter.StoreAsync();
                            await dataWriter.FlushAsync();
                            dataWriter.DetachStream();
                        }
                    }
                }
                result = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending Speech Stream: " + ex.Message);
            }
            return result;
        }
        async System.Threading.Tasks.Task<bool> AnalysingSpeechStream(Windows.Storage.Streams.IRandomAccessStream wavStream, System.Threading.CancellationToken token)
        {
            bool result = false;
            try
            {
                if (wavStream != null)
                {
                    int Len = GetWAVHeaderLength(wavStream);
                    if (Len > 0)
                    {
                        ulong index = (ulong)Len;
                        uint length = 4096;
                        byte[] dataArray = new byte[length];

                        while (!token.IsCancellationRequested)
                        {
                            Windows.Storage.Streams.IInputStream iStream = wavStream.GetInputStreamAt(index);
                            if (iStream != null)
                            {

                                if (wavStream.Size > index + length)
                                {
                                    iStream.ReadAsync(dataArray.AsBuffer(), length, Windows.Storage.Streams.InputStreamOptions.Partial).AsTask().Wait();
                                    index += length;
                                    var amplitude = Decode(dataArray).Select(Math.Abs).Average(x => x);
                                    if (AudioLevel != null)
                                        this.AudioLevel(this, amplitude);
                                }
                                else
                                    await System.Threading.Tasks.Task.Delay(10);
                            }
                        }
                    }
                }
                result = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while analysing Speech Stream: " + ex.Message);
            }
            return result;
        }
        int GetWAVHeaderLength(Windows.Storage.Streams.IRandomAccessStream fileStream)
        {
            int Len = 0;
            byte[] array = new byte[1024];
            if (array != null)
            {
                fileStream.Seek(0);
                fileStream.ReadAsync(array.AsBuffer(), (uint)1024, Windows.Storage.Streams.InputStreamOptions.Partial).AsTask().Wait();

                byte[] IntBuffer = new byte[4];
                if ((array[0] == 'R') &&
                    (array[1] == 'I') &&
                    (array[2] == 'F') &&
                    (array[3] == 'F'))
                {
                    System.Buffer.BlockCopy(array, 4, IntBuffer, 0, 4);
                    int Index = 8;
                    int FileLen = BitConverter.ToInt32(IntBuffer, 0);
                    if ((array[Index] == 'W') &&
                        (array[Index + 1] == 'A') &&
                        (array[Index + 2] == 'V') &&
                        (array[Index + 3] == 'E'))
                    {
                        Index += 4;
                        while (Index < array.Length)
                        {
                            if ((array[Index] == 'd') &&
                                (array[Index + 1] == 'a') &&
                                (array[Index + 2] == 't') &&
                                (array[Index + 3] == 'a'))
                            {
                                Len = Index;
                                break;
                            }
                            else
                            {
                                Index += 4;
                                System.Buffer.BlockCopy(array, Index, IntBuffer, 0, 4);
                                Index += 4;
                                Index += BitConverter.ToInt32(IntBuffer, 0);
                            }
                        }
                    }
                }
            }
            return Len;
        }
        async System.Threading.Tasks.Task<bool> SendSpeechFileHeader(Windows.Storage.StorageFile wavFile)
        {
            bool result = false;
            try
            {
                using (var fileStream = await wavFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    int Len = GetWAVHeaderLength(fileStream);
                    if (Len > 0)
                    {
                        byte[] header = new byte[Len];
                        if (header != null)
                        {
                            fileStream.Seek(0);
                            fileStream.ReadAsync(header.AsBuffer(), (uint)Len, Windows.Storage.Streams.InputStreamOptions.Partial).AsTask().Wait();

                            var headerBytes = CreateAudioWebSocketHeader();
                            var headerHead = CreateAudioWebSocketHeaderLength(headerBytes);

                            var arr = headerHead.Concat(headerBytes).Concat(header).ToArray();


                            if ((webSocket != null) && (bWebSocketReady))
                            {
                                //Create a request id that is unique for this 
                                webSocket.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Binary;
                                //    System.Diagnostics.Debug.Write(DumpHex(arr));

                                using (var dataWriter = new Windows.Storage.Streams.DataWriter(webSocket.OutputStream))
                                {
                                    dataWriter.WriteBytes(arr);
                                    await dataWriter.StoreAsync();
                                    await dataWriter.FlushAsync();
                                    dataWriter.DetachStream();
                                }
                            }

                        }

                    }
                }
                result = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending Speech Config: " + ex.Message);
            }
            return result;
        }

        async System.Threading.Tasks.Task<bool> SendSpeechStreamHeader(Windows.Storage.Streams.IRandomAccessStream wavStream)
        {
            bool result = false;
            try
            {
                if (wavStream != null)
                {
                    int Len = GetWAVHeaderLength(wavStream);
                    if (Len > 0)
                    {
                        byte[] header = new byte[Len];
                        if (header != null)
                        {
                            wavStream.Seek(0);
                            wavStream.ReadAsync(header.AsBuffer(), (uint)Len, Windows.Storage.Streams.InputStreamOptions.Partial).AsTask().Wait();

                            var headerBytes = CreateAudioWebSocketHeader();
                            var headerHead = CreateAudioWebSocketHeaderLength(headerBytes);

                            var arr = headerHead.Concat(headerBytes).Concat(header).ToArray();


                            if ((webSocket != null) && (bWebSocketReady))
                            {
                                //Create a request id that is unique for this 
                                webSocket.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Binary;
                                //    System.Diagnostics.Debug.Write(DumpHex(arr));

                                using (var dataWriter = new Windows.Storage.Streams.DataWriter(webSocket.OutputStream))
                                {
                                    dataWriter.WriteBytes(arr);
                                    await dataWriter.StoreAsync();
                                    await dataWriter.FlushAsync();
                                    dataWriter.DetachStream();
                                }
                            }

                        }

                    }
                }
                result = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while sending Speech Config: " + ex.Message);
            }
            return result;
        }
        bool CleanupWebSocket()
        {
            if (webSocket != null)
            {
                try
                {
                    webSocket.MessageReceived -= WebSocket_MessageReceived;
                    webSocket.Closed -= WebSocket_Closed;
                    webSocket.Dispose();
                }
                catch (Exception)
                {

                }
                webSocket = null;
            }
            return true;
        }
        bool CreateWebSocket()
        {
            CleanupWebSocket();
            if (webSocket == null)
            {
                webSocket = new Windows.Networking.Sockets.MessageWebSocket();
            }
            if (webSocket != null)
            {
                webSocket.Control.MessageType = Windows.Networking.Sockets.SocketMessageType.Binary;

                webSocket.MessageReceived += WebSocket_MessageReceived;
                webSocket.Closed += WebSocket_Closed;
            }
            return true;
        }
        #endregion
        #region private
        
        private async System.Threading.Tasks.Task<bool> InitializeRecording()
        {
            isRecordingInitialized = false;
            try
            {
                // Initialize MediaCapture
                mediaCapture = new Windows.Media.Capture.MediaCapture();

                await mediaCapture.InitializeAsync(new Windows.Media.Capture.MediaCaptureInitializationSettings
                {
                    //VideoSource = screenCapture.VideoSource,
                    //      AudioSource = screenCapture.AudioSource,
                    StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.Audio,
                    MediaCategory = Windows.Media.Capture.MediaCategory.Other,
                    AudioProcessing = Windows.Media.AudioProcessing.Raw

                });
                mediaCapture.RecordLimitationExceeded += MediaCapture_RecordLimitationExceeded;
                mediaCapture.Failed += MediaCapture_Failed;
                System.Diagnostics.Debug.WriteLine("Device Initialized Successfully...");
                isRecordingInitialized = true;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception while initializing the device: " + e.Message);
            }
            return isRecordingInitialized;
        }
        async void MediaCapture_Failed(Windows.Media.Capture.MediaCapture sender, Windows.Media.Capture.MediaCaptureFailedEventArgs errorEventArgs)
        {
            System.Diagnostics.Debug.WriteLine("Fatal Error " + errorEventArgs.Message);
            await StopRecording();
            AudioCaptureError?.Invoke(this, errorEventArgs.Message);
        }

        async void MediaCapture_RecordLimitationExceeded(Windows.Media.Capture.MediaCapture sender)
        {
            System.Diagnostics.Debug.WriteLine("Stopping Record on exceeding max record duration");
            await StopRecording();
            AudioCaptureError?.Invoke(this, "Error Media Capture: Record Limitation Exceeded");
        }
        string GetAzureRegion(string SpeechHostName)
        {
            if (!string.IsNullOrEmpty(SpeechHostName))
            {
                string[] array = SpeechHostName.Split('.');
                if ((array != null) && (array.Count() > 0))
                {
                    return array[0];
                }
            }
            return string.Empty;
        }

        private void WebSocket_MessageReceived(Windows.Networking.Sockets.MessageWebSocket sender, Windows.Networking.Sockets.MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                if (bWebSocketReady)
                {
                    using (Windows.Storage.Streams.DataReader dataReader = args.GetDataReader())
                    {
                        dataReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                        string message = dataReader.ReadString(dataReader.UnconsumedBufferLength);
                        System.Diagnostics.Debug.WriteLine("Message received from MessageWebSocket: " + message);
                        WebSocketMessage wsm = WebSocketMessage.CreateSocketMessage(message);
                        if (wsm != null)
                        {
                            SpeechToTextResponse sr = new SpeechToTextResponse(wsm.Body);
                            switch (wsm.Path.ToLower())
                            {
                                case "turn.start":
                                    WebSocketInitializedEvent.Set();
                                    WebSocketEvent?.Invoke(this, wsm.Path.ToLower(), sr);
                                    break;
                                case "turn.end":
                                    bWebSocketReady = false;
                                    AudioCaptureError?.Invoke(this, "Received end of conversation from the service");
                                    WebSocketEvent?.Invoke(this, wsm.Path.ToLower(), sr);
                                    break;
                                case "speech.enddetected":
                                    WebSocketEvent?.Invoke(this, wsm.Path.ToLower(), sr);
                                    break;
                                case "speech.phrase":
                                    WebSocketEvent?.Invoke(this, wsm.Path.ToLower(), sr);
                                    break;
                                case "speech.hypothesis":
                                    WebSocketEvent?.Invoke(this, wsm.Path.ToLower(), sr);
                                    break;
                                case "speech.startdetected":
                                    WebSocketEvent?.Invoke(this, wsm.Path.ToLower(), sr);
                                    break;
                                case "speech.fragment":
                                    WebSocketEvent?.Invoke(this, wsm.Path.ToLower(), sr);
                                    break;
                                default:
                                    break;
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Windows.Web.WebErrorStatus webErrorStatus = Windows.Networking.Sockets.WebSocketError.GetStatus(ex.GetBaseException().HResult);
                // Add additional code here to handle exceptions.
            }
        }

        private void WebSocket_Closed(Windows.Networking.Sockets.IWebSocket sender, Windows.Networking.Sockets.WebSocketClosedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("WebSocket_Closed; Code: " + args.Code + ", Reason: \"" + args.Reason + "\"");
            // Add additional code here to handle the WebSocket being closed.
            if (webSocket != null)
                webSocket.Dispose();
            webSocket = null;
        }
        private void ProgressHandler(Windows.Web.Http.HttpProgress progress)
        {
            System.Diagnostics.Debug.WriteLine("Http progress: " + progress.Stage.ToString() + " " + progress.BytesSent.ToString() + "/" + progress.TotalBytesToSend.ToString());
        }
        #endregion
    }
}
