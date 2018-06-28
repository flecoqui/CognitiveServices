<!---
  category: AudioVideoAndCamera
  samplefwlink: http://go.microsoft.com/fwlink/p/?LinkId=620563&clcid=0x409
--->

# Speech-To-Text and Text-To-Speech UWP Sample Application

Overview
--------------
This sample application can use the following Cognitive Services:
- Microsoft Speech API or Bing Speech services: documentation is available [here](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/home)
- Custom Speech service: the documentation is available [here](https://docs.microsoft.com/en-us/azure/cognitive-services/custom-speech-service/cognitive-services-custom-speech-home)
- Speech Service (Preview): the documentation is available [here](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/)

Those 3 services are accesible through:

- a REST API for the Speech-To-Text described [here](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech/getstarted/getstartedrest?tabs=Powershell)  

- a WebSocket protocol for the Speech-To-Text described [here](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech/api-reference-rest/websocketprotocol) 
- a REST API for the Text-To-Speech described [here](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech/API-Reference-REST/BingVoiceOutput)


This Speech-To-Text and Text-To-Speech UWP Sample Application  can:

- **Record**: record spoken audio into a WAV file, 
- **Play**: play the WAV files stored on the local disk,
- **Convert WAV file**: Convert the WAV file to text with Cognitive Services,
- **Convert live audio**: Convert live audio to text with Cognitive Services, the audio buffer is sent to Cogntive Services at the end of the recording session.
- **Convert continuously live audio**: Convert continuously live audio to text with Cognitive Services using the conversation mode only available through the WebSocket API.
- **Convert Text**: Convert a text into a WAV stream associated with the current language (Text-To-Speech),

The spoken audio is recorded into a WAV file in the following format:

- **Number of Channels**: one channel, 
- **Samples per second**: 16000,
- **Bits per sample**: 16 bits,
- **Average Bytes per second**: 256 kbit/s.

In order to use the application you need a Cognitive Services Speech-To-Text subscription Key.
You can sign up [here](https://www.microsoft.com/cognitive-services/en-us/sign-up)  


Installing the application
----------------------------
You can install the application on:

- **Personal Computer Platform**: a desktop running Windows 10 RS1
- **Windows 10 Mobile Platform**: a phone running Windows 10 RS1

The applications packages for x86, x64 and ARM are available there :
[ZIP file of the application x86, x64, ARM Packages](https://github.com/flecoqui/CognitiveServices/raw/master/SpeechToTextUWPSampleApp/Releases/LatestRelease.zip)


**Personal Computer installation:**

1.  Download the ZIP file on your computer harddrive
2.  Unzip the ZIP file
3.  Launch the PowerShell file Add-AppPackage.ps1. The PowerShell script will install the application on your computer running Windows 10


**Phone installation:**

1.  Connect the phone running Windows 10 Mobile to your computer with a USB Cable.
2.  After few seconds, you should see the phone storage with Windows Explorer running on your computer
3.  Copy the application packages on your phone storage, for instance in the Downloads folder
4.  On the phone install a File Explorer application from Windows Store
5.  With File Explorer on the phone navigate to the folder where the packages have been copied
6.  Tap on the file SpeechToTextUWPSampleApp_1.0.XX.O_x86_x64_arm.cer to install the certificate.
7.  Tap on the file SpeechToTextUWPSampleApp_1.0.XX.O_x86_x64_arm.appxbundle to install the application


Using the application
----------------------------
Once the application is installed on your device, you can launch it and the main page will be displayed after few seconds.

### Main page

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/main.png)

The application is used to record spoken audio into a WAV file, play the WAV files stored on the local disk, convert the WAV file to text, convert live audio to text and convert continuously live audio to text .

### Selecting your service (Bing Speech, Custom Speech or Speech)
Once the application is launched, you can select the service you want to use in selecting the hostname associated with your service. "speech.platform.bing.com" is associated with Bing Speech, the other hostnames "region.stt.speech.microsoft.com" are associated to Speech Service or Custom Speech services.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/hostname.png)

### Entering your subscription Key and your customEndpointID
Once the hostname is selected, you can enter your subscription key which will be used for the communication with Speech-To-Text Cognitive Services. If you select Custom Speech you need to enter the Custom Speech Endpoint ID as well. 

Bing speech selection:
![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/bingkey.png)

Custom speech selection:
![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/customkey.png)

Speech service selection:
![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/speechkey.png)


### Recording Spoken Audio into a WAV file
With the application you can record the spoken audio. 
Click on the button "Record" to start the recording.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/record.png)


