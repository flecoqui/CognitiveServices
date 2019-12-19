<img src="Docs/CSAudioTOOL_logo.png">

# What is CSAudioTool?
CSAudioTool is a Cognitive Service Audio Tool used to prepare and test audio files with Microsoft Cognitive Speech Services. The first version is specifically dedicated to Microsoft Cognitive Speech Services .
For instance, with CSAudioTool version 1.0 you can
- Parse a WAV file,
- Play a WAV file,
- Capture a WAV file from the Microphone Input, the WASAPI loopback input
- Convert a WAV file in a format compliant with Cognitive Services (16000 KHZ, 16 bits, 1 channel)
- Get the transcript of a WAV stream coming either from a WAV file, the Microphone Input or the WASAPI loopback input

As CSAudioTool is based on .Net Core, the application can be installed on any operating system supporting .Net Core (Windows, Mac OS, Ubuntu, Debian, Centos, Red Hat).
The current version is limited to Windows operating system.

<img src="Docs/CSAudioTOOL_Architecture.png" width="600">


The script to build the different release for each operating system is avaiable [here](https://github.com/flecoqui/CognitiveServices/tree/master/CSAudioTool/Scripts)


- [Windows latest release](https://github.com/flecoqui/CognitiveServices/raw/master/CSAudioTool/Releases/LatestRelease.win.zip) </p>

    [win-download]:                 https://github.com/flecoqui/CognitiveServices/raw/master/CSAudioTool/Releases/LatestRelease.win.zip
    [CSAudioTool-version-badge]:            https://github.com/flecoqui/flecoqui.github.io/blob/master/CognitiveServices/csaudiotoolbuild.svg
    [![Github Release][CSAudioTool-version-badge]][win-download]


- [Linux (Ubuntu, Centos, Debian,...)  latest release](https://github.com/flecoqui/CognitiveServices/raw/master/CSAudioTool/Releases/LatestRelease.linux.tar.gz)</p>

    [linux-download]:                 https://github.com/flecoqui/CognitiveServices/raw/master/CSAudioTool/Releases/LatestRelease.linux.tar.gz
    [CSAudioTool-version-badge]:            https://github.com/flecoqui/flecoqui.github.io/blob/master/CognitiveServices/csaudiotoolbuild.svg
    [![Github Release][CSAudioTool-version-badge]][linux-download]


- [Linux (Lightweight distributions using musl like Alpine Linux) latest release](https://github.com/flecoqui/CognitiveServices/raw/master/CSAudioTool/Releases/LatestRelease.linux-musl.tar.gz)</p>

    [linux-musl-download]:                 https://github.com/flecoqui/CognitiveServices/raw/master/CSAudioTool/Releases/LatestRelease.linux-musl.tar.gz
    [CSAudioTool-version-badge]:            https://github.com/flecoqui/flecoqui.github.io/blob/master/CognitiveServices/csaudiotoolbuild.svg
    [![Github Release][CSAudioTool-version-badge]][linux-musl-download]


- [Red Hat latest release](https://github.com/flecoqui/CognitiveServices/raw/master/CSAudioTool/Releases/LatestRelease.rhel.tar.gz)</p>


    [rhel-download]:                 https://github.com/flecoqui/CognitiveServices/raw/master/CSAudioTool/Releases/LatestRelease.rhel.tar.gz
    [CSAudioTool-version-badge]:            https://github.com/flecoqui/flecoqui.github.io/blob/master/CognitiveServices/csaudiotoolbuild.svg
    [![Github Release][CSAudioTool-version-badge]][rhel-download]


- [Mac OS latest release](https://github.com/flecoqui/CognitiveServices/raw/master/CSAudioTool/Releases/LatestRelease.osx.tar.gz)</p>


    [osx-download]:                 https://github.com/flecoqui/CognitiveServices/raw/master/CSAudioTool/Releases/LatestRelease.osx.tar.gz
    [CSAudioTool-version-badge]:            https://github.com/flecoqui/flecoqui.github.io/blob/master/CognitiveServices/csaudiotoolbuild.svg
    [![Github Release][CSAudioTool-version-badge]][osx-download]





# Required Software
|[![Windows](Docs/windows_logo.png)](https://docs.microsoft.com/en-us/dotnet/core/windows-prerequisites?tabs=netcore2x)[Windows pre-requisites](https://docs.microsoft.com/en-us/dotnet/core/windows-prerequisites?tabs=netcore2x)|[![Linux](Docs/linux_logo.png)](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x) [Linux pre-requisites](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x)|[![MacOS](Docs/macos_logo.png)](https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore2x)  [Mac OS pre-requisites](https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore2x)|
| :--- | :--- | :--- |
| .NET Core is supported on the following versions of Windows 7 SP1, Windows 8.1, Windows 10 (version 1607) or later versions, Windows Server 2008 R2 SP1, Windows Server 2012 SP1, Windows Server 2012 R2, Windows Server 2016 or later versions | The Linux pre-requisites depends on the Linux distribution. Click on the link above to get further information &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;| .NET Core 2.x is supported on the following versions of macOS macOS 10.12 "Sierra" and later versions &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;|



# Features area

The Cognitive Service Audio Tool (CSAudioTool) is an Open Source command line tool supporting several features. This chapter describes how to launch a feature from a command line.

##  Transcript feature: 
This feature allow the user to get the transcript in different language (en-US, fr-FR, en-GB, de-DE, ...) for the source audio stream (microphone, loopback WASAPI, WAV file).
To test this feature you need a Speech Service Account, the Key associated with this account and the region where the speech service is deployed will be required.

### Syntax

Command to launch the transcript from a WAV file:

    csaudiotool --transcript --input <WAV Audio File path> --region <AzureRegion> --key <AccountKey>
                             --language <Language> 

Command to launch the transcript from a microphone or a loopback input:

    csaudiotool --transcript --input <AudioSource> --region <AzureRegion> --key <AccountKey>
                             --language <Language> [--device <CaptureDeviceID>]            



| option<img width=230/>| value type | default value | Description | 
| :--- | :--- | :--- | :--- | 
|--input| string | null | Path to the local WAV file (This file must support 16000KHZ sample rate, 16 bits, and 1 channel)|
|--input| string | null | for a transcript from a microphone the value is 'microphone', from a loopback input 'loopback'|
|--region| string | null | Azure region where the Speech Service is deployed for instance: northeurope, eastus2, |
|--key| string | null | The key associated with the Speech Service |
|--language| string | null  | The language associated with the audio source, for instance: 'fr-FR', 'en-US', 'de-DE', 'it-IT'|
|--device| string | string | This option is only use when capturing the audio stream from microphone or loopback input. The parameter is the DeviceId associated with this audio source you can get the device ID using the Device feature described below in this document|


### Examples

Transcript with loopback input to capture the speaker output:

    csaudiotool.exe --transcript --input loopback --region northeurope --key  14f47d6474644eacabf62e73e604fa22 --language fr-FR

Transcript with microphone input to capture the microphone output:

    csaudiotool.exe --transcript --input microphone --region northeurope --key  14f47d6474644eacabf62e73e604fa22 --language fr-FR

Transcript with stereo mix input (if avaiable on your machine) using device ID to capture the speaker output:

    csaudiotool.exe --transcript --input microphone --region northeurope --key  14f47d6474644eacabf62e73e604fa22 --language fr-FR --device {0.0.1.00000000}.{e31952f5-2c78-4696-bc13-fe9c3573b512}



##  Capture feature: 
This feature allow the user to capture the audio stream from the microphone or the loopback input into a local WAV File.

### Syntax

Command to launch the transcript from a WAV file:

    csaudiotool --capture --input <AudioSource> --output <WAV Audio File path> --duration <Duration in ms> 
                 [--device <CaptureDeviceID>]  


| option<img width=200/> | value type | default value | Description | 
| :--- | :--- | :--- |  :--- | 
|--input| string | null | for a transcript from a microphone the value is 'microphone', from a loopback input 'loopback'|
|--output| string | null | Path to the local WAV file where the stream will be stored|
|--duration| long | null | Duration in ms of the recording |
|--device| string | string | This option is only use when capturing the audio stream from microphone or loopback input. The parameter is the DeviceId associated with this audio source you can get the device ID using the Device feature described below in this document|

### Examples

Capture the audio source from the loopback input into a local file for a duration of 10 seconds:

    csaudiotool.exe --capture  --input loopback --output local.wav --duration 10000

Capture the audio source from the stereo mix input into a local file for a duration of 10 seconds:

    csaudiotool.exe --capture --input microphone --output local1.wav --duration 10000 --device {0.0.1.00000000}.{e31952f5-2c78-4696-bc13-fe9c3573b512}

##  Convert feature: 
This feature allow the users to convert the WAV files into a format supported by Microsoft Cognitive Services sample rate 16000 khz, 16 bits for each sample and 1 channel.

### Syntax

Command to convert a WAV file:

    csaudiotool --convert --input <Source WAV File> --output <New WAV File> --format <new format> 


| option<img width=100/>| value type | default value | Description | 
| :--- | :--- | :--- |  :--- | 
|--input| string | null | Path to the source local WAV file to convert |
|--output| string | null | Path of the new local WAV file converted in the expected format|
|--format| string | string | String which define the format of the new WAV File. The format of the string is <SampleRate>_<Channels> . For instance: 16000_1 for a conversion into a 16KHZ and mono channel file.|

### Examples

Convert the input WAV file into a 16KHZ mono WAV file:

    csaudiotool.exe --convert  --input local.wav --output local16khz.wav --format 16000_1



##  Parse feature: 
With this feature the user can display for format of any WAV file

### Syntax

    CSAudioTool --parse    --input <local WAV file> 

| option | value type | default value | Description | 
| :--- | :--- | :--- |  :--- | 
|--input| string | null | Path to the local WAV file on the disk|


### Examples

Parsing an WAV  file and displaying the WAV header :

    csaudiotool.exe --parse --input local.wav



##  Play feature: 
With this feature the user can play a WAV file

### Syntax

    CSAudioTool --play    --input <local WAV file> 

| option | value type | default value | Description | 
| :--- | :--- | :--- |  :--- | 
|--input| string | null | Path to the local WAV file on the disk to play|


### Examples

Playing a WAV file:

    csaudiotool.exe --play --input local.wav



# Building CSAudioTool
If you want to build CSAudioTool on your machine, you need first to install all the pre-requisites to run .Net Core on your machine, check in the table below based on your current Operating System:

## Pre-requisites

|[![Windows](Docs/windows_logo.png)](https://docs.microsoft.com/en-us/dotnet/core/windows-prerequisites?tabs=netcore2x)[Windows pre-requisites](https://docs.microsoft.com/en-us/dotnet/core/windows-prerequisites?tabs=netcore2x)|[![Linux](Docs/linux_logo.png)](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x) [Linux pre-requisites](https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x)|[![MacOS](Docs/macos_logo.png)](https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore2x)  [Mac OS pre-requisites](https://docs.microsoft.com/en-us/dotnet/core/macos-prerequisites?tabs=netcore2x)|
| :--- | :--- | :--- |
| .NET Core is supported on the following versions of Windows 7 SP1, Windows 8.1, Windows 10 (version 1607) or later versions, Windows Server 2008 R2 SP1, Windows Server 2012 SP1, Windows Server 2012 R2, Windows Server 2016 or later versions | The Linux pre-requisites depends on the Linux distribution. Click on the link above to get further information &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;| .NET Core 2.x is supported on the following versions of macOS macOS 10.12 "Sierra" and later versions &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;&nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp; &nbsp;|

## Installing git and .Net Core SDK version 2.1

Once the pre-requisites are installed, you need to install:

- Git from https://github.com/
- .Net Core SDK version 2.1 or later from https://dot.net/
- Clone CSAudioTool github repository on your machine
  For instance on a machine running linux:

        mkdir /git
        cd /git
        git clone https://github.com/flecoqui/CognitiveServices.git 
        cd CognitiveServices/CSAudioTool/CSAudioTool


## Building the self-contained binaries

You are now ready to build CSAudioTool binaries, as CSAudioTool needs to be easy to install and doesn't require the installation before of .Net Core, you can build Self Contained binaries of CSAudioTool which doesn't require the installation of .Net Core.

For instance you can run the following commands to build the different flavors of CSAudioTool:

    cd /git/CognitiveServices/CSAudioTool/CSAudioTool/
    dotnet publish --self-contained -c Release -r win-x64
    dotnet publish --self-contained -c Release -r centos-x64
    dotnet publish --self-contained -c Release -r rhel-x64
    dotnet publish --self-contained -c Release -r ubuntu-x64
    dotnet publish --self-contained -c Release -r debian-x64
    dotnet publish --self-contained -c Release -r osx-x64

The Command lines above built the CSAudioTool binaries for Windows, Centos, RedHat, Ubuntu, Debian and Mac OS.

When you run the following command:
    
    dotnet publish --self-contained -c Release -r [RuntimeFlavor]

the binaries will be available under:

    /git/CSAudioTool/cs/CSAudioTool/CSAudioTool/bin/Release/netcoreapp2.0/[RuntimeFlavor]/publish


# Next Steps

1. Port  CSAudioTool on MacOS and Linux
