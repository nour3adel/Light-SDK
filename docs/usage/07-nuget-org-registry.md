# Publishing Light SDK to NuGet.org

This guide explains how to publish Light SDK to the public NuGet.org registry under the LightPxl organization.

## 1. Prerequisites

1. A NuGet.org account with owner/admin role in the LightPxl organization
2. An API key from https://www.nuget.org/account/apikeys
3. The Light.SDK.*.nupkg package built locally or in CI/CD

## 2. Create API Key on NuGet.org

1. Log in to https://www.nuget.org/account/apikeys
2. Click "Create" to generate a new API key
3. Select scope: "Push new packages and package versions"
4. Copy the key (you'll only see it once)

## 3. Publish Light SDK to NuGet.org

### Option A: Command Line

```bash
dotnet nuget push bin/Release/Light.SDK.1.0.4.nupkg --api-key <YOUR_NUGET_API_KEY> --source https://api.nuget.org/v3/index.json
```

Or using the shorthand (nuget.org is default):

```bash
dotnet nuget push bin/Release/Light.SDK.1.0.4.nupkg --api-key <YOUR_NUGET_API_KEY>
```

### Option B: Store API Key Locally

Configure NuGet to store the API key:

```bash
dotnet nuget update source nuget.org --username __USERNAME__ --password <YOUR_NUGET_API_KEY> --store-password-in-clear-text
```

Then push without specifying the key:

```bash
dotnet nuget push bin/Release/Light.SDK.1.0.4.nupkg
```

## 4. Verify Publication

After 5-10 minutes, verify the package appears at:

- https://www.nuget.org/packages/Light.SDK
- https://www.nuget.org/profiles/LightPxl

## 5. Install from NuGet.org

Users can install Light SDK directly:

```bash
dotnet add package Light.SDK
```

Or specify a version:

```bash
dotnet add package Light.SDK --version 1.0.4
```

## 6. Publish with GitHub Actions

Store your NuGet API key as a GitHub secret (e.g., `NUGET_API_KEY`):

```yaml
- name: Build and Pack
  run: dotnet pack Light.SDK.csproj -c Release

- name: Push to NuGet.org
  run: dotnet nuget push bin/Release/Light.SDK.*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## Notes

- Once published to NuGet.org, the package is immutable (cannot be overwritten)
- To release a new version, increment the version number in Light.SDK.csproj
- NuGet.org takes ~5-10 minutes to index and make packages searchable
- Unlisting (hiding) a version is possible but should be used sparingly
