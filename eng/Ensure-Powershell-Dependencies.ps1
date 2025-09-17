# Licensed to the Apache Software Foundation (ASF) under one
# or more contributor license agreements.  See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership.  The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License.  You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing,
# software distributed under the License is distributed on an
# "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
# KIND, either express or implied.  See the License for the
# specific language governing permissions and limitations
# under the License.

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
