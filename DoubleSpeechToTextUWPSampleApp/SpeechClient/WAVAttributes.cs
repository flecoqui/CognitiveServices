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

namespace SpeechClient
{
    /// <summary>
    /// class WAVAttributes
    /// </summary>
    /// <info>
    /// Static Class with Guid used to parse the WAV header
    /// </info>
    static class WAVAttributes
    {
        /// <summary>
        /// Audio block alignment
        /// </summary>
        public static readonly Guid MF_MT_AUDIO_BLOCK_ALIGNMENT = new Guid("322de230-9eeb-43bd-ab7a-ff412251541d");
        /// <summary>
        /// Audio number of channels
        /// </summary>
        public static readonly Guid MF_MT_AUDIO_NUM_CHANNELS = new Guid("37e48bf5-645e-4c5b-89de-ada9e29b696a");
        /// <summary>
        /// Media type Major Type
        /// </summary>
        public static readonly Guid MF_MT_MAJOR_TYPE = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
        /// <summary>
        /// Audio samples per second
        /// </summary>
        public static readonly Guid MF_MT_AUDIO_SAMPLES_PER_SECOND = new Guid("5faeeae7-0290-4c31-9e8a-c534f68d9dba");
        /// <summary>
        /// Specifies for a media type whether each sample is independent of the other samples in the stream. 
        /// </summary>
        public static readonly Guid MF_MT_ALL_SAMPLES_INDEPENDENT = new Guid("c9173739-5e56-461c-b713-46fb995cb95f");
        /// <summary>
        /// Audio bits per sample
        /// </summary>
        public static readonly Guid MF_MT_AUDIO_BITS_PER_SAMPLE = new Guid("f2deb57f-40fa-4764-aa33-ed4f2d1ff669");
        /// <summary>
        /// Media Type subtype
        /// </summary>
        public static readonly Guid MF_MT_SUBTYPE = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        /// <summary>
        /// Audio average bytes per second
        /// </summary>
        public static readonly Guid MF_MT_AUDIO_AVG_BYTES_PER_SECOND = new Guid("1aab75c8-cfef-451c-ab95-ac034b8e1731");
    }
}
