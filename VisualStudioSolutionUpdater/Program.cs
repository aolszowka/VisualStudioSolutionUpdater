// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018-2020. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace VisualStudioSolutionUpdater
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using NDesk.Options;

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
            string solutionOrDirectoryArgument = null;
            bool isValidateTask = false;
            string ignoredSolutionPatternsArgument = null;
            bool filterConditionalReferences = false;
            bool showHelp = false;

            OptionSet p = new OptionSet() {
                { "<>", v => solutionOrDirectoryArgument = v },
                { "v|validate", "Perform Validation Only, Save No Changes, Exit Code is Number of Solutions Modified.", v => isValidateTask = v != null },
                { "f|filter|filterConditionalReferences", "Enable Filtering of Conditional References", v => filterConditionalReferences = v != null },
                { "i|ignore|ignorePatterns=", "A plain-text file containing Regular Expressions (one per line) of solution file names/paths to ignore", v=> ignoredSolutionPatternsArgument = v },
                { "?|h|help", "Show this message and exit", v => showHelp = v != null },
            };

            // Parse the Options
            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("VisualStudioSolutionUpdater: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `VisualStudioSolutionUpdater --help` for more information.");
                return;
            }


            if (showHelp || solutionOrDirectoryArgument == null)
            {
                errorCode = ShowUsage(p);
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                if (isValidateTask)
                {
                    sb.Append("Validating");
                }
                else
                {
                    sb.Append("Updating");
                }

                if (Directory.Exists(solutionOrDirectoryArgument))
                {
                    string[] ignoredSolutionPatterns = new string[0];
                    if (ignoredSolutionPatternsArgument != null)
                    {
                        ignoredSolutionPatterns = _GetIgnoredSolutionPatterns(ignoredSolutionPatternsArgument).ToArray();
                    }

                    sb.Append($" all Visual Studio Solutions (*.sln) in `{solutionOrDirectoryArgument}`");

                    if (ignoredSolutionPatterns.Any())
                    {
                        sb.Append($" except those filtered by `{ignoredSolutionPatternsArgument}`");
                    }

                    if (filterConditionalReferences)
                    {
                        sb.Append(" and filtering conditional references");
                    }

                    Console.WriteLine(sb.ToString());

                    errorCode = FixAllSolutions(solutionOrDirectoryArgument, ignoredSolutionPatterns, filterConditionalReferences, isValidateTask == false);
                }
                else if (File.Exists(solutionOrDirectoryArgument))
                {
                    // We need the Full Path
                    solutionOrDirectoryArgument = new FileInfo(solutionOrDirectoryArgument).FullName;

                    sb.Append($" Single Solution `{solutionOrDirectoryArgument}`");

                    if (filterConditionalReferences)
                    {
                        sb.Append(" and filtering conditional references");
                    }

                    Console.WriteLine(sb.ToString());

                    if (UpdateSingleSolution(solutionOrDirectoryArgument, filterConditionalReferences, isValidateTask == false))
                    {
                        errorCode = 1;
                    }
                }
                else
                {
                    string error = $"The provided path `{solutionOrDirectoryArgument}` is not a folder or file.";
                    Console.WriteLine(error);
                    errorCode = 9009;
                }
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

        private static int ShowUsage(OptionSet p)
        {
            Console.WriteLine(Resources.HelpMessage);
            p.WriteOptionDescriptions(Console.Out);
            return 21;
        }

        /// <summary>
        /// Given a single solution file; add all N-Order ProjectReferences to the Solution.
        /// </summary>
        /// <param name="targetSolution">The solution to modify.</param>
        /// <param name="saveChanges">Indicates whether or not to save changes back to disk.</param>
        /// <returns><c>true</c> if the solution needed to be updated; otherwise, <c>false</c>.</returns>
        static bool UpdateSingleSolution(string targetSolution, bool filterConditionalReferences, bool saveChanges)
        {
            bool solutionUpdated = false;

            try
            {
                solutionUpdated = SolutionUpdater.Update(targetSolution, filterConditionalReferences, saveChanges);

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
        static int FixAllSolutions(string targetDirectory, IEnumerable<string> ignoredSolutionPatterns, bool filterConditionalReferences, bool saveChanges)
        {
            int numberOfFixedSolutions = 0;

            IEnumerable<string> targetSolutions =
                Directory
                .EnumerateFiles(targetDirectory, "*.sln", SearchOption.AllDirectories)
                .Where(targetSolution => ShouldProcessSolution(targetSolution, ignoredSolutionPatterns));

            Parallel.ForEach(targetSolutions, targetSolution =>
            {
                if (UpdateSingleSolution(targetSolution, filterConditionalReferences, saveChanges))
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

            if (isSolutionIgnored)
            {
                shouldProcessSolution = false;
            }

            return shouldProcessSolution;
        }
    }
}
