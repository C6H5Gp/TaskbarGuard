@echo off
cd /d "%~dp0"
%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:TaskbarGuard.exe /target:exe src\TaskbarGuard.cs
%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /out:HideTaskbar.exe /target:exe src\HideTaskbar.cs
echo BUILD OK
