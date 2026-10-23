[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $PublishDirectory,
    [string] $ResultsDirectory = 'artifacts/aot-validation',
    [switch] $SmokeTest,
    [switch] $DisplayTest,
    [switch] $RequireAudio
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$publishRoot = (Resolve-Path -LiteralPath $PublishDirectory).Path
$resultsRoot = [IO.Path]::GetFullPath($ResultsDirectory)
$runningOnWindows = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
if ($RequireAudio -and -not ($SmokeTest -or $DisplayTest)) { throw '-RequireAudio requires -SmokeTest or -DisplayTest.' }
$executable = Join-Path $publishRoot $(if ($runningOnWindows) { 'MonoGalGame.Demo.exe' } else { 'MonoGalGame.Demo' })

foreach ($file in @('Game/game.json', 'Game/Stories/main.json', 'Game/Fonts/NotoSansSC-Regular.otf', 'Game/Fonts/OFL.txt', 'Game/Audio/spring.wav', 'THIRD-PARTY-NOTICES.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $publishRoot $file) -PathType Leaf)) {
        throw "Missing published file: $file"
    }
}
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw "Missing executable: $executable" }
foreach ($managedFile in @('MonoGalGame.Demo.dll', 'MonoGalGame.Demo.runtimeconfig.json')) {
    if (Test-Path -LiteralPath (Join-Path $publishRoot $managedFile)) {
        throw "Found $managedFile; publish Native AOT into a fresh directory."
    }
}
New-Item -ItemType Directory -Path $resultsRoot -Force | Out-Null

function Invoke-NativeValidation([string] $Name, [string] $NativeArguments) {
    $stdout = Join-Path $resultsRoot "$Name.stdout.txt"
    $stderr = Join-Path $resultsRoot "$Name.stderr.txt"
    $processOptions = @{
        FilePath = $executable
        ArgumentList = $NativeArguments
        # Verify all content is resolved from the executable, independently of the shell's directory.
        WorkingDirectory = [IO.Path]::GetTempPath()
        RedirectStandardOutput = $stdout
        RedirectStandardError = $stderr
        Wait = $true
        PassThru = $true
    }
    if ($runningOnWindows) { $processOptions.WindowStyle = 'Hidden' }
    $validation = Start-Process @processOptions
    Get-Content -LiteralPath $stdout, $stderr -Encoding utf8
    if ($validation.ExitCode -ne 0) { throw "$Name failed (exit code $($validation.ExitCode)). See $stderr" }
}

Invoke-NativeValidation 'validate' '--validate --require-aot'
if ($SmokeTest -or $DisplayTest) {
    $smokeRoot = Join-Path $resultsRoot 'smoke'
    $nativeArguments = '--require-aot --smoke-test "' + $smokeRoot + '"'
    if ($DisplayTest) { $nativeArguments += ' --display-test' }
    Invoke-NativeValidation 'smoke' $nativeArguments
    $success = Get-Content -LiteralPath (Join-Path $smokeRoot 'success.json') -Raw -Encoding utf8 | ConvertFrom-Json
    if (-not $success.Passed -or
        $success.Runtime.IsDynamicCodeSupported -or
        $success.Runtime.IsDynamicCodeCompiled -or
        $success.Runtime.HasManagedEntryAssembly) {
        throw 'Smoke test did not complete under Native AOT.'
    }
    if ($DisplayTest -and -not $success.DisplayTest) { throw 'Display validation did not complete.' }
    if ($RequireAudio -and $success.AudioError) { throw "Audio validation failed: $($success.AudioError)" }
}
Write-Output "Native AOT validation passed: $executable"
