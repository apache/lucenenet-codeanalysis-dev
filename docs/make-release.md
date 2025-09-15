# Making a Release

This project uses Nerdbank.GitVersioning to assist with creating version numbers based on the current branch and commit. This tool handles making pre-release builds on the main branch and production releases on release branches.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [nbgv tool](https://www.nuget.org/packages/nbgv/) (the version must match the one defined in [Directory.Packages.props](../Directory.Packages.props))

### Installing NBGV Tool

Perform a one-time install of the nbgv tool using the following dotnet CLI command:

```console
dotnet tool install -g nbgv --version <theActualVersion>
```

## Versioning Primer

This project uses [Semantic Versioning 2.0](https://semver.org/spec/v2.0.0.html) and strictly adheres to the guidelines about bumping the major, minor and build numbers of the version number.

The assembly version should remain the same in all cases, except when the major version changes, so that it can be used as a drop-in replacement.

## Creating a Release Branch

### Release Workflow Overview

![Release Workflow](images/release-workflow.svg)

### Ready to Release

When the changes in the main branch are ready to release, create a release branch using the following nbgv tool command as specified in the [documentation](https://github.com/dotnet/Nerdbank.GitVersioning/blob/master/doc/nbgv-cli.md).

For example, assume the `version.json` file on the main branch is currently setup as `2.0.0-alpha.{height}`. We want to go from this version to a release of `2.0.0` and set the next version on the main branch as `2.0.1-alpha.{height}`.

```console
nbgv prepare-release --nextVersion 2.0.1
```

The command should respond with:

```console
release/v2.0 branch now tracks v2.0.0 stabilization and release.
main branch now tracks v2.0.1-alpha.{height} development.
```

The tool created a release branch named `release/v2.0`. Every build from this branch (regardless of how many commits are added) will be versioned 2.0.0. 

### Requires Stabilization

When creating a release that may require a few iterations to become stable, it is better to create a beta branch (more about that decision can be found [here](https://github.com/dotnet/Nerdbank.GitVersioning/blob/master/doc/nbgv-cli.md#preparing-a-release)). Starting from the same point as the [Ready to Release](#ready-to-release) scenario, we use the following command.

```console
nbgv prepare-release beta --nextVersion 2.0.1
```

The command should respond with:

```console
release/v2.0 branch now tracks v2.0.0-beta.{height} stabilization and release.
main branch now tracks v2.0.0-alpha.{height} development.
```

The tool created a release branch named `release/v2.0`. Every build from this branch will be given a unique pre-release version starting with 2.0.0-beta and ending in a dot followed by one or more digits.

### Bumping the Version Manually

When releasing a version that does not directly follow the current release version, manually update the `version` (and `assemblyVersion` if this is a major version bump) in `version.json` before creating the release branch. See the [version.json schema](https://raw.githubusercontent.com/AArnott/Nerdbank.GitVersioning/master/src/NerdBank.GitVersioning/version.schema.json) to determine valid options.

## Correcting the Release Version Height

Nerdbank.GitVersioning is designed in a way that it doesn't produce the same version number twice. This is done by using a "git height", which counts the number of commits since the last version update. This works great for CI, but is less than ideal when we don't want to skip over versions for the release.

Since Nerdbank.GitVersioning considers each commit a new "version," the `versionHeightOffset` can be adjusted on the release branch to ensure the release uses the correct version number. This can be done by using the following command to see what version we are currently on, and then adjusting the value accordingly.

```console
nbgv get-version
```

> [!NOTE]
> Officially, it is not recommended to use `versionHeightOffset` and in general that is true. However, using it in a narrow scope, such as on a release branch should be okay. In practice, users will not build from these branches, they will build from a release tag.

Then open the `version.json` file at the repository root, and set the `versionHeightOffset` using the formula `versionHeightOffset - ((versionHeight - desiredHeight) + 1)`. For example, if the current version is 2.0.1-beta.14 and we want to release 2.0.1-beta.5 (because the last version released was 2.0.1-beta.4), and the `versionHeightOffset` is set to -21:

###### Calculating versionHeightOffset
```
-21 - ((14 - 5) + 1) = -31
```

So, we must set `versionHeightOffset` to -31 and commit the change.

Note that the + 1 is because we are creating a new commit that will increment the number by 1. The change must be committed to see the change to the version number. Run the command again to check that the version will be correct.

```console
nbgv get-version
```

## Creating a Release Build

The release process is mostly automated. However, a manual review is required on the GitHub releases page. This allows you to:

1. Manually review and edit the release notes
2. Re-generate the release notes after editing PR tags and titles
3. Manually check the release packages
4. Abort the release to try again
5. Publish the release to deploy the packages to NuGet.org

## Create a Draft Release

Tagging the commit and pushing it to the GitHub repository will start the automated draft release. The progress of the release can be viewed in the [GitHub Actions UI](https://github.com/apache/lucenenet-codeanalysis-dev/actions). Select the run corresponding to the version tag that is pushed upstream to view the progress.

### Tagging the Commit

If you don't already know the version that corresponds to the HEAD commit, check it now.

```console
nbgv get-version
```

The reply will show a table of version information. 

```console
Version:                      2.0.0
AssemblyVersion:              2.0.0.0
AssemblyInformationalVersion: 2.0.0-beta.5+a54c015802
NuGetPackageVersion:          2.0.0-beta.5
NpmPackageVersion:            2.0.0-beta.5
```

Tag the commit with `v` followed by the NuGetPackageVersion.

```console
git tag -a v<package-version> <commit-hash> -m "v<package-version>"
git push <remote-name> <release-branch> --follow-tags
```

> [!NOTE]
> If there are any local commits that have not yet been pushed, the above command will include them in the release.

The push will start the automated draft release which will take a few minutes. When completed, there will be a new draft release in the [GitHub Releases](https://github.com/apache/lucenenet-codeanalysis-dev/releases) corresponding to the version you tagged.

> [!NOTE]
> If the release doesn't appear, check the [GitHub Actions UI](https://github.com/apache/lucenenet-codeanalysis-dev/actions). Select the run corresponding to the version tag that is pushed upstream to view the progress.

### Successful Draft Release

#### Release Notes

Review the draft release notes and edit or regenerate them if necessary. release notes are generated based on PR titles and categorized by their labels. If something is amiss, they can be corrected by editing the PR titles and labels, deleting the previously generated release notes, and clicking the Generate Release Notes button.

##### Labels that Apply to the Release Notes

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

1. Put the `.nupkg` files into a local directory, and add a reference to the directory from Visual Studio. See [this answer](https://stackoverflow.com/a/10240180) for the steps. Check to ensure the NuGet packages can be referenced by a new project and the project will compile.
2. Check the version information in [JetBrains dotPeek](https://www.jetbrains.com/decompiler/) to ensure the assembly version, file version, and informational version are consistent with what was specified in `version.json`.
3. Open the `.nupkg` files in [NuGet Package Explorer](https://www.microsoft.com/en-us/p/nuget-package-explorer/9wzdncrdmdm3#activetab=pivot:overviewtab) and check that files in the packages are present and that the XML config is up to date.

#### Publish the Release

Once everything is in order, the release can be published, which will deploy the packages to NuGet.org automatically.

> [!NOTE]
> While the deployment will probably succeed, note that there is currently no automation if it fails to deploy on the first try. The GitHub API key must be regenerated once per year. If you are uncertain that it is still valid, check the expiry date in the NuGet.org portal now and regenerate, if needed. Update the `NUGET_API_KEY` in [GitHub Secrets](https://github.com/apache/lucenenet-codeanalysis-dev/settings/secrets/actions) with the new key.

At the bottom of the draft release page, click on **Publish release**.

### Failed Draft Release

If the build failed in any way, the release can be restarted by deleting the tag and trying again. First check to see the reason why the build failed in the [GitHub Actions UI](https://github.com/apache/lucenenet-codeanalysis-dev/actions) and correct any problems that were reported.

#### Restarting the Draft Release

##### Delete the Failed Tag

Since the tag didn't make it to release, it is important to delete it to avoid a confusing release history. It is required to be removed if the next attempt will be for the same version number.

```console
git tag -d v<package-version>
git push --delete <remote-name> v<package-version>
```

##### Push a New Version Tag

> [!NOTE]
> The same version number can be reused if there weren't any commits required to correct the release problem or if the `versionHeightOffset` is changed as described above.

Once the issues have been addressed to fix the build and reset the version (if necessary), follow the same procedure starting at [Tagging the Commit](#tagging-the-commit) to restart the draft release.

## Post Release Steps

### Merge the Release Branch

Finally, merge the release branch back into the main branch and push the changes to GitHub.

```console
git checkout <main-branch>
git merge <release-branch>
git push <remote-name> <main-branch>
```
