// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018-2019. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace VisualStudioSolutionUpdater
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using VisualStudioSolutionUpdater.Properties;

    class Program
    {
        /// <summary>
        /// Utility to update Visual Studio Solution Files (SLN), scans the
        /// given solution or directory for solution files, and ensures that
        /// solution file contains all of the N-Order ProjectReference
        /// projects.
        /// </summary>
        /// <param name="args">See <see cref="ShowUsage"/></param>
        static void Main(string[] args)
        {
            int errorCode = 0;

            if (args.Any())
            {
                string command = args.First().ToLowerInvariant();

                if (command.Equals("-?") || command.Equals("/?") || command.Equals("-help") || command.Equals("/help"))
                {
                    errorCode = ShowUsage();
                }
                else if (command.Equals("validate"))
                {
                    if (args.Length < 2)
                    {
                        string error = "You must provide either a file or directory as a second argument to use validate";
                        Console.WriteLine(error);
                        errorCode = 1;
                    }
                    else
                    {
                        // The second argument is a directory
                        string targetArgument = args[1];

                        if (Directory.Exists(targetArgument))
                        {
                            string[] ignoredSolutionPatterns = new string[0];

                            if (args.Length == 2)
                            {
                                string validatingAllSolutions = $"Validating all solutions in `{targetArgument}`";
                                Console.WriteLine(validatingAllSolutions);
                            }
                            else
                            {
                                string ignoredSolutionsArgument = args[2];
                                string validatingAllSolutions = $"Validating all solutions in `{targetArgument}` except those filtered by `{ignoredSolutionsArgument}`";
                                Console.WriteLine(validatingAllSolutions);

                                // Because we're going to constantly use this for lookups save it off
                                ignoredSolutionPatterns = _GetIgnoredSolutionPatterns(ignoredSolutionsArgument).ToArray();
                            }

                            errorCode = FixAllSolutions(targetArgument, ignoredSolutionPatterns, false);
                        }
                        else if (File.Exists(targetArgument))
                        {
                            string validatingSingleFile = $"Validating solution `{targetArgument}`";
                            Console.WriteLine(validatingSingleFile);
                            if (UpdateSingleSolution(targetArgument, false))
                            {
                                errorCode = 1;
                            }
                        }
                        else
                        {
                            string error = $"The provided path `{targetArgument}` is not a folder or file.";
                            errorCode = 9009;
                        }
                    }
                }
                else
                {
                    string targetPath = command;

                    if (Directory.Exists(targetPath))
                    {
                        IEnumerable<string> ignoredSolutionPatterns = new string[0];

                        if (args.Length == 1)
                        {
                            string updatingAllSolutionsInDirectory = $"Updating all Visual Studio Solutions (*.sln) in `{targetPath}`";
                            Console.WriteLine(updatingAllSolutionsInDirectory);
                        }
                        else
                        {
                            string ignoredSolutionsArgument = args[1];
                            string updatingAllSolutionsInDirectory = $"Updating all solutions in `{targetPath}` except those filtered by `{ignoredSolutionsArgument}`";
                            Console.WriteLine(updatingAllSolutionsInDirectory);

                            // Because we're going to constantly use this for lookups save it off
                            ignoredSolutionPatterns = _GetIgnoredSolutionPatterns(ignoredSolutionsArgument).ToArray();
                        }

                        FixAllSolutions(targetPath, ignoredSolutionPatterns, true);
                        errorCode = 0;
                    }
                    else if (File.Exists(targetPath))
                    {
                        string updatingSingleFile = $"Updating solution `{targetPath}`";
                        Console.WriteLine(updatingSingleFile);
                        UpdateSingleSolution(targetPath, true);
                        errorCode = 0;
                    }
                    else
                    {
                        string error = $"The specified path `{targetPath}` is not valid.";
                        Console.WriteLine(error);
                        errorCode = 1;
                    }
                }
            }
            else
            {
                // This was a bad command
                errorCode = ShowUsage();
            }

            Environment.Exit(errorCode);
        }

        private static IEnumerable<string> _GetIgnoredSolutionPatterns(string targetIgnoreFile)
        {
            if (!File.Exists(targetIgnoreFile))
            {
                string exceptionMessage = $"The specified ignore pattern file at `{targetIgnoreFile}` did not exist or was not accessible.";
                throw new InvalidOperationException(exceptionMessage);
            }

            IEnumerable<string> ignoredPatterns =
                File
                .ReadLines(targetIgnoreFile)
                .Where(currentLine => !currentLine.StartsWith("#"));

            return ignoredPatterns;
        }

        private static int ShowUsage()
        {
            Console.WriteLine(Resources.HelpMessage);
            return 21;
        }

        /// <summary>
        /// Given a single solution file; add all N-Order ProjectReferences to the Solution.
        /// </summary>
        /// <param name="targetSolution">The solution to modify.</param>
        /// <param name="saveChanges">Indicates whether or not to save changes back to disk.</param>
        /// <returns><c>true</c> if the solution needed to be updated; otherwise, <c>false</c>.</returns>
        static bool UpdateSingleSolution(string targetSolution, bool saveChanges)
        {
            bool solutionUpdated = false;

            try
            {
                solutionUpdated = SolutionUpdater.Update(targetSolution, saveChanges);

                if (solutionUpdated)
                {
                    Console.WriteLine(targetSolution);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Bad Solution `{targetSolution}` Error `{ex.Message}`";

                Console.WriteLine(errorMessage);
            }

            return solutionUpdated;
        }

        /// <summary>
        ///     Given a target directory find all Visual Studio Solution Files
        /// (*.sln) and attempt to update their N-Order ProjectReferences.
        /// </summary>
        /// <param name="targetDirectory">The directory to scan for Solution Files.</param>
        /// <param name="ignoredSolutionPatterns">An IEnumerable of ignored solution patterns.</param>
        /// <param name="saveChanges">Indicates whether or not to save the changes to the solutions.</param>
        /// <returns>An <see cref="int"/> indicating the number of solution files that were updated.</returns>
        static int FixAllSolutions(string targetDirectory, IEnumerable<string> ignoredSolutionPatterns, bool saveChanges)
        {
            int numberOfFixedSolutions = 0;

            IEnumerable<string> targetSolutions =
                Directory
                .EnumerateFiles(targetDirectory, "*.sln", SearchOption.AllDirectories)
                .Where(targetSolution => ShouldProcessSolution(targetSolution, ignoredSolutionPatterns));

            Parallel.ForEach(targetSolutions, targetSolution =>
            {
                if (UpdateSingleSolution(targetSolution, saveChanges))
                {
                    numberOfFixedSolutions++;
                }
            }
            );

            return numberOfFixedSolutions;
        }

        private static bool ShouldProcessSolution(string targetSolution, IEnumerable<string> ignoredSolutionPatterns)
        {
            bool shouldProcessSolution = true;

            bool isSolutionIgnored = ignoredSolutionPatterns.Any(ignoredPatterns => Regex.IsMatch(targetSolution, ignoredPatterns));

            if(isSolutionIgnored)
            {
                shouldProcessSolution = false;
            }

            return shouldProcessSolution;
        }
    }
}
