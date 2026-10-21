param([switch]$Test, [switch]$DesktopTest)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = $PSScriptRoot
$outputRoot = Join-Path $projectRoot 'dist'
$artifactRoot = Join-Path $projectRoot 'artifacts'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x C# compiler was not found.' }
New-Item -ItemType Directory -Force -Path $outputRoot, $artifactRoot | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
$executable = Join-Path $outputRoot 'BeYourself.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /warnaserror+ /codepage:65001 "/out:$executable" "/win32manifest:$projectRoot\app.manifest" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll $sources
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'App.config') -Destination ($executable + '.config') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $outputRoot 'README.md') -Force

if ($Test -or $DesktopTest) {
    $testExecutable = Join-Path $artifactRoot 'BeYourself.Tests.exe'
    & $compiler /nologo /target:exe /platform:anycpu /optimize+ /warnaserror+ /codepage:65001 "/out:$testExecutable" "/win32manifest:$projectRoot\app.manifest" "/reference:$executable" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll (Join-Path $projectRoot 'tests\SelfTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    Copy-Item -LiteralPath $executable -Destination $artifactRoot -Force
    Copy-Item -LiteralPath ($executable + '.config') -Destination ($testExecutable + '.config') -Force
    $testArguments = @($artifactRoot)
    if ($DesktopTest) { $testArguments += '--desktop' }
    & $testExecutable @testArguments
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
$package = Join-Path $outputRoot 'BeYourself-v1.0.1-windows.zip'
Compress-Archive -LiteralPath $executable, ($executable + '.config'), (Join-Path $outputRoot 'README.md') -DestinationPath $package -Force
Get-Item -LiteralPath $executable, $package | Select-Object Name, Length
