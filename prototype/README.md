# Snowflake on Desktop

A Windows desktop snowfall prototype with snow accumulation and animated lights on window frames. The current executable still uses the QuietSnow name.

## Try the prototype

Exit any running copy using the snowflake icon in the system tray, then start `prototype/QuietSnow-1.0.4-screen-test.exe`. Right-click the tray icon for settings or double-click to pause. This snapshot contains the compiled test build and its measurement script. The original C# project is still needed for a reproducible build.

## Test on Windows

1. Start with an empty desktop and check that snow appears immediately on each monitor.
2. Drag an ordinary window across the monitor boundary. Watch the ends of the light cord for drift. Repeat with different display scales and with one monitor offset vertically.
3. From PowerShell in a writable directory, run `./tools/Measure-QuietSnow-v2.ps1` (adjust the path if necessary). The CSV compares an empty desktop, a stationary window, dragging that window, a second stationary baseline, and pause. Record display layout, scaling and build revision with the CSV. CPU samples alone cannot establish frame latency.

## Known issues

- A transparent top-level window can mask snowfall while screen status reports that effects are allowed.
- Failed installation of the window movement hook can cause a full window scan every frame.
- An escaped snowball chooses a return monitor using only its horizontal coordinate.
- Light cord tracking still needs a visual test under load on Windows.

## Working with Git

Start each change from the latest default branch and use a short branch name such as `fix/window-tracking`. Include reproduction steps, monitor layout and test results in the pull request. Keep machine-specific measurements local unless they are relevant to the change. Once the C# source is recovered, commit the project files and publish subsequent tested executables as versioned GitHub Release assets.
