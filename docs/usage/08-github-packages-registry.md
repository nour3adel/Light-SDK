# Working with the GitHub Packages NuGet Registry

This guide explains how to publish and consume Light SDK using GitHub Packages (NuGet registry on GitHub).

## 1. Registry URL

For this repository, the feed URL is:

```text
https://nuget.pkg.github.com/LightPxl/index.json
```

General format:

```text
https://nuget.pkg.github.com/<NAMESPACE>/index.json
```

## 2. Authentication

GitHub Packages NuGet registry supports:

1. Personal access token (classic) for local development and CLI.
2. GITHUB_TOKEN inside GitHub Actions workflows.

## 3. Authenticate Locally with PAT (classic)

Create a token with at least:

1. read:packages for install/restore
2. write:packages for publish
3. delete:packages only if you need delete permissions

Add GitHub Packages as a source:

```bash
dotnet nuget add source --username LightPxl --password <YOUR_GITHUB_PAT_CLASSIC> --store-password-in-clear-text --name github "https://nuget.pkg.github.com/LightPxl/index.json"
```

## 4. Authenticate via nuget.config (alternative)

Create a nuget.config file:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/LightPxl/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="LightPxl" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT_CLASSIC" />
    </github>
  </packageSourceCredentials>
</configuration>
```

## 5. Publish Light SDK to GitHub Packages

1. Build and pack:

```bash
dotnet pack Light.SDK.csproj -c Release
```

2. Push package to GitHub Packages:

```bash
dotnet nuget push bin/Release/Light.SDK.1.0.4.nupkg --source "github" --api-key <YOUR_GITHUB_PAT_CLASSIC>
```

Notes:

1. The nupkg should remain below registry size limits.
2. First publish is private by default in GitHub Packages unless visibility is changed.

## 6. Publish with GitHub Actions (recommended)

Use GITHUB_TOKEN in workflow instead of hardcoded PAT when possible.

Example step:

```yaml
- name: Add GitHub Packages source
  run: dotnet nuget add source --username LightPxl --password ${{ secrets.GITHUB_TOKEN }} --store-password-in-clear-text --name github "https://nuget.pkg.github.com/LightPxl/index.json"

- name: Pack
  run: dotnet pack Light.SDK.csproj -c Release

- name: Push
  run: dotnet nuget push bin/Release/*.nupkg --source github --api-key ${{ secrets.GITHUB_TOKEN }} --skip-duplicate
```

## 7. Install from GitHub Packages

1. Add source (PAT or configured nuget.config).
2. Restore/install:

```bash
dotnet add package LightPxl --version 1.0.4 --source "github"
```

Or restore with both sources configured:

```bash
dotnet restore
```

## 8. Avoid 403 Restore Issues with Source Mapping

When using both nuget.org and GitHub Packages, use package source mapping:

```xml
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/LightPxl/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="github">
      <package pattern="LightPxl*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

This reduces intermittent authentication failures and helps prevent dependency confusion.

## 9. Repository Linking

If RepositoryUrl is set in Light.SDK.csproj, GitHub can automatically associate the package with that repository.

Current value:

1. https://github.com/LightPxl/LightPxl

## 10. Release Model Assets

GitHub Packages stores the SDK package.
Model and template bundles should stay in GitHub Releases assets:

1. lightpxl-models-v<version>.zip
2. lightpxl-templates-v<version>.zip

This keeps package install lightweight while still giving users full runtime assets.
