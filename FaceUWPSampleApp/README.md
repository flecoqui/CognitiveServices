<!---
  category: AudioVideoAndCamera
  samplefwlink: http://go.microsoft.com/fwlink/p/?LinkId=620563&clcid=0x409
--->

# Cognitive Services Face UWP Sample Application

Overview
--------------
This Face UWP Sample Application  can:

- **Create, Update and Delete a group of persons** 
- **Create, Update and Delete a person in a group of persons** 
- **Associated a picture with a person** 
- **Train a model for a group of person** 
- **Take a picture and try to identify the persons** 

In order to use the application you need a Cognitive Services Face key.
You can sign up [here](https://www.microsoft.com/cognitive-services/en-us/sign-up)  


Installing the application
----------------------------
You can install the application on:

- **Personal Computer Platform**: a desktop running Windows 10
- **Windows 10 Mobile Platform**: a phone running Windows 10RS1


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
6.  Tap on the file FaceUWPSampleApp_1.0.XX.O_x86_x64_arm.cer to install the certificate.
7.  Tap on the file FaceUWPSampleApp_1.0.XX.O_x86_x64_arm.appxbundle to install the application


Using the application with Face Cognitive Service
----------------------------
Once the application is installed on your device, you can launch it and the main page will be displayed after few seconds.

### Main page

To be completed

![](https://raw.githubusercontent.com/flecoqui/CognitiveServices/master/FaceUWPSampleApp/Docs/main.png)

The application is used to take picture, open picture and analyze the picture with Cognitive Services.




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

The Face UWP Sample Applicaton could be improved to support the following features:
<p/>

1. Better Camera usage
2. Display results over the preview stream using native UWP Face API 






