$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (!(Test-Path $csc)) { throw '.NET Framework compiler bulunamadi.' }
New-Item -ItemType Directory -Force dist | Out-Null
& $csc /nologo /target:winexe /platform:anycpu /optimize+ /out:dist/NiyazisugarProxy.exe /win32manifest:app.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll MainForm.cs ClientFiles.cs Updates.cs Version.cs
if ($LASTEXITCODE -ne 0) { throw 'App build failed' }
& $csc /nologo /target:exe /out:dist/Tests.exe /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll Updates.cs ClientFiles.cs Tests.cs
if ($LASTEXITCODE -ne 0) { throw 'Test build failed' }
& ./dist/Tests.exe
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
