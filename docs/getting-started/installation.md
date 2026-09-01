# Installation

Aletheia is a .NET 8 solution with an optional Python documentation environment.

## Install the Windows App

For non-developer users, download the latest Windows package from
[GitHub Releases](https://github.com/EidoAut/Aletheia/releases/latest), unzip it, and run
`Aletheia.Desktop.exe`.

The desktop package is self-contained for `win-x64`, so it does not require installing the .NET SDK.
Windows may show a SmartScreen prompt because the binary is not code-signed yet.

## Prerequisites

- Windows is the primary target for the native WinForms desktop application.
- .NET SDK 8.0.100 or later is required.
- Python 3.12 is recommended for building this Wiki locally.

Check the .NET SDK:

```powershell
dotnet --list-sdks
dotnet --version
```

## Restore, Build, and Test

```powershell
dotnet restore Aletheia.sln
dotnet build Aletheia.sln
dotnet test Aletheia.sln
```

The repository also provides a build script used by CI:

```powershell
./scripts/build.ps1
```

## Run Aletheia

=== "Sample CLI"
    ```powershell
    dotnet run --project src/Aletheia.Cli -- sample
    ```

=== "Desktop"
    ```powershell
    dotnet run --project src/Aletheia.Desktop
    ```

## Build the Wiki Locally

```powershell
python -m venv .venv-docs
.venv-docs\Scripts\Activate.ps1
pip install -r requirements-docs.txt
mkdocs serve
```

Build the static site in strict mode:

```powershell
mkdocs build --strict
```

The generated `site/` directory is a self-contained static website. It can be served from any basic
HTTP server. Opening complex generated pages directly with `file://` can be less reliable than
using `mkdocs serve` or a small local HTTP server because browser security rules differ for local
files.
