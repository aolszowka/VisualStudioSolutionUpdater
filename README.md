# VisualStudioSolutionUpdater
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
* Add all additional projects into this Solution Folder

Currently this tool **WILL NOT** add Configuration information to the Solution; you are still REQUIRED to manually open the solution to blow this information in.

## Usage
```
Usage: VisualStudioSolutionUpdater [validate] directory/solution [ignore.txt]

Given either a Visual Studio Solution (*.sln) or a Directory to Scan; Validate
or update any solution file that is missing an N-Order ProjectReference Project
in the Solution File.

Invalid Command/Arguments. Valid commands are:

Directory-Solution [IgnorePatterns.txt]
    [MODIFIES] If given a solution file or a directory find all solution
    files then opening each solution find all N-Order ProjectReference projects,
    then add any missing projects to the Solution file into a Solution Subfolder
    called "Dependencies" saving the the results back to disk.

validate Directory-Solution [IgnorePatterns.txt]
    [READS] Performs the above operation but instead do not write the solution
    back to the disk.

In all cases you can provide an optional argument of IgnorePatterns.txt (you
can use any filename) which should be a plain text file of regular expression
filters of solution files you DO NOT want this tool to operate on.
```

## Hacking
### Supported Project Types
The most likely change you will want to make is changing the supported project files. In theory this tool should support any MSBuild Project Format that utilizes a ProjectGuid.

Start by looking at `SolutionGenerationUtilities.SUPPORTED_PROJECT_TYPES` and follow the rabbit trail from there.

### Configuration Generation
What really needs to happen is we need a usable API for creating Solution Files; baring that one possible solution would be to reverse engineer what Visual Studio is doing to create the various configuration elements. THIS IS NOT TRIVIAL.

## Contributing
Pull requests and bug reports are welcomed so long as they are MIT Licensed.

## License
This tool is MIT Licensed.
