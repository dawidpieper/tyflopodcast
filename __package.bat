@echo off
SET ILMERGE_VERSION=3.0.41
SET ILMERGE_PATH=%USERPROFILE%\.nuget\packages\ilmerge\%ILMERGE_VERSION%\tools\net452
SET INNOSETUP_PATH=C:\Program Files (x86)\Inno Setup 6
SET SEVENZIP_PATH=C:\Program Files\7-Zip
set path=%path%;%ILMERGE_PATH%;%INNOSETUP_PATH%;%SEVENZIP_PATH%
md bin 2>nul
md bin\lib32 2>nul
md bin\lib64 2>nul
copy /y lib32\*.dll bin\lib32\*.dll 1>nul
copy /y lib64\*.dll bin\lib64\*.dll 1>nul
ilmerge tp.exe lib/Newtonsoft.Json.dll lib/Bass.Net.dll lib/HtmlAgilityPack.dll /out:bin\tyflopodcast.exe
del bin\tyflopodcast.pdb
del /Q out\*

signtool sign /n "Dawid Pieper" /t http://time.certum.pl /fd sha256 /v bin\tyflopodcast.exe

iscc installer.iss

signtool sign /n "Dawid Pieper" /t http://time.certum.pl /fd sha256 /v out\tyflopodcast_setup.exe

7z a out/tyflopodcast.zip bin\
7z rn out\tyflopodcast.zip bin\ tyflopodcast\