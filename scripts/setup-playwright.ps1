param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repositoryRoot "tests/CommandSynergy.PlaywrightTests/CommandSynergy.PlaywrightTests.csproj"

dotnet build $projectPath --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$playwrightScript = Join-Path $repositoryRoot "tests/CommandSynergy.PlaywrightTests/bin/$Configuration/net10.0/playwright.ps1"
if (-not (Test-Path $playwrightScript)) {
    throw "Playwright install script was not generated at '$playwrightScript'."
}

& $playwrightScript install chromium
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
