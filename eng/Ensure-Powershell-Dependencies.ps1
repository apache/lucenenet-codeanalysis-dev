param(
    [string] $PesterVersion = "5.5.0"
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Ensure NuGet provider exists
if (-not (Get-PackageProvider -Name NuGet -ErrorAction SilentlyContinue)) {
    Install-PackageProvider -Name NuGet -Force -Scope CurrentUser | Out-Null
}

# Ensure PSGallery is registered
$repo = Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue
if (-not $repo) {
    Register-PSRepository -Name PSGallery -SourceLocation "https://www.powershellgallery.com/api/v2" -InstallationPolicy Untrusted
    $repo = Get-PSRepository -Name PSGallery
}

# Track original InstallationPolicy
$originalPolicy = $repo.InstallationPolicy
$restorePolicy = $false

try {
    if ($originalPolicy -ne 'Trusted') {
        # Temporarily trust PSGallery
        Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
        $restorePolicy = $true
    }

    # Check if correct Pester version is installed
    $module = Get-Module -ListAvailable -Name Pester | Sort-Object Version -Descending | Select-Object -First 1
    if (-not $module -or $module.Version -ne [version]$PesterVersion) {
        Install-Module Pester -Scope CurrentUser -Force -SkipPublisherCheck -RequiredVersion $PesterVersion
    }
}
finally {
    if ($restorePolicy) {
        # Restore original policy
        Set-PSRepository -Name PSGallery -InstallationPolicy $originalPolicy
    }
}
