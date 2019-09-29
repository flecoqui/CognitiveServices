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
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TranslatorText
{

    public class TranslationProperty
    {
        public string name;
        public string nativeName;
        public string dir;
    }
    public class ScriptProperty
    {
        public string code;
        public string name;
        public string nativeName;
        public string dir;
    }
    public class ScriptItem
    {
        public string code;
        public string name;
        public string nativeName;
        public string dir;
        public List<ScriptProperty> toScripts;
    }
    public class TransliterationProperty
    {
        public string name;
        public string nativeName;
        public List<ScriptItem> scripts;
    }
    public class DictionaryItem
    {
        public string name;
        public string nativeName;
        public string dir;
        public string code;
    }
    public class DictionaryProperty
    {
        public string name;
        public string nativeName;
        public string dir;
        public List<DictionaryItem> translations;
    }
    public class GetLanguageResult
    {
        public System.Collections.Generic.Dictionary<string, TranslationProperty>  translation;
        public System.Collections.Generic.Dictionary<string, TransliterationProperty> transliteration;
        public System.Collections.Generic.Dictionary<string, DictionaryProperty> dictionary;
    }
    public class TextItem
    {
        public string text;
        public string to;
    }
    public class TranslationItem
    {
        public List<TextItem> translations;
    }
    public class TranslationResult
    {
        public List<TranslationItem> translations;
    }
    public class LanguageItem
    {
        public string language;
        public double score;
        public bool isTranslationSupported;
        public bool isTransliterationSupported;
    }
    public class LanguageResult
    {
        public string language;
        public double score;
        public bool isTranslationSupported;
        public bool isTransliterationSupported;
        public List<LanguageItem> alternatives;
    }
    /// <summary>
    /// class TranslatorTextClient: TranslatorText UWP Client
    /// </summary>
    /// <info>
    /// Event data that describes how this page was reached.
    /// This parameter is typically used to configure the page.
    /// </info>
    public class TranslatorTextClient
    {
        private string SubscriptionKey;
        private string Token;
        private const string AuthUrl = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";
        //private const string BaseUrl = "http://api.microsofttranslator.com/v2/Http.svc/";
        private const string BaseUrl = "https://api.cognitive.microsofttranslator.com/";
//        private const string LanguagesUri = "GetLanguagesForTranslate";
        private const string LanguagesUri = "languages?api-version=3.0";
//        private const string TranslateUri = "Translate?text={0}&to={1}&contentType=text/plain";
//        private const string TranslateWithFromUri = "Translate?text={0}&from={1}&to={2}&contentType=text/plain";

        private const string TranslateUri = "translate?api-version=3.0&to={0}&contentType=text/plain";
        private const string TranslateWithFromUri = "translate?api-version=3.0&from={0}&to={1}&contentType=text/plain";

        //private const string DetectUri = "Detect?text={0}";
        private const string DetectUri = "detect?api-version=3.0";
        private const string ArrayNamespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";


        /// <summary>
        /// class TranslatorTextClient constructor
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        public TranslatorTextClient()
        {
            SubscriptionKey = string.Empty;
            Token = string.Empty;
        }

        /// <summary>
        /// GetToken method
        /// </summary>
        /// <param name="subscriptionKey">SubscriptionKey associated with the TranslatorText 
        /// Cognitive Service subscription.
        /// </param>
        /// <return>Token which is used for all calls to the TranslatorText REST API.
        /// </return>
        public async System.Threading.Tasks.Task<string> GetToken(string subscriptionKey )
        {
            if (string.IsNullOrEmpty(subscriptionKey))
                return string.Empty;
            SubscriptionKey = subscriptionKey;
            try
            {
                Token = string.Empty;
                Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();
                hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", SubscriptionKey);
                Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent(String.Empty);
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
                                Token = "Bearer " + result;
                                return Token;
                            }
                            break;

                        default:
                            System.Diagnostics.Debug.WriteLine("Http Response Error:" + hrm.StatusCode.ToString() + " reason: " + hrm.ReasonPhrase.ToString());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while getting the token: " + ex.Message);
            }
            return string.Empty;
        }
        /// <summary>
        /// RenewToken method
        /// </summary>
        /// <param>
        /// </param>
        /// <return>Token which is used to all the calls to the TranslatorText REST API.
        /// </return>
        public async System.Threading.Tasks.Task<string> RenewToken()
        {
            if (string.IsNullOrEmpty(SubscriptionKey))
                return string.Empty;
            try
            {
                Token = string.Empty;
                Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();
                hc.DefaultRequestHeaders.TryAppendWithoutValidation("Ocp-Apim-Subscription-Key", SubscriptionKey);
                Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent(String.Empty);
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
                                Token = "Bearer  " + result;
                                return Token;
                            }
                            break;

                        default:
                            System.Diagnostics.Debug.WriteLine("Http Response Error:" + hrm.StatusCode.ToString() + " reason: " + hrm.ReasonPhrase.ToString());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception while getting the token: " + ex.Message);
            }
            return string.Empty;
        }
        /// <summary>
        /// HasToken method
        /// </summary>
        /// <param>Check if a Token has been acquired
        /// </param>
        /// <return>true if a Token has been acquired to use the TranslatorText REST API.
        /// </return>
        public bool HasToken()
        {
            if (string.IsNullOrEmpty(Token))
                return false;
            return true;
        }
        /// <summary>
        /// GetLanguages method
        /// Return the list of supported languages.
        /// </summary>
        /// <return>list of supported languages
        /// </return>
        public async System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<string,string>> GetLanguages()
        {
            System.Collections.Generic.Dictionary <string,string> r = new System.Collections.Generic.Dictionary<string, string>();
            int loop = 1;

            while (loop-- > 0)
            {
                try
                {
                    string languageUrl = BaseUrl + LanguagesUri;
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Authorization", Token);
                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    System.Diagnostics.Debug.WriteLine("REST API Get");
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                    hrm = await hc.GetAsync(new Uri(languageUrl)).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                var b = await hrm.Content.ReadAsBufferAsync();
                                string json = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());
                                if (!string.IsNullOrEmpty(json))
                                {
                                  
                                    
                                    var result = JsonConvert.DeserializeObject<GetLanguageResult>(json);
                                    if(result!=null)
                                    {
                                        foreach(var keyval in result.translation)
                                        {
                                            r.Add(keyval.Key, keyval.Value.name);
                                        }
                                    }
                                    
                                    /*
                                    System.Xml.Linq.XNamespace ns = ArrayNamespace;
                                    var doc = System.Xml.Linq.XDocument.Parse(result);

                                    doc.Root.Elements(ns + "string").Select(s => s.Value).ToList().ForEach(lang =>
                                    {
                                        try
                                        {
                                            var culture = new System.Globalization.CultureInfo(lang);
                                            if (!culture.EnglishName.StartsWith("Unknown"))
                                            {
                                                r.Add(lang, culture.EnglishName);
                                            }
                                        }
                                        catch
                                        {
                                        }
                                    });
                                    */
                                }
                                break;
                            case Windows.Web.Http.HttpStatusCode.Forbidden:
                                string token = await RenewToken();
                                if (string.IsNullOrEmpty(token))
                                {
                                    loop++;
                                }
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
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

        }

        /// <summary>
        /// Translate method
        /// Translate input text from one language to another language.
        /// </summary>
        /// <param name="Text">
        /// Input Text 
        /// <param name="FromLang">
        /// Input Language
        /// <param name="ToLang">
        /// Output Language
        /// </param>
        /// <return>return output text (translated)
        /// </return>
        public async System.Threading.Tasks.Task<string> Translate(string Text, string FromLang, string ToLang)
        {
            string r = string.Empty;
            int loop = 1;

            while (loop-- > 0)
            {
                try
                {
                    string uriString = null;
                    if (string.IsNullOrWhiteSpace(FromLang))
                    {
//                        uriString = string.Format(TranslateUri, Uri.EscapeDataString(Text), ToLang);
                        uriString = string.Format(TranslateUri,  ToLang);
                    }
                    else
                    {
//                        uriString = string.Format(TranslateWithFromUri, Uri.EscapeDataString(Text), FromLang, ToLang);
                        uriString = string.Format(TranslateWithFromUri,  FromLang, ToLang);
                    }
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Authorization", Token);
                   
                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    System.Diagnostics.Debug.WriteLine("REST API POST");
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                    Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent("[{ \"Text\":\""+ Text +"\"}]" );
                    content.Headers.ContentType = new Windows.Web.Http.Headers.HttpMediaTypeHeaderValue("application/json");
                    //"Application/JSON";


                    hrm = await hc.PostAsync(new Uri(BaseUrl + uriString),content).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                var b = await hrm.Content.ReadAsBufferAsync();
                                string json = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());

                                
                                List<TranslationItem>  result = JsonConvert.DeserializeObject<List<TranslationItem>>(json);
                                if (result != null)
                                {
                                    foreach (var val in result)
                                    {
                                        var text = val.translations.FirstOrDefault();
                                        r = text.text;
                                        break;
                                    }
                                }
                                /*
                                if (!string.IsNullOrEmpty(result))
                                {
                                    System.Xml.Linq.XNamespace ns = ArrayNamespace;
                                    var doc = System.Xml.Linq.XDocument.Parse(result);
                                    r = doc.Root.Value;
                                }
                                */
                                break;
                            case Windows.Web.Http.HttpStatusCode.Forbidden:
                                string token = await RenewToken();
                                if (string.IsNullOrEmpty(token))
                                {
                                    loop++;
                                }
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
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
        }

        /// <summary>
        /// DetectLanguage method
        /// Detect Language from input text.
        /// </summary>
        /// <param name="Text">
        /// Input Text 
        /// <return>return language(string)
        /// </return>
        public async System.Threading.Tasks.Task<string> DetectLanguage(string Text)
        {
            string r = string.Empty;
            int loop = 1;

            while (loop-- > 0)
            {
                try
                {
                    string uriString = null;
//                    uriString = string.Format(DetectUri, Uri.EscapeDataString(Text));
                    uriString = string.Format(DetectUri);
                    Windows.Web.Http.HttpClient hc = new Windows.Web.Http.HttpClient();
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    hc.DefaultRequestHeaders.TryAppendWithoutValidation("Authorization", Token);
                    Windows.Web.Http.HttpResponseMessage hrm = null;
                    System.Diagnostics.Debug.WriteLine("REST API Post");
                    IProgress<Windows.Web.Http.HttpProgress> progress = new Progress<Windows.Web.Http.HttpProgress>(ProgressHandler);
                    Windows.Web.Http.HttpStringContent content = new Windows.Web.Http.HttpStringContent("[{ \"Text\":\"" + Text + "\"}]");
                    content.Headers.ContentType = new Windows.Web.Http.Headers.HttpMediaTypeHeaderValue("application/json");

                    hrm = await hc.PostAsync(new Uri(BaseUrl + uriString),content).AsTask(cts.Token, progress);
                    if (hrm != null)
                    {
                        switch (hrm.StatusCode)
                        {
                            case Windows.Web.Http.HttpStatusCode.Ok:
                                var b = await hrm.Content.ReadAsBufferAsync();
                                string json = System.Text.UTF8Encoding.UTF8.GetString(b.ToArray());
                                List<LanguageResult> result = JsonConvert.DeserializeObject<List<LanguageResult>>(json);
                                if (result != null)
                                {
                                   // r = json;
                                    var lang = result.FirstOrDefault();
                                    r = lang.language;
                                }
                                /*
                                if (!string.IsNullOrEmpty(result))
                                {
                                    System.Xml.Linq.XNamespace ns = ArrayNamespace;
                                    var doc = System.Xml.Linq.XDocument.Parse(result);
                                    r = doc.Root.Value;
                                }
                                */
                                break;
                            case Windows.Web.Http.HttpStatusCode.Forbidden:
                                string token = await RenewToken();
                                if (string.IsNullOrEmpty(token))
                                {
                                    loop++;
                                }
                                break;

                            default:
                                int code = (int)hrm.StatusCode;
                                string HttpError = "Http Response Error: " + code.ToString() + " reason: " + hrm.ReasonPhrase.ToString();
                                System.Diagnostics.Debug.WriteLine(HttpError);
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
        }


        #region private

        private void ProgressHandler(Windows.Web.Http.HttpProgress progress)
        {
            System.Diagnostics.Debug.WriteLine("Http progress: " + progress.Stage.ToString() + " " + progress.BytesSent.ToString() + "/" + progress.TotalBytesToSend.ToString());
        }
        #endregion private
    }

}
