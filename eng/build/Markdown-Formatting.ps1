<#
.SYNOPSIS
    Formats test results into a Markdown summary with icons and status text.

.DESCRIPTION
    The Format-Test-Results function takes one or more test result objects
    (typically PSCustomObjects with fields such as SuiteName, PassedCount,
    FailedCount, IgnoredCount, and Crashed) and produces a Markdown string
    summarizing the results. Each test suite is displayed with an icon,
    status text, and counts of passed, failed, and ignored tests.

    By default, the output includes a "## Test Results" heading and a bullet
    point for each suite.

.PARAMETER Results
    One or more PSCustomObjects representing test results. Each object should
    include the following properties:
      - SuiteName    [string]
      - PassedCount  [int]
      - FailedCount  [int]
      - IgnoredCount [int]
      - Crashed      [bool]

    This parameter is mandatory and accepts input from the pipeline.

.PARAMETER IconPassed
    The icon to display for suites where all tests passed (default: ✅).

.PARAMETER TextPassed
    The label to display for passing suites (default: "Passed").

.PARAMETER IconFailed
    The icon to display for suites with at least one failed test (default: ❌).

.PARAMETER TextFailed
    The label to display for failing suites (default: "Failed").

.PARAMETER IconCrashed
    The icon to display for suites that crashed (default: ⚠️).

.PARAMETER TextCrashed
    The label to display for crashed suites (default: "Crashed").

.EXAMPLE
    $results = @(
        [pscustomobject]@{ SuiteName = "UnitTests"; PassedCount=10; FailedCount=0; IgnoredCount=0; Crashed=$false },
        [pscustomobject]@{ SuiteName = "IntegrationTests"; PassedCount=8; FailedCount=2; IgnoredCount=1; Crashed=$false },
        [pscustomobject]@{ SuiteName = "UITests"; PassedCount=0; FailedCount=0; IgnoredCount=0; Crashed=$true }
    )

    $results | Format-Test-Results

    Produces output similar to:

    ## Test Results

    - ✅ Passed - **UnitTests** | Passed=10, Failed=0, Ignored=0
    - ❌ Failed - **IntegrationTests** | Passed=8, Failed=2, Ignored=1
    - ⚠️ Crashed - **UITests** | Passed=0, Failed=0, Ignored=0

.EXAMPLE
    $results | Format-Test-Results -IconFailed "💥" -TextFailed "Broken"

    Overrides the failed suite indicator with a custom icon and text.

.OUTPUTS
    System.String
    Returns a Markdown-formatted string suitable for console output,
    saving to a file, or inclusion in CI/CD summaries (e.g., GitHub Actions).

.NOTES
    The Markdown output is designed for human-readable summaries,
    not for machine parsing.
#>

function Format-Test-Results {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline, Position=0)]
        [ValidateNotNullOrEmpty()]
        [PSCustomObject[]] $Results,

        # Icons/texts as parameters (with defaults)
        [string] $IconPassed  = '✅',
        [string] $TextPassed  = 'Passed',

        [string] $IconFailed  = '❌',
        [string] $TextFailed  = 'Failed',

        [string] $IconCrashed = '⚠️',
        [string] $TextCrashed = 'Crashed'
    )

    begin {
        $sb = [System.Text.StringBuilder]::new()
        [void]$sb.AppendLine("## Test Results`n")
    }

    process {
        foreach ($r in $Results) {
            if ($r.Crashed) {
                $statusIcon = $IconCrashed
                $statusText = $TextCrashed
            }
            elseif ($r.FailedCount -gt 0) {
                $statusIcon = $IconFailed
                $statusText = $TextFailed
            }
            else {
                $statusIcon = $IconPassed
                $statusText = $TextPassed
            }

            $line = "- $statusIcon $statusText - **$($r.SuiteName)** " +
                    "| Passed=$($r.PassedCount), Failed=$($r.FailedCount), Ignored=$($r.IgnoredCount)"

            [void]$sb.AppendLine($line)
        }
    }

    end {
        return $sb.ToString()
    }
}
