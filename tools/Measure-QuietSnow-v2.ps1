# Run in Windows PowerShell with QuietSnow already running.
[CmdletBinding()]
param(
    [int]$ProcessId,
    [ValidateRange(5,3600)][int]$Seconds = 30
)
$ErrorActionPreference = 'Stop'
if ($ProcessId) {
    $app = Get-Process -Id $ProcessId
} else {
    $apps = @(Get-Process -Name 'QuietSnow*' -ErrorAction SilentlyContinue)
    if ($apps.Count -ne 1) { throw 'Start one QuietSnow instance or specify -ProcessId.' }
    $app = $apps[0]
}
if (-not ('QuietSnowGuiCounters' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class QuietSnowGuiCounters {
    [DllImport("user32.dll")]
    public static extern uint GetGuiResources(IntPtr process, uint flags);
}
'@
}
$csv = Join-Path $PWD ('QuietSnow-{0}-{1}.csv' -f $app.Id, [DateTime]::Now.ToString('yyyyMMdd-HHmmss-fff'))
$stages = [ordered]@{
    desktop = 'Show the desktop, with snow visible. Leave the mouse still.'
    window_idle = 'Use a normal, unmaximized window with the garland. Focus it after Enter and leave it still.'
    drag = 'Use the same window, size and displays. After Enter, drag it repeatedly along the same path.'
    window_idle_after = 'Return the same window to its starting position. Focus it and leave it still.'
    pause = 'Pause QuietSnow from the tray. Return to the same window and leave it still.'
}
Write-Host ("Measuring {0} (PID {1}). Output: {2}" -f $app.ProcessName,$app.Id,$csv)
try {
    foreach ($stage in $stages.Keys) {
        [void](Read-Host ($stages[$stage] + ' Press Enter when ready'))
        Write-Host 'Switch back to the desktop or test window. Sampling starts in five seconds.'
        Start-Sleep 5
        $app.Refresh()
        $previousCpu = $app.TotalProcessorTime.TotalSeconds
        $watch = [Diagnostics.Stopwatch]::StartNew()
        $previousTime = 0
        while ($watch.Elapsed.TotalSeconds -lt $Seconds) {
            Start-Sleep -Seconds 1
            $app.Refresh()
            if ($app.HasExited) { throw 'QuietSnow exited during sampling.' }
            $cpu = $app.TotalProcessorTime.TotalSeconds
            $now = $watch.Elapsed.TotalSeconds
            $corePercent = 100 * ($cpu - $previousCpu) / ($now - $previousTime)
            $gdiCount = [QuietSnowGuiCounters]::GetGuiResources($app.Handle,0)
            $userCount = [QuietSnowGuiCounters]::GetGuiResources($app.Handle,1)
            [pscustomobject]@{
                stage = $stage
                process = $app.ProcessName
                pid = $app.Id
                elapsed_s = [math]::Round($now,3)
                cpu_one_core_pct = [math]::Round($corePercent,2)
                logical_processors_visible = [Environment]::ProcessorCount
                cpu_visible_capacity_pct = [math]::Round($corePercent / [Environment]::ProcessorCount,2)
                working_set_MiB = [math]::Round($app.WorkingSet64 / 1048576,2)
                private_MiB = [math]::Round($app.PrivateMemorySize64 / 1048576,2)
                handles = $app.HandleCount
                gdi_objects = if ($gdiCount -gt 0) { $gdiCount }
                user_objects = if ($userCount -gt 0) { $userCount }
            } | Export-Csv -Path $csv -Append -NoTypeInformation -Encoding UTF8
            $previousCpu = $cpu
            $previousTime = $now
        }
    }
} finally {
    $app.Dispose()
    if (Test-Path $csv) { Write-Host "Saved samples to $csv" }
}
Write-Host 'Resume QuietSnow from the tray when finished.'
