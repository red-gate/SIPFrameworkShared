[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $BranchName = 'dev',
    [bool] $IsDefaultBranch = $false,
    [string] $NugetFeedUrl,
    [string] $NugetFeedApiKey,
    [string] $SigningServiceUrl
)

$RootDir = "$PsScriptRoot\.." | Resolve-Path
$OutputDir = "$RootDir\.output\$Configuration"
$LogsDir = "$OutputDir\logs"
$NugetPackageOutputDir = "$OutputDir\nugetpackages"
$Solution = "$RootDir\SIPFrameworkShared.sln"
$PublishNugetPackages = $env:TEAMCITY_VERSION -and $IsDefaultBranch
$NugetExe = "$PSScriptRoot\packages\Nuget.CommandLine\tools\Nuget.exe" | Resolve-Path

task CreateFolders {
    New-Item $OutputDir -ItemType Directory -Force | Out-Null
    New-Item $LogsDir -ItemType Directory -Force | Out-Null
    New-Item $NugetPackageOutputDir -ItemType Directory -Force | Out-Null
}

task GenerateVersionInformation {
    "Retrieving version information"

    # For dev builds, version suffix is always 0
    $versionSuffix = 0
    if($env:BUILD_NUMBER) {
        $versionSuffix = $env:BUILD_NUMBER
    }

    $ReleaseNotesPath = "$RootDir\RELEASENOTES.md" | Resolve-Path
    $Notes = Read-ReleaseNotes -ReleaseNotesPath $ReleaseNotesPath
    $script:Version = [System.Version] "$($Notes.Version).$VersionSuffix"
    $script:ReleaseNotes = [string] $Notes.Content

    # Establish assembly version number
    $script:AssemblyVersion = [version] "$($script:Version.Major).0.0.0"
    $script:AssemblyFileVersion = [version] "$script:Version.0"

    TeamCity-PublishArtifact "$ReleaseNotesPath"

    TeamCity-SetBuildNumber $script:Version

    $script:NugetPackageVersion = New-NugetPackageVersion -Version $script:Version -BranchName $BranchName -IsDefaultBranch $IsDefaultBranch

    "Version = $script:Version"
    "AssemblyVersion = $script:AssemblyVersion"
    "AssemblyFileVersion = $script:AssemblyFileVersion"
    "NugetPackageVersion = $script:NugetPackageVersion"
    "ReleaseNotes = $script:ReleaseNotes"
}

# Synopsis: Update the version info in all AssemblyInfo.cs
task UpdateVersionInfo GenerateVersionInformation, {
    "Updating assembly information"

    # Ignore anything under the Testing/ folder
    @(Get-ChildItem $RootDir AssemblyInfo.cs -Recurse) | ForEach {
        Update-AssemblyVersion $_.FullName `
            -Version $script:AssemblyVersion `
            -FileVersion $script:AssemblyFileVersion `
            -InformationalVersion $script:NuGetPackageVersion
    }
}

# Synopsis: A task that makes sure our initialization tasks have been run before we can do anything useful
task Init CreateFolders, GenerateVersionInformation

# Synopsis: Compile the Visual Studio solution
task Compile Init, UpdateVersionInfo, {
    Set-Alias msbuild (Resolve-MSBuild -MinimumVersion 15.0)
    try {
        exec {
            & msbuild `
                $Solution `
                /maxcpucount `
                /nodereuse:false `
                /target:Build `
                /p:Configuration=$Configuration `
                /flp1:verbosity=normal`;LogFile=$LogsDir\_msbuild.log.normal.txt `
                /flp2:WarningsOnly`;LogFile=$LogsDir\_msbuild.log.warnings.txt `
                /flp3:PerformanceSummary`;NoSummary`;verbosity=quiet`;LogFile=$LogsDir\_msbuild.log.performanceSummary.txt `
                /flp4:verbosity=detailed`;LogFile=$LogsDir\_msbuild.log.detailed.txt `
                /flp5:verbosity=diag`;LogFile=$LogsDir\_msbuild.log.diag.txt `
        }
    } finally {
        TeamCity-PublishArtifact "$LogsDir\_msbuild.log.* => logs/msbuild.$Configuration.logs.zip"
    }
}

# Synopsis: Sign all the RedGate assemblies (Release and Obfuscated)
task SignAssemblies -If ($Configuration -eq 'Release' -and $SigningServiceUrl) {
    Get-Item -Path "$RootDir\Build\Release\*.*" -Include 'Redgate*.dll' |
        Invoke-SigningService -SigningServiceUrl $SigningServiceUrl -Verbose
}

# Synopsis: Build the nuget packages.
task BuildNugetPackages Init, {
    New-Item $NugetPackageOutputDir -ItemType Directory -Force | Out-Null

    $escaped=$ReleaseNotes.Replace('"','\"')
    $properties = "releaseNotes=$escaped"

    "$RootDir\Nuspec\*.nuspec" | Resolve-Path | ForEach {
        exec {
            & $NugetExe pack $_ `
                -Version $NugetPackageVersion `
                -OutputDirectory $NugetPackageOutputDir `
                -BasePath $RootDir `
                -Properties $properties `
                -NoPackageAnalysis
        }
    }

    TeamCity-PublishArtifact "$NugetPackageOutputDir\*.nupkg => NugetPackages"
}

# Synopsis: Publish the nuget packages (Teamcity only)
task PublishNugetPackages -If($PublishNugetPackages) {
  assert ($NugetFeedUrl) '$NugetFeedUrl is missing. Cannot publish nuget packages'
  assert ($NugetFeedApiKey) '$NugetFeedApiKey is missing. Cannot publish nuget packages'

  Get-ChildItem $NugetPackageOutputDir -Filter "*.nupkg" | ForEach {
    & $NugetExe push $_.FullName -Source $NugetFeedUrl -ApiKey $NugetFeedApiKey
  }
}

task BuildArtifacts {
  TeamCity-PublishArtifact "$RootDir\Build\** => Build.zip"
}

# Synopsis: Build the project.
task Build Init, Compile, SignAssemblies, BuildArtifacts, BuildNugetPackages, PublishNugetPackages

# Synopsis: By default, Call the 'Build' task
task . Build
