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