after few seconds click on the same button "Stop Record" to stop the recording.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/stoprecordinginfile.png)

And then select the WAV file where you want to store the recording.
The path of the WAV file is automatically copied in the Edit box "Path" used to select a WAV file.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/path.png) 


### Playing a WAV file
In order to play a WAV file click on the button "Open WAV File" to select a WAV file.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/openfile.png) 

Then click on the "Play" button.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/play.png) 

#### Start over, play, pause and stop 

Once the application is playing an audio file it's possible to:
<p/>
- pause/play the current asset
- start over the current asset
- stop the current asset 

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/playpause.png)


#### Mute and Audio level
Once the application is playing an audio file it's possible to switch off the audio (`Mute` button) or change the audio output level (`Audio+` and `Audio-` button)

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/audio.png)


### Converting Spoken Audio WAV file to Text (Speech-To-Text)
With the application, you can convert to text the WAV file you have just recorded. 
First, check the path of the your audio file is correct in the Path Edit box,
then select the language in the "Speech-To-Text Language" Combo Box: 

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/language.png)

You can select the way to exchange information with the Cognitive Services either through the REST API or through the Web Socket protocol. Web socket protocol support conversation mode, which is not the case for the REST API: 

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/rest.png)
![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/websocket.png)

You can select the conversation model: interactive, conversation, dictation with "Cognitive Services Speech Recognition API"  Combo Box. the conversation mode is only available when the WebSocket protocol is selected: 

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/mode.png)

You can also select the result type: simple or detailed  with "Cognitive Services Speech Result type"  Combo Box: 

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/resulttype.png)


Finally click on the button "Upload" to upload the file towards the Cognitive Services.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/upload.png) 

Once the file is uploaded, after less than one second, the result is displayed in the "Result" Edit box: 

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/result.png)

### Converting Live Spoken Audio to Text (Speech-To-Text)
You can also directly convert the live Spoken Audio to text. 
First, select the language in the "Language" Combo Box: 

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/language.png).

Then click on the button "Convert" to start the recording of Live Spoken Audio.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/liverecord.png) 

after few seconds click on the same button "Stop Convert" to stop the recording and transmit the audio buffer to Cognitive Services.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/stopliverecordbutton.png) 

After less than one second, the result is displayed in the "Result" Edit box: 

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/result.png)


### Converting continuously Live Spoken Audio to Text (Speech-To-Text)
You can also directly convert the live Spoken Audio continuously to text. 
First, select the language in the "Language" Combo Box: 

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/language.png).

For continuous recording, you need to select conversation mode and WebSocket communication.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/continuous.png).

Then click on the button "Continuous Record" to start the recording of Live Spoken Audio.

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/continuousrecord.png) 

Now you can speak, you can see the audio level in cyan

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/audiolevel.png)


IF you want to stop the continuous recording click on the same button:

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/stopcontinuousrecord.png) 

### Converting text to speech  (Text-To-Speech)
With the application, you can also convert a text into speech . 
First, enter your text in the Result text box, 
then select the language in the "Text-To-Speech Language" Combo Box: 

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/language.png)

You can select the speech gender Male or Female with the "Gender" Combo Box:  

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/texttospeech.png)

Then click on the TextToSpeech button to get the WAV stream associated with the text and the current language:  

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/SpeechToTextUWPSampleApp/Docs/texttospeechbutton.png)






Building the application
----------------

1. If you download the samples ZIP, be sure to unzip the entire archive, not just the folder with the sample you want to build. 
2. Ensure the Red Stone 1 (RS1) Windows 10 SDK is installed on your machine
3. Start Microsoft Visual Studio 2015 and select **File** \> **Open** \> **Project/Solution**.
3. Starting in the folder where you unzipped the samples, go to the Samples subfolder, then the subfolder for this specific sample, then the subfolder for your preferred language (C++, C#, or JavaScript). Double-click the Visual Studio 2015 Solution (.sln) file.
4. Press Ctrl+Shift+B, or select **Build** \> **Build Solution**.


**Deploying and running the sample**
1.  To debug the sample and then run it, press F5 or select **Debug** \> **Start Debugging**. To run the sample without debugging, press Ctrl+F5 or select**Debug** \> **Start Without Debugging**.

Next steps
--------------

The Speech-To-Text UWP Sample Applicaton could be improved to support the following features:
<p/>

1. Integration with LUIS Cognitive Services for continuous recording
2. Support of MP3, AAC, WMA audio files 






