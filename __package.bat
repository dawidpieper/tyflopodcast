SET ILMERGE_VERSION=3.0.40
SET ILMERGE_PATH=%USERPROFILE%\.nuget\packages\ilmerge\%ILMERGE_VERSION%\tools\net452
set path=%path%;%ILMERGE_PATH%
md bin 2>nul
md bin\lib32 2>nul
md bin\lib64 2>nul
copy /y lib32\*.dll bin\lib32\*.dll 1>nul
copy /y lib64\*.dll bin\lib64\*.dll 1>nul
ilmerge tp.exe lib/Newtonsoft.Json.dll lib/Bass.Net.dll /out:bin\tyflopodcast.exe
del bin\tyflopodcast.pdb