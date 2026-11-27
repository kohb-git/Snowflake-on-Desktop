# Snowflake on Desktop (QuietSnow)

A Windows desktop snowfall effect with animated lights on window frames.

| Path | Contents |
| --- | --- |
| src/ | Editable C# reconstructed from the 1.0.4 screen-test executable, including the application icon and embedded resource. |
| prototype/ | Reference 1.0.4 test executable and known issues. |
| archive/1.0.0/ | Four unmodified files from the original portable distribution, including its historical release report. |
| tools/ | PowerShell script for collecting Windows process measurements. |

## Build on Windows

With a modern .NET SDK installed, run `dotnet build src/QuietSnow.csproj -c Release` at the repository root. NuGet restores .NET Framework 4.0 reference assemblies. The expected output is `src/bin/Release/net40/QuietSnow.exe`. Exit existing QuietSnow instances before testing.

## Recovery and verification

The 1.0.0 README mentions build.ps1, package-release.ps1, automated tests and a Windows smoke test. Those files were excluded from the portable distribution. Its measurements apply only to the old release.

The current editable source was decompiled from the 1.0.4 screen-test binary with ILSpy. The foreground-window refresh helper was restored from its IL, and compiler-generated names were cleaned up as needed to compile. Original comments, local variable names, test sources and build scripts are unavailable.

The recovered C# files compiled together into a Windows GUI executable using Roslyn 3.11, C# 7.3, .NET Framework 4.0 reference assemblies, the recovered icon and embedded resource. Windows runtime equivalence, a full SDK project build and multi-monitor behavior still need verification. Use the reference executable and prototype/README.md for comparisons.
