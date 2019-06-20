<!---
  category: AudioVideoAndCamera
  samplefwlink: http://go.microsoft.com/fwlink/p/?LinkId=620563&clcid=0x409
--->

# Double-Speech-To-Text UWP Sample Application

Overview
--------------
This sample application can use the following Cognitive Services:
- **Microsoft Speech API** or **Bing Speech** services: documentation is available [here](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/home)
- **Custom Speech** service: the documentation is available [here](https://docs.microsoft.com/en-us/azure/cognitive-services/custom-speech-service/cognitive-services-custom-speech-home)
- **Speech Service** (Preview): the documentation is available [here](https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/)

Those 3 services are accessible through:

- a **REST API for the Double-Speech-To-Text** described [here](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech/getstarted/getstartedrest?tabs=Powershell)  

- a **WebSocket protocol for the Double-Speech-To-Text** described [here](https://docs.microsoft.com/en-us/azure/cognitive-services/Speech/api-reference-rest/websocketprotocol) 


This Double-Speech-To-Text  UWP Sample Application  can:

- **Convert continuously 2 live audio streams into text**: Convert continuously 2 live audio to text with Cognitive Services using the conversation mode only available through the WebSocket API.

The spoken audio is converted into a WAV format before calling the WebSocket:

- **Number of Channels**: one channel, 
- **Samples per second**: 16000,
- **Bits per sample**: 16 bits,
- **Average Bytes per second**: 256 kbit/s.

In order to use the application you need a Cognitive Services Double-Speech-To-Text subscription Key.
You can sign up [here](https://www.microsoft.com/cognitive-services/en-us/sign-up)  


Installing the application
----------------------------
You can install the application on:

- **Personal Computer Platform**: a desktop running Windows 10 RS1
- **Windows 10 Mobile Platform**: a phone running Windows 10 RS1

The applications packages for x86, x64 and ARM are available there :
[ZIP file of the application x86, x64, ARM Packages](https://github.com/flecoqui/CognitiveServices/raw/master/DoubleSpeechToTextUWPSampleApp/Releases/LatestRelease.zip)


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
6.  Tap on the file DoubleSpeechToTextUWPSampleApp_1.0.XX.O_x86_x64_arm.cer to install the certificate.
7.  Tap on the file DoubleSpeechToTextUWPSampleApp_1.0.XX.O_x86_x64_arm.appxbundle to install the application


Using the application
----------------------------
Once the application is installed on your device, you can launch it and the main page will be displayed after few seconds.

### Main page

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/DoubleSpeechToTextUWPSampleApp/Docs/main.png)

The application is used to record spoken audio into a WAV file, play the WAV files stored on the local disk, convert the WAV file to text, convert live audio to text and convert continuously live audio to text .



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

The Double-Speech-To-Text UWP Sample Applicaton could be improved to support the following features:
<p/>

1. Integration with LUIS Cognitive Services for continuous recording
2. Support of MP3, AAC, WMA audio files 






