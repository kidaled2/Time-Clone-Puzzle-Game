[CmdletBinding()]
param(
    [string]$UnityPath,
    [string]$TestFilter = '',
    [ValidateRange(1, 120)]
    [int]$TimeoutMinutes = 10
)

$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$versionFile = Join-Path $projectPath 'ProjectSettings\ProjectVersion.txt'
$editorVersion = (Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion: (\S+)').Matches[0].Groups[1].Value

if (-not $UnityPath) {
    $UnityPath = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
}

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity $editorVersion was not found at '$UnityPath'. Pass -UnityPath with the matching Unity.exe path."
}
$UnityPath = (Resolve-Path -LiteralPath $UnityPath).ProviderPath

$runId = '{0}-{1}' -f (Get-Date -Format 'yyyyMMdd-HHmmssfff'), $PID
$runPath = Join-Path $projectPath "Logs\PlayMode\$runId"
$null = New-Item -Path $runPath -ItemType Directory -Force
$logPath = Join-Path $runPath 'Unity.log'
$resultsPath = Join-Path $runPath 'results.xml'

$unityArguments = @(
    '-batchmode', '-nographics', '-runTests', '-forgetProjectPath',
    '-projectPath', ('"{0}"' -f $projectPath),
    '-testPlatform', 'PlayMode',
    '-testResults', ('"{0}"' -f $resultsPath),
    '-logFile', ('"{0}"' -f $logPath)
)
if ($TestFilter) {
    if ($TestFilter.Contains('"') -or $TestFilter.Contains("`r") -or $TestFilter.Contains("`n")) {
        throw 'TestFilter must be a single line without quotation marks.'
    }
    $unityArguments += @('-testFilter', ('"{0}"' -f $TestFilter))
}

Write-Host "Unity log: $logPath"
Write-Host "Test results: $resultsPath"
$process = Start-Process -FilePath $UnityPath -ArgumentList $unityArguments -WorkingDirectory $projectPath -WindowStyle Hidden -PassThru
try {
    if (-not $process.WaitForExit($TimeoutMinutes * 60 * 1000)) {
        throw "PlayMode run timed out. Unity process $($process.Id) is still running. Check the Unity log before starting another run."
    }

    if ($process.ExitCode -ne 0) {
        throw "Unity exited with code $($process.ExitCode). See $logPath and $resultsPath."
    }

    if (-not (Test-Path -LiteralPath $resultsPath -PathType Leaf)) {
        throw "Unity did not produce test results. See $logPath."
    }

    [xml]$results = Get-Content -LiteralPath $resultsPath -Raw
    $testRun = $results.SelectSingleNode('/test-run')
    if ($null -eq $testRun -or $testRun.GetAttribute('result') -ne 'Passed' -or
        [int]$testRun.GetAttribute('passed') -lt 1 -or [int]$testRun.GetAttribute('failed') -gt 0) {
        throw "PlayMode run failed or no tests passed. See $resultsPath."
    }

    Write-Host "$($testRun.GetAttribute('passed')) PlayMode tests passed."
}
finally {
    $process.Dispose()
}
