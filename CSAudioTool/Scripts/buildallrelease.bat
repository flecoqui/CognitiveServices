dotnet publish ..\CSAudioTool --self-contained -c Release -r win-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.win.zip ..\CSAudioTool\bin\Release\netcoreapp3.0\win-x64\publish\
dotnet publish ..\CSAudioTool  --self-contained -c Release -r linux-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.linux.tar ..\CSAudioTool\bin\Release\netcoreapp2.2\linux-x64\publish\
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.linux.tar.gz c:\temp\LatestRelease.linux.tar 
dotnet publish ..\CSAudioTool  --self-contained -c Release -r linux-musl-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.linux-musl.tar ..\CSAudioTool\bin\Release\netcoreapp2.2\linux-x64\publish\
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.linux-musl.tar.gz c:\temp\LatestRelease.linux-musl.tar 
dotnet publish ..\CSAudioTool  --self-contained -c Release -r rhel-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.rhel.tar ..\CSAudioTool\bin\Release\netcoreapp2.2\rhel-x64\publish\
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.rhel.tar.gz c:\temp\LatestRelease.rhel.tar 
dotnet publish ..\CSAudioTool  --self-contained -c Release -r osx-x64
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.osx.tar ..\CSAudioTool\bin\Release\netcoreapp2.2\osx-x64\publish\
"C:\Program Files\7-Zip\7z.exe" a c:\temp\LatestRelease.osx.tar.gz c:\temp\LatestRelease.osx.tar 
copy /Y c:\temp\LatestRelease.win.zip ..\Releases\LatestRelease.win.zip
copy /Y c:\temp\LatestRelease.linux.tar.gz ..\Releases\LatestRelease.linux.tar.gz
copy /Y c:\temp\LatestRelease.linux-musl.tar.gz ..\Releases\LatestRelease.linux-musl.tar.gz
copy /Y c:\temp\LatestRelease.rhel.tar.gz ..\Releases\LatestRelease.rhel.tar.gz
copy /Y c:\temp\LatestRelease.osx.tar.gz ..\Releases\LatestRelease.osx.tar.gz




