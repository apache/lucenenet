param (
    [switch] $clean = $false
)

$ErrorActionPreference = "Stop"

# The Java API extractor now lives in its own repository:
#   https://github.com/paulirwin/java-api-extractor
# Its source is no longer vendored here. We download the pre-built shaded
# ("fat") jar from a pinned GitHub Release and run it with `java -jar`.
#
# Requirements:
#   - The GitHub CLI (`gh`) must be installed and authenticated (`gh auth login`).
#     It is used to download the release asset.
#   - A Java 21+ runtime on PATH (the extractor targets Java 21).

$extractorRepo = "paulirwin/java-api-extractor"
$extractorVersion = "v0.1.0"
# The release attaches the shaded fat jar named "<artifact>-<version>-all.jar".
$jarAssetPattern = "*-all.jar"

$dotnetProjectPath = "src/dotnet/Lucene.Net.ApiCheck/Lucene.Net.ApiCheck.csproj"
$configFilePath = "apicheck-config.jsonc"
$jarCachePath = "_artifacts/java-api-extractor/$extractorVersion"
$downloadPath = "_artifacts/lucene-api-extractor/download"
$outputPath = "_artifacts/lucene-api-extractor/output"

if ($clean)
{
    # delete existing output and cached jar
    Remove-Item -Recurse -Force $outputPath -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $jarCachePath -ErrorAction SilentlyContinue
}

# create download, output, and jar cache paths recursively
New-Item -ItemType Directory -Force -Path $downloadPath -InformationAction SilentlyContinue -ErrorAction SilentlyContinue | Out-Null
New-Item -ItemType Directory -Force -Path $outputPath -InformationAction SilentlyContinue -ErrorAction SilentlyContinue | Out-Null
New-Item -ItemType Directory -Force -Path $jarCachePath -InformationAction SilentlyContinue -ErrorAction SilentlyContinue | Out-Null

# Reuse a previously downloaded jar if present (use -clean to force a fresh download).
$jarFile = Get-ChildItem -Path $jarCachePath -Filter $jarAssetPattern -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $jarFile)
{
    # The download requires the GitHub CLI.
    if (-not (Get-Command gh -ErrorAction SilentlyContinue))
    {
        Write-Host "The GitHub CLI ('gh') is required to download the java-api-extractor jar but was not found on PATH."
        Write-Host "Install it from https://cli.github.com/ and run 'gh auth login', then re-run this script."
        exit 1
    }

    Write-Host "Downloading java-api-extractor $extractorVersion fat jar from $extractorRepo..."
    gh release download $extractorVersion --repo $extractorRepo --pattern $jarAssetPattern --dir $jarCachePath --clobber

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Failed to download the java-api-extractor release asset. Exiting."
        exit $LASTEXITCODE
    }

    $jarFile = Get-ChildItem -Path $jarCachePath -Filter $jarAssetPattern -ErrorAction SilentlyContinue | Select-Object -First 1

    if (-not $jarFile)
    {
        Write-Host "Download succeeded but no jar matching '$jarAssetPattern' was found in $jarCachePath. Exiting."
        exit 1
    }
}

Write-Host "Jar file: $($jarFile.FullName)"

if ($clean) {
    # Clean the API Check project
    Write-Host "Cleaning API Check project..."
    dotnet clean $dotnetProjectPath
}

# Run the API check
Write-Host "Running API check..."
dotnet run --project $dotnetProjectPath -- report -j $jarFile.FullName -c $configFilePath -o $outputPath -d $downloadPath
