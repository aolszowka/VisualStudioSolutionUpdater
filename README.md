# VisualStudioSolutionUpdater
![CI - Master](https://github.com/aolszowka/VisualStudioSolutionUpdater/workflows/CI/badge.svg?branch=master)

Utility program to Validate or Update Visual Studio Fix Solution Files (SLN) to Include their N-Order Project References

## When To Use This Tool
This tool is intended to be used when you are managing a complex dependency tree that uses [Microsoft Docs: Common MSBuild Project Items - ProjectReference](https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items?view=vs-2017#projectreference) and Visual Studio Solution Files to manage the dependency tree.

ProjectReference, when combined with a Visual Studio Solution File, assumes that all N-Order ProjectReference elements have been loaded into the solution. Failure to do so will cause the build to fail if you are building from a clean project.

**Caveat:** If, by chance, the build output exists for these projects which have not been included in the solution the build will succeed. It is unclear if this is by design or if this is an implementation detail that has been leveraged by others.

## Operation
This tool will:

* (If Given a Single Solution) Perform the below operation for just this Solution
* (If Given a Directory) Will scan the given directory and all subdirectories for Visual Studio Solution files (*.sln) and perform the operation below.

For Each Solution File
* Load the Solution file and then crawl each Project within the solution file
* For each Project crawl for its N-Order ProjectReferences
* Compare the distinct list of N-Order Project References to the existing projects within the solution (without regard to their location within the solution) and for any projects not currently contained within the solution.

The tool will then:
* (If it does not exist) Create a Solution Folder ([Microsoft Docs: SolutionFolder Interface](https://docs.microsoft.com/en-us/dotnet/api/envdte80.solutionfolder?view=visualstudiosdk-2017)) called "Dependencies"
    * Note that this tool now uses a hard-coded GUID for the "Dependencies" solution folder (if an existing one does not exist); see the Hacking Section for more information.
* Add all additional projects into this Solution Folder

This tooling will try to emulate the default Visual Studio behavior with regards to Configuration Information which is: When a new project is added, it is added to ALL Solution Configurations with a default to build. If this is not desired behavior then you need to manually edit the solution file within Visual Studio.

## Usage
There are now two ways to run this tool:

1. (Compiled Executable) Invoke the tool via `VisualStudioSolutionUpdater` and pass the arguments.
2. (Dotnet Tool) Install this tool using the following command `dotnet tool install VisualStudioSolutionUpdater` (assuming that you have the nuget package in your feed) then invoke it via `dotnet update-solution`

In both cases the flags to the tooling are identical:

```
Usage: Directory-or-Solution [-validate] [-ignorePatterns=ignore.txt] [-filterConditionalReferences]

Validate or update any solution file that is missing an N-Order ProjectReference
Project in the Solution File by putting them into a Solution sub-folder called
"Dependencies".

You can provide an optional argument of -ignorePatterns=IgnorePatterns.txt (you
can use any filename) which should be a plain text file of regular expression
filters of solution files you DO NOT want this tool to operate on.

The optional -filterConditionalReferences flag tells this tool to ignore any
ProjectReference that has a conditional associated with it at any level.

The optional -validate will tell this tool NOT to save changes but instead
return an exit code equal to the number of projects that would have been
modified.

Arguments:

               <>            Either a Visual Studio Solution (*.sln) or
                               Directory to scan for Visual Studio Solutions
  -v, --validate             Perform Validation Only, Save No Changes, Exit
                               Code is Number of Solutions Modified.
  -f, --filter, --filterConditionalReferences
                             Enable Filtering of Conditional References
  -i, --ignore, --ignorePatterns=VALUE
                             A plain-text file containing Regular Expressions
                               (one per line) of solution file names/paths to
                               ignore
  -?, -h, --help             Show this message and exit
```

## Hacking
### Dependencies (Solution Folder)
Previously this tool generated a new GUID on run if the solution did not have an existing Solution Folder called "Dependencies" this was changed to now use a hard-coded GUID for this purpose.

The reasoning is that if you have multiple branches of your code base where you are intending to run this tool you need consistency between versions in the solution file. Using a hard-coded value allows you to do this.

If this is not desired look at changing `SolutionUtilities.TryGetDepedenciesFolderGuid(SolutionFile, String)`

### Supported Project Types
The most likely change you will want to make is changing the supported project files. In theory this tool should support any MSBuild Project Format that utilizes a ProjectGuid.

Start by looking at `SolutionGenerationUtilities.SUPPORTED_PROJECT_TYPES` and follow the rabbit trail from there.

### API For Solution File Creation
What really needs to happen is we need a usable API for creating Solution Files; much of this tooling could be deprecated if `dotnet new sln` and `dotnet sln` were more mature products (see https://github.com/dotnet/cli/issues/11679 for example).

## GOTCHAs
### Relative Paths
The MSBuild Project format appears to use Windows style directory separator chars `\`, this can cause problems on other OSes supported by the .NET Core Runtime, it is believed these are all flushed out in the unit tests, but day to day usage of this tool really only occurs on Windows. I welcome any well formed bug reports!

## Contributing
Pull requests and bug reports are welcomed so long as they are MIT Licensed.

## License
This tool is MIT Licensed.

## Third Party Licenses
This project uses other open source contributions see [LICENSES.md](LICENSES.md) for a comprehensive listing.
