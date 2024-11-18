$mvnPomPath = "src/java/lucene-api-extractor/pom.xml"
$targetPath = "src/java/lucene-api-extractor/target"
$dotnetProjectPath = "src/dotnet/Lucene.Net.ApiCheck/Lucene.Net.ApiCheck.csproj"
$configFilePath = "apicheck-config.json"
$artifactId = "lucene-api-extractor"
$downloadPath = "_artifacts/lucene-api-extractor/download"
$outputPath = "_artifacts/lucene-api-extractor/output"

# delete existing output path
Remove-Item -Recurse -Force $outputPath

# create download and output paths recursively
New-Item -ItemType Directory -Force -Path $downloadPath
New-Item -ItemType Directory -Force -Path $outputPath

# build jar
Write-Host "Building jar file..."
mvn -f $mvnPomPath clean package

$jarFile = Get-ChildItem -Path $targetPath -Filter "$artifactId-*.jar" | Select-Object -First 1
Write-Host "Jar file: $jarFile"

# Run the API check
Write-Host "Running API check..."
dotnet run --project $dotnetProjectPath -- -j $jarFile -c $configFilePath diff -o $outputPath
