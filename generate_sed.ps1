$publishDir = "d:\code\pc\har\publish"
$outputSed = "d:\code\pc\har\setup.sed"
$outputExe = "d:\code\pc\har\HarManagerSetup.exe"

$files = Get-ChildItem -Path $publishDir -File

$sedContent = @"
[Version]
Class=IEXPRESS
SEDVersion=3.0
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=
DisplayLicense=
FinishMessage=
TargetName=$outputExe
FriendlyName=HarManager Installer
AppLaunched=cmd.exe /c install.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles

[Strings]
FILE0="install.cmd"
FILE1="create_shortcut.ps1"
"@

$fileIndex = 2
foreach ($file in $files) {
    if ($file.Name -ne "install.cmd" -and $file.Name -ne "create_shortcut.ps1") {
        $sedContent += "`nFILE$fileIndex=`"$($file.Name)`""
        $fileIndex++
    }
}

$sedContent += @"

[SourceFiles]
SourceFiles0=$publishDir\

[SourceFiles0]
%FILE0%=
%FILE1%=
"@

$fileIndex = 2
foreach ($file in $files) {
    if ($file.Name -ne "install.cmd" -and $file.Name -ne "create_shortcut.ps1") {
        $sedContent += "`n%FILE$fileIndex%="
        $fileIndex++
    }
}

Set-Content -Path $outputSed -Value $sedContent -Encoding ASCII
Write-Host "SED file generated at $outputSed"

