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
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Xml.Linq;
using Windows.Web.Http.Headers;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;

namespace FaceClient
{







    /// <summary>
    /// class FaceClient: Vision UWP Client
    /// </summary>
    /// <info>
    /// Event data that describes how this page was reached.
    /// This parameter is typically used to configure the page.
    /// </info>
    public sealed class FaceClient
    {

        private const string FaceDetectUrl = "https://{0}/face/v1.0/detect?{1}";
        private const string FaceIdentifyUrl = "https://{0}/face/v1.0/identify";
        private const string FaceListPersonGroupsUrl = "https://{0}/face/v1.0/persongroups?returnRecognitionModel=true";
        private const string FaceListPersonGroupsPersonsUrl = "https://{0}/face/v1.0/persongroups/{1}/persons";
        private const string FacePersonGroupsUrl = "https://{0}/face/v1.0/persongroups/{1}";
        private const string FaceAddPersonGroupUrl = "https://{0}/face/v1.0/persongroups/{1}";
        private const string FacePersonGroupsPersonUrl = "https://{0}/face/v1.0/persongroups/{1}/persons";
        private const string FaceTrainPersonGroupUrl = "https://{0}/face/v1.0/persongroups/{1}/train";
        private const string FaceTrainingPersonGroupUrl = "https://{0}/face/v1.0/persongroups/{1}/training";
        private const string FaceDeletePersonGroupsPersonUrl = "https://{0}/face/v1.0/persongroups/{1}/persons/{2}";
        private const string FaceDeletePersonGroupUrl = "https://{0}/face/v1.0/persongroups/{1}";
        private const string FaceAddPersonGroupsPersonUrl = "https://{0}/face/v1.0/persongroups/{1}/persons";
        private const string FaceAddPersonGroupsPersonFaceUrl = "https://{0}/face/v1.0/persongroups/{1}/persons/{2}/persistedfaces";

        private const string Model1 = "recognition_01";
        private const string Model2 = "recognition_02";
        private const string Model = Model1;

        /// <summary>
        /// class FaceClient constructor
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        public FaceClient()
        {

        }


        private void ProgressHandler(Windows.Web.Http.HttpProgress progress)
        {
            System.Diagnostics.Debug.WriteLine("Http progress: " + progress.Stage.ToString() + " " + progress.BytesSent.ToString() + "/" + progress.TotalBytesToSend.ToString());
        }
        /// <summary>
        /// DetectFace method
        /// </summary>
        /// <param name="subscriptionKey">service key
        /// </param>
        /// <param name="hostname">hostname associated with the service url
        /// </param>
        /// <param name="visualFeatures">visual features for the request: tags, ...
        /// </param>
        /// <param name="details">detail information for the request:
        /// </param>
        /// <param name="lang">language for the response chinese or english so far
        /// </param>
        /// <param name="pictureFile">StorageFile associated with the picture file which 
        /// will be sent to the Vision Services.
        /// </param>
        /// <return>The result of the Vision REST API.
        /// </return>
        public IAsyncOperation<FaceResponse> DetectFace(string subscriptionKey, string hostname, string faceFeatures, Windows.Storage.StorageFile pictureFile)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                       string faceUrl = string.Format(FaceDetectUrl, hostname, faceFeatures);
                        Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;

