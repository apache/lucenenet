param (
    [switch] $clean = $false
)

$ErrorActionPreference = "Stop"

$mvnPomPath = "src/java/lucene-api-extractor/pom.xml"
$targetPath = "src/java/lucene-api-extractor/target"
$dotnetProjectPath = "src/dotnet/Lucene.Net.ApiCheck/Lucene.Net.ApiCheck.csproj"
$configFilePath = "apicheck-config.json5"
$artifactId = "lucene-api-extractor"
$downloadPath = "_artifacts/lucene-api-extractor/download"
$outputPath = "_artifacts/lucene-api-extractor/output"

if ($clean)
{
    # delete existing output path
    Remove-Item -Recurse -Force $outputPath -ErrorAction SilentlyContinue
}

# create download and output paths recursively
New-Item -ItemType Directory -Force -Path $downloadPath -InformationAction SilentlyContinue -ErrorAction SilentlyContinue | Out-Null
New-Item -ItemType Directory -Force -Path $outputPath -InformationAction SilentlyContinue -ErrorAction SilentlyContinue | Out-Null

if ($clean)
{
    # build jar
    Write-Host "Building jar file..."
    mvn -f $mvnPomPath clean package

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Maven build failed. Exiting."
        exit $LASTEXITCODE
    }
}
elseif (-not (Test-Path "$targetPath/$artifactId-*.jar"))
{
    # build jar
    Write-Host "Building jar file..."
    mvn -f $mvnPomPath package

    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Maven build failed. Exiting."
        exit $LASTEXITCODE
    }
}


$jarFile = Get-ChildItem -Path $targetPath -Filter "$artifactId-*.jar" | Select-Object -First 1
Write-Host "Jar file: $jarFile"

if ($clean) {
    # Clean the API Check project
    Write-Host "Cleaning API Check project..."
    dotnet clean $dotnetProjectPath
}

# Run the API check
Write-Host "Running API check..."
dotnet run --project $dotnetProjectPath -- report -j $jarFile -c $configFilePath -o $outputPath -d $downloadPath
