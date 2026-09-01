# Build and Test

## Full Repository Build

```powershell
dotnet restore Aletheia.sln -p:EnableWindowsTargeting=true
dotnet build Aletheia.sln --configuration Release --no-restore -p:EnableWindowsTargeting=true
dotnet test Aletheia.sln --configuration Release --no-build --no-restore -p:EnableWindowsTargeting=true
```

The repository script wraps the same flow:

```powershell
./scripts/build.ps1 -Configuration Release
```

## Run a Specific Test Project

```powershell
dotnet test tests/Aletheia.Validation.Tests/Aletheia.Validation.Tests.csproj
dotnet test tests/Aletheia.Application.Tests/Aletheia.Application.Tests.csproj
```

## Run the Desktop

```powershell
dotnet run --project src/Aletheia.Desktop
```

## Publish the Desktop Package

```powershell
./scripts/publish-desktop.ps1 -Configuration Release -Runtime win-x64
./scripts/publish-desktop.ps1 -Configuration Release -Runtime win-x64 -SelfContained
```

The publish script writes `artifacts/Aletheia.Desktop-win-x64.zip` and verifies that the executable
exists before compressing the package.

## Build the Documentation

```powershell
python -m venv .venv-docs
.venv-docs\Scripts\Activate.ps1
pip install -r requirements-docs.txt
mkdocs build --strict
```