                    Windows.Storage.StorageFile file = pictureFile;
                    if (file != null)
                    {
                        using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                        {
                            if (fileStream != null)
                            {


                                Windows.Web.Http.HttpStreamContent content = new Windows.Web.Http.HttpStreamContent(fileStream.AsStream().AsInputStream());
                                System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                                IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                                content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/octet-stream");

                                    hrm = await hc.PostAsync(new Uri(faceUrl), content).AsTask(cts.Token, progress);
                                
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
                                    r = new FaceResponse(result,null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

            return r;
             }).AsAsyncOperation<FaceResponse>();
        }

        /// <summary>
        /// FaceIdentify method
        /// </summary>
        /// <param name="subscriptionKey">service key
        /// </param>
        /// <param name="hostname">hostname associated with the service url
        /// </param>
        /// <param name="visualFeatures">visual features for the request: tags, ...
        /// </param>
        /// <param name="details">detail information for the request:
        /// </param>
        /// <param name="lang">language for the response chinese or english so far
        /// </param>
        /// <param name="pictureFile">StorageFile associated with the picture file which 
        /// will be sent to the Vision Services.
        /// </param>
        /// <return>The result of the Vision REST API.
        /// </return>
        public IAsyncOperation<FaceResponse> IdentifyFaces(string subscriptionKey, string hostname, string largePersonGroupId, int maxNumOfCandidatesReturned, double confidenceThreshold, IEnumerable<string> faceIds)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceIdentifyUrl, hostname);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    Identify ident = new Identify();

                    if (ident != null)
                    {
                        ident.personGroupId = largePersonGroupId;
                        ident.largePersonGroupId = null;
                        ident.confidenceThreshold = confidenceThreshold;
                        ident.maxNumOfCandidatesReturned = maxNumOfCandidatesReturned;
                        ident.faceIds = new List<string>();
                        foreach (var f in faceIds)
                        {
                            ident.faceIds.Add(f);
                        }


                        string s = JsonConvert.SerializeObject(ident);
                        Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent(s);
                        System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                        IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                        content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");

                        hrm = await hc.PostAsync(new Uri(faceUrl), content).AsTask(cts.Token, progress);
                    }
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                var b = await hrm.Content.ReadAsBufferAsync();
                                string result = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }

