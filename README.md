<!--

 Licensed to the Apache Software Foundation (ASF) under one
 or more contributor license agreements.  See the NOTICE file
 distributed with this work for additional information
 regarding copyright ownership.  The ASF licenses this file
 to you under the Apache License, Version 2.0 (the
 "License"); you may not use this file except in compliance
 with the License.  You may obtain a copy of the License at

   http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing,
 software distributed under the License is distributed on an
 "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 KIND, either express or implied.  See the License for the
 specific language governing permissions and limitations
 under the License.

-->

# Apache Lucene.NET Dev Analyzers

This repo contains custom [Roslyn analyzers](https://learn.microsoft.com/en-us/visualstudio/code-quality/roslyn-analyzers-overview?view=vs-2022) that are used by the [Apache Lucene.NET](https://lucenenet.apache.org/) project to enforce code quality and consistency, as well as provide automated code fixes.

These analyzers are intended for use by contributors to the Lucene.NET project only.
They are not intended for public use outside of this project.
Therefore, any releases or NuGet packages produced from this repository are not official Apache Lucene.NET project artifacts, and are not subject to the same quality control or testing as the official Lucene.NET releases.
They are also not subject to the release policy or voting process, as these are not intended
to be used "beyond the group that owns it."

## Building

To build the analyzers, you will need to have the [.NET 8 SDK](https://dotnet.microsoft.com/download) installed.

To build from the repo root, run the following command:

```bash
dotnet build
```

To run the tests, you can use the following command:

```bash
dotnet test
```

## IDE Support and Debugging

These analyzers have been tested with Visual Studio 2022 and JetBrains Rider. They should work with any IDE that supports Roslyn analyzers, but your mileage may vary. Importantly, they also work with MSBuild, so they can be used in our CI pipelines, or to help validate your changes when you build before submitting a pull request.

A `Lucene.Net.CodeAnalysis.Dev.Sample` project has been provided to demonstrate and debug the analyzers and code fixes in the IDE during development of them. After building, you should notice the analyzers producing the expected warnings in the Sample project. You can also debug the analyzers by setting a breakpoint in your analyzer and launching the `DebugRoslynAnalyzers` target of the `Lucene.Net.CodeAnalysis.Dev` project. You can also debug them by debugging the unit tests.

## Contributing

Please read and follow the [Apache Lucene.NET Contributor's Guide](https://github.com/apache/lucenenet/blob/master/CONTRIBUTING.md) first before proceeding further.

### Reserving a Diagnostic ID

Before creating any analyzers, you'll need a reserved diagnostic ID for your analyzer(s).
To avoid multiple contributors attempting to use the same ID at the same time, we have created a simple process to follow.
It is important that you follow this process to avoid rework of your PR.

1. Make sure there is an issue on the main [Apache Lucene.NET repo](https://github.com/apache/lucenenet/issues) for the analyzer(s) needed, that has been approved by the Lucene.NET team as indicated by having the `approved-rule` label.
2. Reserve your diagnostic ID(s) _before_ implementing the analyzer(s):
    - If you are a Lucene.NET committer, you can reserve one yourself. Modify the [DiagnosticCategoryAndIdRanges.txt](DiagnosticCategoryAndIdRanges.txt) file (following the instructions in that file) to reserve your ID(s). Commit and push the change to this file directly to the `main` branch, as the only file in the commit. DO NOT include any other code or changes in this commit. In the event of a conflict, do not merge this file; discard your changes, pull latest, and try again. Include the issue number in your commit message.
    - If you are not a Lucene.NET committer, request in the discussion for the GitHub issue that a committer do the steps above for you for your desired number of diagnostic IDs. Please make sure to mention which category the ID(s) should belong to.
3. Once you have the reserved ID(s), you can proceed with implementing your analyzer and submitting a pull request. Make sure to include your analyzer in the [AnalyzerReleases.Unshipped.md](src/Lucene.Net.CodeAnalysis.Dev/AnalyzerReleases.Unshipped.md) file.

### Requirements

Before submitting a pull request to this repo, ensure that each analyzer you're adding:
1. Has a reserved ID (see above)
2. Has an entry in the [AnalyzerReleases.Unshipped.md](src/Lucene.Net.CodeAnalysis.Dev/AnalyzerReleases.Unshipped.md) file, reflowing the table if needed
3. Matches existing analyzer naming conventions and code styles
4. Has a title, description, and message format resource in the [Resources.resx](src/Lucene.Net.CodeAnalysis.Dev/Resources.resx) file (currently English only)
5. Has a working, sample violation in the `Lucene.Net.CodeAnalysis.Dev.Sample` project
6. Has good unit test coverage in the `Lucene.Net.CodeAnalysis.Dev.Tests` project, using existing styles and practices per the other unit tests there 

