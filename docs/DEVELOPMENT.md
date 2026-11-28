# Development

Build from the repository root on Windows with a modern .NET SDK.

```powershell
dotnet build src/snowlit.csproj -c Release
dotnet run --project tests/RoadTests.csproj -c Release
```

The project targets .NET Framework 4.0. NuGet restores the reference assemblies. The executable is written to `src/bin/Release/net40/snowlit.exe`. Exit running copies before testing. The road tests exercise the settings window and vehicle cycle. For motion issues, record the display layout and scaling, then drag a window across the monitor boundary. CPU samples alone cannot establish frame latency.

## Source history

The editable C# started as a reconstruction of the QuietSnow 1.0.4 screen-test binary using ILSpy. Original comments, local names, tests and build scripts were unavailable. The [prototype](../prototype/README.md) and [1.0.0 archive](../archive/1.0.0/README.md) are historical references.

## Publishing

Use the same version in the assembly metadata, tray UI, EXE filename and release directory. Build and test the executable. Write Russian and English release notes in `docs/releases/VERSION.md`, link the entry in the changelog, and add the EXE and SHA256SUMS to `releases/VERSION/`. Set `releases/current.txt` to that version in the same commit. The release workflow checks the digest and both language headings, then uses that file as the release body.