        /// <summary>
        /// CreatePersonGroup method
        /// </summary>
        /// <param name="subscriptionKey">service key
        /// </param>
        /// <param name="hostname">hostname associated with the service url
        /// </param>
        /// <param name="visualFeatures">visual features for the request: tags, ...
        /// </param>
        /// <param name="details">detail information for the request:
        /// </param>
        /// <param name="lang">language for the response chinese or english so far
        /// </param>
        /// <param name="pictureFile">StorageFile associated with the picture file which 
        /// will be sent to the Vision Services.
        /// </param>
        /// <return>The result of the Vision REST API.
        /// </return>
        public IAsyncOperation<FaceResponse> CreatePersonGroup(string subscriptionKey, string hostname, string PersonGroupId, string PersonGroupName, string PersonGroupData, string PersonGroupModel)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FacePersonGroupsUrl, hostname, PersonGroupId);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    Group grp = new Group();

                    if (grp != null)
                    {
                        grp.name = PersonGroupName;
                        grp.userData = PersonGroupData;
                        grp.recognitionModel = PersonGroupModel;

                        string s = JsonConvert.SerializeObject(grp);
                        Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent(s);
                        System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                        IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                        content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");

                        hrm = await hc.PutAsync(new Uri(faceUrl), content).AsTask(cts.Token, progress);
                    }
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                string result = PersonGroupId;
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }
        public IAsyncOperation<FaceResponse> DeletePersonGroup(string subscriptionKey, string hostname, string groupId)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceDeletePersonGroupUrl, hostname, groupId);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);

                    hrm = await hc.DeleteAsync(new Uri(faceUrl)).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                string result = groupId;
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }


        /// <summary>
        /// ListPersonGroup method
        /// </summary>
        /// <param name="subscriptionKey">service key
        /// </param>
        /// <param name="hostname">hostname associated with the service url
        /// </param>
        /// <param name="visualFeatures">visual features for the request: tags, ...
        /// </param>
        /// <param name="details">detail information for the request:
        /// </param>
        /// <param name="lang">language for the response chinese or english so far
        /// </param>
        /// <param name="pictureFile">StorageFile associated with the picture file which 
        /// will be sent to the Vision Services.
        /// </param>
        /// <return>The result of the Vision REST API.
        /// </return>
        public IAsyncOperation<FaceResponse> ListPersonGroup(string subscriptionKey, string hostname)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceListPersonGroupsUrl, hostname);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);

                    hrm = await hc.GetAsync(new Uri(faceUrl)).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                var b = await hrm.Content.ReadAsBufferAsync();
                                string result = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }
        /// <summary>
        /// ListPersonGroup method
        /// </summary>
        /// <param name="subscriptionKey">service key
        /// </param>
        /// <param name="hostname">hostname associated with the service url
        /// </param>
        /// <param name="visualFeatures">visual features for the request: tags, ...
        /// </param>
        /// <param name="details">detail information for the request:
        /// </param>
        /// <param name="lang">language for the response chinese or english so far
        /// </param>
        /// <param name="pictureFile">StorageFile associated with the picture file which 
        /// will be sent to the Vision Services.
        /// </param>
        /// <return>The result of the Vision REST API.
        /// </return>
        public IAsyncOperation<FaceResponse> ListPersonGroupsPersons(string subscriptionKey, string hostname, string groupId)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceListPersonGroupsPersonsUrl, hostname, groupId);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);

                    hrm = await hc.GetAsync(new Uri(faceUrl)).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                var b = await hrm.Content.ReadAsBufferAsync();
                                string result = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }
        public IAsyncOperation<FaceResponse> DeletePersonGroupsPerson(string subscriptionKey, string hostname, string groupId, string personId)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceDeletePersonGroupsPersonUrl, hostname, groupId,personId);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);

                    hrm = await hc.DeleteAsync(new Uri(faceUrl)).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                string result = personId;
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }

        public IAsyncOperation<FaceResponse> AddPersonGroupsPersonFace(string subscriptionKey, string hostname, string groupId, string personId, Windows.Storage.StorageFile pictureFile)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceAddPersonGroupsPersonFaceUrl, hostname, groupId,personId);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    Windows.Storage.StorageFile file = pictureFile;
                    if (file != null)
                    {
                        using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                        {
                            if (fileStream != null)
                            {

                                Windows.Web.Http.HttpStreamContent content = new Windows.Web.Http.HttpStreamContent(fileStream.AsStream().AsInputStream());
                                System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                                IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                                content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/octet-stream");

                                hrm = await hc.PostAsync(new Uri(faceUrl), content).AsTask(cts.Token, progress);
                                if (hrm != null)
                                {
                                    switch (hrm.StatusCode)
                                    {
                                        case Windows.Web.Http.HttpStatusCode.Ok:
                                            string result = file.Path;
                                            if (!string.IsNullOrEmpty(result))
                                                r = new FaceResponse(result, null);
                                            break;

                                        default:
                                            int code = (int)hrm.StatusCode;
                                            string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                            System.Diagnostics.Debug.WriteLine(HttpError);
                                            r = new FaceResponse(string.Empty, HttpError);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }
        public IAsyncOperation<FaceResponse> TrainingPersonGroup(string subscriptionKey, string hostname, string groupName)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceTrainingPersonGroupUrl, hostname, groupName);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;



                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                    hrm = await hc.GetAsync(new Uri(faceUrl)).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                var b = await hrm.Content.ReadAsBufferAsync();
                                string result = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null); 
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }
        public IAsyncOperation<FaceResponse> TrainPersonGroup(string subscriptionKey, string hostname, string groupName)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceTrainPersonGroupUrl, hostname, groupName);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;



                    Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent("");
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                    content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");
                    hrm = await hc.PostAsync(new Uri(faceUrl),content).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Accepted:
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                string result = groupName;
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }
        public IAsyncOperation<FaceResponse> AddPersonGroup(string subscriptionKey, string hostname, string groupName)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceAddPersonGroupUrl, hostname, groupName);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;


                    NewGroup g = new NewGroup();
                    g.name = groupName;
                    g.userData = "user data for " + groupName;
                    g.recognitionModel = Model;
                    Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent(JsonConvert.SerializeObject(g));
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                    content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");
                    hrm = await hc.PutAsync(new Uri(faceUrl), content).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                string result = groupName;
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }
        public IAsyncOperation<FaceResponse> AddPersonGroupsPerson(string subscriptionKey, string hostname, string groupId, string personName)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceAddPersonGroupsPersonUrl, hostname, groupId);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;


                    NewPerson p = new NewPerson(); 
                    p.name = personName;
                    p.userData = "user data for " + personName;
                    Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent(JsonConvert.SerializeObject(p));
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                    content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");
                    hrm = await hc.PostAsync(new Uri(faceUrl), content).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                string result = personName;
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }
        /// <summary>
        /// CreatePerson method
        /// </summary>
        /// <param name="subscriptionKey">service key
        /// </param>
        /// <param name="hostname">hostname associated with the service url
        /// </param>
        /// <param name="visualFeatures">visual features for the request: tags, ...
        /// </param>
        /// <param name="details">detail information for the request:
        /// </param>
        /// <param name="lang">language for the response chinese or english so far
        /// </param>
        /// <param name="pictureFile">StorageFile associated with the picture file which 
        /// will be sent to the Vision Services.
        /// </param>
        /// <return>The result of the Vision REST API.
        /// </return>
        public IAsyncOperation<FaceResponse> CreatePerson(string subscriptionKey, string hostname, string PersonGroupId, string PersonName, string PersonGroupData, string PersonGroupModel)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FacePersonGroupsPersonUrl, hostname, PersonGroupId);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    Group grp = new Group();

                    if (grp != null)
                    {
                        grp.name = PersonName;
                        grp.userData = PersonGroupData;
                        grp.recognitionModel = PersonGroupModel;

                        string s = JsonConvert.SerializeObject(grp);
                        Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent(s);
                        System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                        IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                        content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");

                        hrm = await hc.PutAsync(new Uri(faceUrl), content).AsTask(cts.Token, progress);
                    }
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                string result = PersonGroupId;
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }
        /// <summary>
        /// CreatePerson method
        /// </summary>
        /// <param name="subscriptionKey">service key
        /// </param>
        /// <param name="hostname">hostname associated with the service url
        /// </param>
        /// <param name="visualFeatures">visual features for the request: tags, ...
        /// </param>
        /// <param name="details">detail information for the request:
        /// </param>
        /// <param name="lang">language for the response chinese or english so far
        /// </param>
        /// <param name="pictureFile">StorageFile associated with the picture file which 
        /// will be sent to the Vision Services.
        /// </param>
        /// <return>The result of the Vision REST API.
        /// </return>
        public IAsyncOperation<FaceResponse> TrainPersonGroup(string subscriptionKey, string hostname, string PersonGroupId, string PersonName, string PersonGroupData, string PersonGroupModel)
        {
            return Task.Run<FaceResponse>(async () =>
            {
                FaceResponse r = null;

                try
                {
                    string faceUrl = string.Format(FaceTrainPersonGroupUrl, hostname, PersonGroupId);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();

                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", subscriptionKey);
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("ContentType", "application/octet-stream");

                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    Group grp = new Group();

                    if (grp != null)
                    {
                        grp.name = PersonName;
                        grp.userData = PersonGroupData;
                        grp.recognitionModel = PersonGroupModel;

                        string s = JsonConvert.SerializeObject(grp);
                        Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent(s);
                        System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                        IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                        content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");

                        hrm = await hc.PutAsync(new Uri(faceUrl), content).AsTask(cts.Token, progress);
                    }
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                string result = PersonGroupId;
                                if (!string.IsNullOrEmpty(result))
                                    r = new FaceResponse(result, null);
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
                                r = new FaceResponse(string.Empty, HttpError);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Exception while sending the audio file:" + ex.Message);
                }

                return r;
            }).AsAsyncOperation<FaceResponse>();
        }
    }

}
