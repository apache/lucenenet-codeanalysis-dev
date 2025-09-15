$global:TempTestDir = $null

Describe "Parse-Test-Results" {
    BeforeAll {
        # Create a single temp directory for all tests
        $global:TempTestDir = Join-Path ([IO.Path]::GetTempPath()) ([Guid]::NewGuid())
        New-Item -ItemType Directory -Path $global:TempTestDir | Out-Null

        # Helper function to invoke the script under test
        function Parse-Test-Results {
            param(
                [Parameter(Position = 0)]
                [string]$Path
            )
            & $PSCommandPath.Replace('.Tests.ps1','.ps1') $Path
        }

        # Helper function to create a TRX file in the temp folder
        function New-TrxFile {
            param(
                [string]$Content,
                [string]$FileName = "$(New-Guid).trx"
            )
        
            $filePath = Join-Path $global:TempTestDir $FileName
            $Content | Set-Content -Path $filePath -Encoding UTF8
            return $filePath
        }
    }

    It "parses a passed run" {
        # Arrange
        $trxContent = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun>
  <ResultSummary outcome="Completed">
    <Counters total="3" executed="3" passed="3" failed="0" />
  </ResultSummary>
</TestRun>
"@
        $trxPath = New-TrxFile -Content $trxContent -FileName 'passed.trx'

        # Act
        $result = Parse-Test-Results -Path $trxPath

        # Assert
        $result.PassedCount     | Should -Be 3
        $result.FailedCount     | Should -Be 0
        $result.IgnoredCount    | Should -Be 0
        $result.Crashed         | Should -Be $false
    }

    It "parses a failed run" {
        # Arrange
        $trxContent = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun>
  <ResultSummary outcome="Failed">
    <Counters total="3" executed="3" passed="2" failed="1" />
  </ResultSummary>
</TestRun>
"@
        $trxPath = New-TrxFile -Content $trxContent -FileName 'failed.trx'

        # Act
        $result = Parse-Test-Results -Path $trxPath

        # Assert
        $result.PassedCount     | Should -Be 2
        $result.FailedCount     | Should -Be 1
        $result.IgnoredCount    | Should -Be 0
        $result.Crashed         | Should -Be $false
    }

    It "calculates ignored test count" {
        # Arrange
        $trxContent = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun>
  <ResultSummary outcome="Failed">
    <Counters total="7" executed="3" passed="2" failed="1" />
  </ResultSummary>
</TestRun>
"@
        $trxPath = New-TrxFile -Content $trxContent -FileName 'failed.trx'

        # Act
        $result = Parse-Test-Results -Path $trxPath

        # Assert
        $result.PassedCount     | Should -Be 2
        $result.FailedCount     | Should -Be 1
        $result.IgnoredCount    | Should -Be 4
        $result.Crashed         | Should -Be $false
    }

    Context "detects a crash" {
        It "could not find dotnet" {
            # Arrange
            $trxContent = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun>
  <ResultSummary outcome="Failed">
    <Counters total="0" executed="0" passed="0" failed="0" />
  </ResultSummary>
  <RunInfos>
    <RunInfo computerName="localhost" outcome="Error">
      <Text>Could not find 'dotnet.exe' host</Text>
    </RunInfo>
  </RunInfos>
</TestRun>
"@
            $trxPath = New-TrxFile -Content $trxContent -FileName 'crashed-could-not-find-dotnet.trx'

            # Act
            $result = Parse-Test-Results -Path $trxPath

            # Assert
            $result.PassedCount     | Should -Be 0
            $result.FailedCount     | Should -Be 0
            $result.IgnoredCount    | Should -Be 0
            $result.Crashed         | Should -Be $true
        }

        It "could not load assembly" {
            # Arrange
            $trxContent = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun>
  <ResultSummary outcome="Failed">
    <Counters total="0" executed="0" passed="0" failed="0" />
  </ResultSummary>
  <RunInfos>
    <RunInfo computerName="localhost" outcome="Error">
      <Text>Could not load file or assembly 'foo.dll'</Text>
    </RunInfo>
  </RunInfos>
</TestRun>
"@
            $trxPath = New-TrxFile -Content $trxContent -FileName 'crashed-could-not-load-assembly.trx'

            # Act
            $result = Parse-Test-Results -Path $trxPath

            # Assert
            $result.PassedCount     | Should -Be 0
            $result.FailedCount     | Should -Be 0
            $result.IgnoredCount    | Should -Be 0
            $result.Crashed         | Should -Be $true
        }

        It "exited with error" {
            # Arrange
            $trxContent = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun>
  <ResultSummary outcome="Failed">
    <Counters total="0" executed="0" passed="0" failed="0" />
  </ResultSummary>
  <RunInfos>
    <RunInfo computerName="localhost" outcome="Error">
      <Text>The program exited with error 1234.</Text>
    </RunInfo>
  </RunInfos>
</TestRun>
"@
            $trxPath = New-TrxFile -Content $trxContent -FileName 'crashed-exited-with-error.trx'

            # Act
            $result = Parse-Test-Results -Path $trxPath

            # Assert
            $result.PassedCount     | Should -Be 0
            $result.FailedCount     | Should -Be 0
            $result.IgnoredCount    | Should -Be 0
            $result.Crashed         | Should -Be $true
        }

        It "no test is available" {
            # Arrange
            $trxContent = @"
<?xml version="1.0" encoding="utf-8"?>
<TestRun>
  <ResultSummary outcome="Failed">
    <Counters total="0" executed="0" passed="0" failed="0" />
  </ResultSummary>
  <RunInfos>
    <RunInfo computerName="localhost" outcome="Error">
      <Text>No test is available in the assembly</Text>
    </RunInfo>
  </RunInfos>
</TestRun>
"@
            $trxPath = New-TrxFile -Content $trxContent -FileName 'crashed-no-test-is-available.trx'

            # Act
            $result = Parse-Test-Results -Path $trxPath

            # Assert
            $result.PassedCount     | Should -Be 0
            $result.FailedCount     | Should -Be 0
            $result.IgnoredCount    | Should -Be 0
            $result.Crashed         | Should -Be $true
        }
    }

    AfterAll {
        # Clean up temp directory
        if ($global:TempTestDir -and (Test-Path $global:TempTestDir)) {
            Remove-Item -Path $global:TempTestDir -Recurse -Force
        }

        # Perform cleanup based on the variable's value
        $global:TempTestDir = $null # Reset for subsequent runs if needed
    }
}
