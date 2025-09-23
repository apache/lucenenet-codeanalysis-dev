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

# Making a Release

> [!NOTE]
> All commands should be executed from the root of the repository unless otherwise stated.

## Prerequisites

- [PowerShell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell) 6.0 or higher (see [this question](http://stackoverflow.com/questions/1825585/determine-installed-powershell-version) to check your PowerShell version)
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [nbgv tool](https://www.nuget.org/packages/nbgv/) (the version must match the one defined in [Directory.Packages.props](../Directory.Packages.props))
- [Java 8](https://adoptium.net/temurin/releases) or higher (either a JRE or JDK)
- Bash (Installed automatically with [Git for Windows](https://gitforwindows.org/) on Windows).

### Installing the NBGV Tool

Perform a one-time install of the nbgv tool using the following dotnet CLI command:

> [!NOTE]
> The version should match the one used in [Directory.Packages.props](../Directory.Packages.props).

```console
dotnet tool install -g nbgv --version <theActualVersion>
```

### Configure the Git Commit Hooks

To synchronize the `AnalyzerReleases.Shipped.md` release version with the latest commit, there is a Git commit hook that ensures that the version in the HEAD commit is the same version that is committed to the file.

Check whether the Git `core.hooksPath` is correctly set:

```console
git config core.hooksPath
```

If the command outputs a path, confirm that the path is `./eng/git-hooks`. In all other cases, run the following command to set it appropriately.

```console
git config core.hooksPath ./eng/git-hooks
```

Repeat the first command to confirm that it is set.

---------------------------------------------

## Prior to Release

This project uses Nerdbank.GitVersioning to assist with creating version numbers based on the current branch and commit. This tool handles making pre-release and production releases on release branches.

### Release Workflow Overview

![Release Workflow](images/release-workflow.svg)

### Prepare the Main Branch

1. Ensure all of the features that will be included have been merged to the `main` branch.
2. Check whether the `AnalyzerReleases.Unshipped.md` and `AnalyzerReleases.Shipped.md` are set up consistently and align with the features that have been merged since the prior release. Do not yet move any rules from `AnalyzerReleases.Unshipped.md` to `AnalyzerReleases.Shipped.md`. That task will be performed in a later step.
3. Check whether the `README`, `LICENSE`, `NOTICE` and other documentation files are up to date.

If any changes are required, it is recommended to use feature branch(es) and pull request(s) to update the `main` branch as appropriate before creating a release branch.

### Decide on a Release Version

The version that will be released next is controlled by the `version.json` file. We must choose the release version and commit it to the `main` branch prior to creating a release branch.

> [!NOTE]
> If you are not familiar with these terms, these are covered in the [Semantic Versioning 2.0](https://semver.org/spec/v2.0.0.html) document.

For the purposes of this project:

- **Major (Advanced)** - Released only when a new port of Lucene.NET is started (primarily to show a relationship between Lucene.NET and these analyzers)
- **Minor** - A typical release with one or more new features
- **Patch** - A release that only contains patches to existing features and/or updates to documentation
- **Prerelease** - A release that requires stabilization or is a one-off release for a specific purpose

> [!NOTE]
> This project doesn't have any public API that users consume, so the type of release is strictly informational in nature, not functional.

Now is the time to decide which of these strategies to use for the current version. For the next version (a future release version), we should always assume a patch. This is primarily so we never have to downgrade a version even if a patch is rarely done in practice.

With that in mind, open `version.json` and look at the "version" property, which will determine next version that will be released.

#### Example Version

```json
  "version": "2.0.0-alpha.{height}"
```

The above example shows that the next version that will be released from a release branch is 2.0.0 or 2.0.0-beta.x (where x is an auto-incrementing number). The actual version in the file (alpha) will be used only if the `main` branch is released directly (something that is rare and not covered here).

If we are releasing new features and want the next Minor version (2.1.0), we need to update the `version.json` file to reflect that version.

```json
  "version": "2.1.0-alpha.{height}"
```

Or, if the next version will be a patch, then leave the file unchanged. Commit any changes to the `main` branch and push them upstream before proceeding.

Prereleases should rarely need to change the `version.json` file and will later choose the [Requires Stabilization](#requires-stabilization) option when creating a release branch.

> [!IMPORTANT]
> Release version numbers must always use all 3 version components when specified in `version.json`.

## Create a Release Branch

There are 2 supported scenarios for the release workflow:

1. [Ready to Release](#ready-to-release) - No additional stabilization is required
2. [Requires Stabilization](#requires-stabilization) - A beta will be released, which will be marked as a pre-release to consumers

> [!NOTE]
> In both cases, `main` is advanced to the specified `--nextVersion`. This number should always be a **patch** bump and it should always use all 3 version components (major.minor.patch).
>
> The release branch name is always based on the version being released (e.g., `release/v2.0.0`).

### Ready to Release

When the changes in the main branch are ready to release, create a release branch using the following nbgv tool command as specified in the [documentation](https://dotnet.github.io/Nerdbank.GitVersioning/docs/nbgv-cli.html#preparing-a-release).

For example, assume the `version.json` file on the main branch is currently set up as `2.0.0-alpha.{height}`. We want to go from this version to a release of `2.0.0` and set the next version on the main branch as `2.0.1-alpha.{height}`.

```console
nbgv prepare-release --nextVersion 2.0.1
```

The command should respond with:

```console
release/v2.0.0 branch now tracks v2.0.0 stabilization and release.
main branch now tracks v2.0.1-alpha.{height} development.
```

The tool created a release branch named `release/v2.0.0`. Every build from this branch will be versioned 2.0.0, regardless of how many commits are added.

### Requires Stabilization

When creating a release that may require a few iterations to become stable, it is better to create a beta branch (more about that decision can be found [here](https://dotnet.github.io/Nerdbank.GitVersioning/docs/nbgv-cli.html#preparing-a-release)). Starting from the same point as the [Ready to Release](#ready-to-release) scenario, run the following command.

```console
nbgv prepare-release beta --nextVersion 2.0.1
```

The command should respond with:

```console
release/v2.0.0 branch now tracks v2.0.0-beta.{height} stabilization and release.
main branch now tracks v2.0.1-alpha.{height} development.
```

The tool created a release branch named `release/v2.0.0`. Every commit to this branch will be given a unique pre-release version starting with 2.0.0-beta and ending in a dot followed by one or more digits (i.e. `2.0.0-beta.123`).

### Checkout the Release Branch

After the release branch is created, the rest of the commits will be added to the release branch, so use the git checkout command to switch to that branch.

```console
git checkout <release-branch>
```

---------------------------------------------

## Run the Apache Release Audit Tool

> [!IMPORTANT]
> This command depends on Powershell and Java.

The Release Audit Tool will ensure that all source code files and most other non-generated text files contain a license header.

```console
pwsh ./rat.ps1
```

The tool will apply the updates directly to the local working directory. Review and commit the changes to your local Git clone, adding exclusions to `.rat-excludes` and re-running as necessary.

- Exclude files that already include license headers
- Exclude files that are automatically generated
- Exclude files that cannot contain license headers (such as test data)

> [!NOTE]
> These extra commits will automatically bump the version number from what was specified when [Creating a Release Branch](creating-a-release-branch). It is normal and expected that we may have extra gaps between release version numbers.


## Updating the AnalyzerReleases Files

Roslyn analyzers use two release tracking files to manage analyzer rule metadata:

- **`AnalyzerReleases.Unshipped.md`**
  Tracks analyzer rules that have been added or modified since the last release but are not yet published in a shipped package.

- **`AnalyzerReleases.Shipped.md`**
  Tracks analyzer rules that have been released in one or more shipped packages. This is the authoritative record of rules shipped at specific versions.

Before tagging the release, you must ensure that these files are up to date. This ensures that the release metadata exactly matches the rules shipped in the NuGet package.

> [!NOTE]
> If the release doesn't contain new or changed analyzer rules, this step can be skipped. For example, if the release only contains new code fixes and/or backward compatible patches to existing analyzers.

### Release Version Token

Since Nerdbank.GitVersioning calculates the release version, the `AnalyzerReleases.Shipped.md` file is expected to include a version token when it is committed. A version token must be included in the header of the new section being added to `AnalyzerReleases.Shipped.md`.

#### Release Version Token Example

```markdown
## Release {{vnext}}
```

### Standard Workflow

> [!IMPORTANT]
> This change is expected to be the **final** commit prior to release. If there are any other changes you anticipate that need to be included in the release, they should be committed to the release branch prior to this step.

> [!IMPORTANT]
> This step depends on the NBGV tool, Bash, and the setup of the Git commit hook as described in [Prerequisites](#prerequisites).

1. **Locate pending unshipped rules**  
   Open `AnalyzerReleases.Unshipped.md`. This contains all rules added or modified since the last release.

2. **Move unshipped rules into `AnalyzerReleases.Shipped.md`**  
   - Create a new section in `AnalyzerReleases.Shipped.md` with a heading for the release version, containing the version token.
   - Copy the rules listed under `AnalyzerReleases.Unshipped.md` into this section.
   - Keep the table formatting consistent with previous releases.

3. **Clear `AnalyzerReleases.Unshipped.md`**  
   After the rules are copied over, `AnalyzerReleases.Unshipped.md` should either be empty or contain only rules that are not part of this release.

4. **Commit the changes**  
   Commit the modifications before tagging the release.

### Example: First and Second Releases with Version Token

`AnalyzerReleases.Shipped.md` evolves by appending each release as a new section. Each release is marked with a `## Release <version>` header.

```markdown
## Release 2.0.0-alpha.1

### New Rules

 Rule ID       | Category | Severity | Notes
---------------|----------|----------|-----------------------------------------
 LuceneDev1000 | Design   | Warning  | Floating point types should not be compared for exact equality
 LuceneDev1001 | Design   | Warning  | Floating point types should be formatted with J2N methods

## Release {{vnext}}

### New Rules

 Rule ID       | Category | Severity | Notes
---------------|----------|----------|-----------------------------------------
 LuceneDev1002 | Design   | Warning  | Floating point type arithmetic needs to be checked

### Removed Rules

 Rule ID       | Notes
---------------|-------------------------------------------------
 LuceneDev1001 | Replaced with LuceneDev1002 (better precision)
```

---------------------------------------------

## Creating a Release Build

The release process is mostly automated. However, a manual review is required on the GitHub releases page. This allows you to:

1. Manually review and edit the release notes
2. Re-generate the release notes after editing PR tags and titles
3. Manually check the release packages
4. Abort the release to try again
5. Publish the release to deploy the packages to NuGet.org

<p align="center">
  <img src="images/release-build-outcomes.svg" alt="Release Build Outcomes" width="40%" align="center" />
</p>

### Create a Draft Release

Tagging the commit and pushing it to the GitHub repository will start the automated draft release. The progress of the release can be viewed in the [GitHub Actions UI](https://github.com/apache/lucenenet-codeanalysis-dev/actions). Select the run corresponding to the version tag that is pushed upstream to view the progress.

#### Tag the HEAD Commit

Run the following command to tag the HEAD commit of the release branch.

```console
nbgv tag
```

> [!NOTE]
> The release build workflow always builds from the HEAD commit of the release branch.

#### Push the Release Branch to the Upstream Repository

The final step to begin the release build is to push the tag and any new commits to the upstream repository.

```console
git push <remote-name> <release-branch> --follow-tags
```

> [!NOTE]
> If there are any local commits that have not yet been pushed, the above command will include them in the release.

The push will start the automated draft release which will take a few minutes. When completed, there will be a new draft release in the [GitHub Releases](https://github.com/apache/lucenenet-codeanalysis-dev/releases) corresponding to the version you tagged.

> [!NOTE]
> If the release doesn't appear, check the [GitHub Actions UI](https://github.com/apache/lucenenet-codeanalysis-dev/actions). Select the run corresponding to the version tag that is pushed upstream to view the progress.

There are 2 possible outcomes for the release workflow:

1. [Successful Draft Release](#successful-draft-release) - Proceed normally
2. [Failed Draft Release](#failed-draft-release) - Fix the problem that caused the release failure and reset the release branch for release

---------------------------------------------

### Successful Draft Release

#### Release Notes

Review the draft release notes and edit or regenerate them if necessary. Release notes are generated based on PR titles and categorized by their labels. If something is amiss, they can be corrected by editing the PR titles and labels, deleting the previously generated release notes, and clicking the Generate Release Notes button.

##### Labels that Apply to the Release Notes

The following labels are recognized by the release notes generator.

| GitHub Label                   | Action                                                   |
|--------------------------------|----------------------------------------------------------|
| notes:ignore                   | Removes the PR from the release notes                    |
| notes:breaking-change          | Categorizes the PR under "Breaking Changes"              |
| notes:new-feature              | Categorizes the PR under "New Features"                  |
| notes:bug-fix                  | Categorizes the PR under "Bug Fixes"                     |
| notes:performance-improvement  | Categorizes the PR under "Performance Improvements"      |
| notes:improvement              | Categorizes the PR under "Improvements"                  |
| notes:website-or-documentation | Categorizes the PR under "Website and API Documentation" |
| \<none of the above\>          | Categorizes the PR under "Other Changes"                 |

> [!NOTE]
> Using multiple labels from the above list is not supported and the first category in the above list will be used if more than one is applied to a GitHub pull request.

#### Release Artifacts

The release will also attach the NuGet packages that will be released to NuGet. Download the packages and run some basic checks:

1. Put the `.nupkg` files into a local directory, and add a reference to the directory from Visual Studio. See [this answer](https://stackoverflow.com/a/10240180) for the steps. Verify that the NuGet packages can be referenced by a new project and that the project compiles.
2. Check the version information in [JetBrains dotPeek](https://www.jetbrains.com/decompiler/) to ensure the assembly version, file version, and informational version are consistent with what was specified in `version.json`.
3. Open the `.nupkg` files in [NuGet Package Explorer](https://www.microsoft.com/en-us/p/nuget-package-explorer/9wzdncrdmdm3#activetab=pivot:overviewtab) and check that files in the packages are present and that the XML config is up to date.

#### Publish the Release

Once everything is in order, the release can be published, which will deploy the packages to NuGet.org automatically.

> [!NOTE]
> While the deployment will probably succeed, note that there is currently no automation if it fails to deploy on the first try. The GitHub API key must be regenerated once per year. If you are uncertain that it is still valid, check the expiry date in the NuGet.org portal now and regenerate, if needed. Update the `NUGET_API_KEY` in [GitHub Secrets](https://github.com/apache/lucenenet-codeanalysis-dev/settings/secrets/actions) with the new key.

At the bottom of the draft release page, click on **Publish release**.

---------------------------------------------

### Failed Draft Release

If the build failed in any way, the release can be restarted by deleting the tag and trying again. First check to see the reason why the build failed in the [GitHub Actions UI](https://github.com/apache/lucenenet-codeanalysis-dev/actions) and correct any problems that were reported.

#### Restarting the Draft Release

##### Delete the Failed Tag

Since the tag did not result in a release, it is important to delete it to avoid a confusing release history.

```console
git tag -d v<package-version>
git push --delete <remote-name> v<package-version>
```

##### Resetting the Version in `AnalyzerReleases.Shipped.md`

If you previously added a new section to `AnalyzerReleases.Shipped.md`, it may contain a version number that no longer corresponds to the release. Change the release header to include the replacement token, once again.

```markdown
## Release {{vnext}}
```

Then commit the change to the release branch.

Next, follow the same procedure starting at [Tag the HEAD Commit](#tag-the-head-commit) to restart the draft release.

---------------------------------------------

## Post Release Steps

### Merge the Release Branch

Finally, merge the release branch back into the main branch and push the changes to the upstream repository.

> [!IMPORTANT]
> Release branches start with `release/v`.

```console
git checkout main
git merge <release-branch>
git push <remote-name> main
```

### Delete the Release Branch

From this point, the release will be tracked historically using the Git tag, so there is no reason to keep the release branch once it has been merged. You may wish to delay the deletion for a few days in case it is needed for some reason, but when you are ready, the commands to delete the local and remote branches are:

> [!IMPORTANT]
> Release branches start with `release/v`. Take care not to delete the tag, which starts with a `v`.

```console
git branch -d <release-branch>
git push --delete <remote-name> <release-branch>
```

### Update Lucene.NET

The Lucene.NET project is the only consumer of this package. If the release was intended for general use (not just a one-off scan), update the version in `Dependencies.props` to reflect the new release and submit a pull request to [the Lucene.NET repository](https://github.com/apache/lucenenet).
