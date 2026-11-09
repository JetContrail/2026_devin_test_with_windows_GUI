<#
.SYNOPSIS
    Measures DrawingApp startup time (process start -> main window ready for input) and idle memory.
.DESCRIPTION
    Starts the executable -Runs times, closes it after each run and reports every run plus the median.
    Fails (exit 1) when the median exceeds -MaxStartupSeconds or -MaxMemoryMB (working set).
    Writes a Markdown table to $env:GITHUB_STEP_SUMMARY when it is set.
#>
param(
    [string] $Exe = 'publish/DrawingApp/DrawingApp.exe',
    [int] $Runs = 5,
    [double] $MaxStartupSeconds = 3,
    [double] $MaxMemoryMB = 300,
    [int] $SettleSeconds = 2
)

$ErrorActionPreference = 'Stop'
$Exe = (Resolve-Path $Exe).Path

function Get-Median([double[]] $values) {
    $sorted = $values | Sort-Object
    $mid = [math]::Floor($sorted.Count / 2)
    if ($sorted.Count % 2) { return $sorted[$mid] }
    return ($sorted[$mid - 1] + $sorted[$mid]) / 2
}

$results = foreach ($run in 1..$Runs) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath $Exe -PassThru
    try {
        while ($process.MainWindowHandle -eq [IntPtr]::Zero) {
            if ($process.HasExited) { throw "DrawingApp exited during startup (exit code $($process.ExitCode))." }
            if ($watch.Elapsed.TotalSeconds -gt 30) { throw 'The main window did not appear within 30 seconds.' }
            Start-Sleep -Milliseconds 10
            $process.Refresh()
        }
        $null = $process.WaitForInputIdle(10000)
        $startup = $watch.Elapsed.TotalSeconds

        Start-Sleep -Seconds $SettleSeconds
        $process.Refresh()
        [pscustomobject]@{
            Run            = $run
            StartupSeconds = [math]::Round($startup, 2)
            WorkingSetMB   = [math]::Round($process.WorkingSet64 / 1MB, 1)
            PrivateMB      = [math]::Round($process.PrivateMemorySize64 / 1MB, 1)
        }
    }
    finally {
        if (-not $process.HasExited) {
            $null = $process.CloseMainWindow()
            if (-not $process.WaitForExit(10000)) { $process.Kill() }
        }
    }
}

$startupMedian = Get-Median ($results | ForEach-Object StartupSeconds)
$memoryMedian = Get-Median ($results | ForEach-Object WorkingSetMB)
$results | Format-Table | Out-String | Write-Output
$summary = "Median startup {0:N2} s (limit {1} s), working set {2:N1} MB (limit {3} MB)" -f `
    $startupMedian, $MaxStartupSeconds, $memoryMedian, $MaxMemoryMB
Write-Output $summary

if ($env:GITHUB_STEP_SUMMARY) {
    $lines = @('### Performance', '', '| Run | Startup (s) | Working set (MB) | Private (MB) |', '|---|---|---|---|')
    $lines += $results | ForEach-Object { "| $($_.Run) | $($_.StartupSeconds) | $($_.WorkingSetMB) | $($_.PrivateMB) |" }
    $lines += '', $summary
    $lines | Add-Content $env:GITHUB_STEP_SUMMARY
}

if ($startupMedian -gt $MaxStartupSeconds -or $memoryMedian -gt $MaxMemoryMB) {
    Write-Error $summary
    exit 1
}
