BeforeAll {
    . $PSCommandPath.Replace('.Tests.ps1','.ps1')
}

Describe "Format-Test-Results" {
    $testCases = @(
        @{ SuiteName="Alpha"; Passed=5; Failed=0; Ignored=16; Crashed=$false; Expected="✅ Passed" },
        @{ SuiteName="Beta";  Passed=1; Failed=2; Ignored=0; Crashed=$false; Expected="❌ Failed" },
        @{ SuiteName="Gamma"; Passed=0; Failed=0; Ignored=1; Crashed=$true;  Expected="⚠️ Crashed" }
    )

    It "produces expected status lines" -ForEach $testCases {
        $obj = [PSCustomObject]@{
            SuiteName    = $_.SuiteName
            PassedCount  = $_.Passed
            FailedCount  = $_.Failed
            IgnoredCount = $_.Ignored
            Crashed      = $_.Crashed
        }

        $output = Format-Test-Results $obj
        Write-Host $output -ForegroundColor Green
        $output | Should -Match $_.Expected
        $output | Should -Match "\*\*$($_.SuiteName)\*\*"
        $output | Should -Match "Passed=$($_.PassedCount)"
        $output | Should -Match "Failed=$($_.FailedCount)"
        $output | Should -Match "Ignored=$($_.IgnoredCount)"
        "" | Should -Be ""
    }

    Context "respects custom status text/icons" {
        It "respects Crashed" {
            $obj = [PSCustomObject]@{
                SuiteName="Delta"; PassedCount=0; FailedCount=0; IgnoredCount=0; Crashed=$true
            }

            $output = Format-Test-Results $obj `
                -IconCrashed 'XX' -TextCrashed 'Boom'
            $output | Should -Match "XX Boom"
        }
        
        It "respects Passed" {
            $obj = [PSCustomObject]@{
                SuiteName="Delta"; PassedCount=30; FailedCount=0; IgnoredCount=0; Crashed=$false
            }

            $output = Format-Test-Results $obj `
                -IconPassed 'YY' -TextPassed 'MePassed'
            $output | Should -Match "YY MePassed"
        }
        
        It "respects Failed" {
            $obj = [PSCustomObject]@{
                SuiteName="Delta"; PassedCount=30; FailedCount=2; IgnoredCount=0; Crashed=$false
            }

            $output = Format-Test-Results $obj `
                -IconFailed 'ZZ' -TextFailed 'MeFailed'
            $output | Should -Match "ZZ MeFailed"
        }
    }
}
