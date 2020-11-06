// -----------------------------------------------------------------------
// <copyright file="SolutionUpdater.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018-2020. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace VisualStudioSolutionUpdater
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.Build.Construction;

    public static class SolutionUpdater
    {
        /// <summary>
        /// Given a Visual Studio Solution (sln) add any missing N-Order Project References.
        /// </summary>
        /// <param name="targetSolution">The solution file to update</param>
        /// <returns><c>true</c> if the solution was updated; otherwise, <c>false</c>.</returns>
        public static bool Update(string targetSolution, bool filterConditionalReferences, bool saveChanges)
        {
            bool solutionModified = false;

            // Parse the "SolutionFile" Structure Once
            SolutionFile solution = SolutionFile.Parse(targetSolution);

            string[] newReferences = GetNewDependenciesInSolution(solution, filterConditionalReferences);

            if (newReferences.Length == 0)
            {
                solutionModified = false;
            }
            else
            {
                solutionModified = true;

                if (saveChanges)
                {
                    _InsertNewProjects(targetSolution, solution, newReferences);
                }
            }

            return solutionModified;
        }

        /// <summary>
        ///     Given a Solution File; Identify the new Projects that would
        /// need to be added to reflect the current dependency tree.
        /// </summary>
        /// <param name="solution">The Solution file to examine.</param>
        /// <returns>The new projects for the given solution</returns>
        internal static string[] GetNewDependenciesInSolution(SolutionFile solution, bool filterConditionalReferences)
        {
            // Get a list of projects in the solution
            IEnumerable<string> existingProjects = SolutionUtilities.GetProjectsFromSolution(solution);

            // Get an updated list of dependencies
            IEnumerable<string> resolvedNOrderReferences = MSBuildUtilities.ProjectsIncludingNOrderDependencies(existingProjects, filterConditionalReferences);

            // Filter to only new projects
            string[] newReferences = resolvedNOrderReferences.Except(existingProjects, StringComparer.InvariantCultureIgnoreCase).ToArray();

            return newReferences;
        }

        internal static void _InsertNewProjects(string targetSolutionPath, SolutionFile solution, IEnumerable<string> newReferences)
        {
            // Update the project in memory
            string[] solutionLines = _InsertNewProjectsInternal(targetSolutionPath, solution, newReferences);

            // Write it out to the file
            SolutionGenerationUtilities.WriteSolutionFileToDisk(targetSolutionPath, solutionLines);
        }

        /// <summary>
        /// Update the target solution to contain the new projects.
        /// </summary>
        /// <param name="targetSolutionPath">The path to the solution file.</param>
        /// <param name="solution">A <see cref="SolutionFile"/> that represents the solution.</param>
        /// <param name="newReferences">An <see cref="IEnumerable{string}"/> of projects to add to the solution.</param>
        /// <returns>The solution lines modified</returns>
        /// <remarks>
        ///     This internal version allows the lines to be updated in memory
        /// but does not write it to a file. This is done to assist in Unit
        /// Testing these changes.
        /// </remarks>
        internal static string[] _InsertNewProjectsInternal(string targetSolutionPath, SolutionFile solution, IEnumerable<string> newReferences)
        {
            string solutionRoot = PathUtilities.AddTrailingSlash(Path.GetDirectoryName(targetSolutionPath));
            List<string> projectFragmentsToInsert = new List<string>();
            List<string> dependencyFolderItems = new List<string>();
            List<string> projectConfigurationFragmentsToInsert = new List<string>();
            IEnumerable<string> solutionConfigurations = SolutionUtilities.GetSolutionConfigurations(solution);

            // Perform all the generation of the elements to insert
            string dependenciesFolderGuid;
            bool dependenciesFolderExists = SolutionUtilities.TryGetDepedenciesFolderGuid(solution, out dependenciesFolderGuid);

            if (dependenciesFolderExists != true)
            {
                projectFragmentsToInsert.AddRange(SolutionGenerationUtilities.FragmentForSolutionFolder("Dependencies", dependenciesFolderGuid));
            }

            foreach (string newReference in newReferences)
            {
                (IEnumerable<string> ProjectFragment, string ProjectGuid) solutionFragment = SolutionGenerationUtilities.FragmentForProject(solutionRoot, newReference);
                projectFragmentsToInsert.AddRange(solutionFragment.ProjectFragment);
                dependencyFolderItems.Add(solutionFragment.ProjectGuid);
                projectConfigurationFragmentsToInsert.AddRange(SolutionGenerationUtilities.FragmentForSolutionConfiguration(solutionFragment.ProjectGuid, solutionConfigurations));
            }

            // We only want to read the file once
            string[] solutionLines = File.ReadLines(targetSolutionPath).ToArray();

            // Now each of these build upon each other; we need to evaluate
            // each of the actions completely prior to the next step.
            solutionLines = _InsertProjectFragments(solutionLines, projectFragmentsToInsert).ToArray();
            solutionLines = _InsertDependencyFolderItems(solutionLines, dependenciesFolderGuid, dependencyFolderItems).ToArray();
            solutionLines = _InsertProjectConfigurationFragments(solutionLines, projectConfigurationFragmentsToInsert).ToArray();

            return solutionLines;
        }

        private static IEnumerable<string> _InsertProjectConfigurationFragments(string[] existingSolutionLines, IEnumerable<string> projectConfigurationFragmentsToInsert)
        {
            bool insertPerformed = false;

            foreach (string existingSolutionLine in existingSolutionLines)
            {
                yield return existingSolutionLine;

                if (insertPerformed == false && existingSolutionLine.Trim().Equals("GlobalSection(ProjectConfigurationPlatforms) = postSolution"))
                {
                    // We insert at this point
                    foreach (string projectConfigurationFragment in projectConfigurationFragmentsToInsert)
                    {
                        yield return projectConfigurationFragment;
                    }

                    insertPerformed = true;
                }
            }
        }

        internal static IEnumerable<string> _InsertProjectFragments(IEnumerable<string> existingSolutionLines, IEnumerable<string> projectFragmentsToInsert)
        {
            bool insertPerformed = false;

            foreach (string existingSolutionLine in existingSolutionLines)
            {
                if (insertPerformed == false && existingSolutionLine.Equals("Global"))
                {
                    // We insert at this point
                    foreach (string projectFragmentToInsert in projectFragmentsToInsert)
                    {
                        yield return projectFragmentToInsert;
                    }

                    insertPerformed = true;
                }

                yield return existingSolutionLine;
            }
        }

        internal static IEnumerable<string> _InsertDependencyFolderItems(IEnumerable<string> existingSolutionLines, string dependenciesFolderGuid, IEnumerable<string> dependencyFolderItems)
        {
            string GLOBAL_SECTION_SENTINEL = "GlobalSection(NestedProjects) = preSolution";
            string END_GLOBAL_SENTINEL = "EndGlobal";
            string END_GLOBALSECTION_SENTINEL = "EndGlobalSection";

            // This really stinks because we have to scan the whole file to see if it has a (NestedProjects) section
            bool hasNestedProjectGlobal =
                existingSolutionLines
                .AsParallel()
                .Any(lineInSolution => lineInSolution.Trim().Equals(GLOBAL_SECTION_SENTINEL));

            // Now we have to read the whole file again and then depending
            // on if the nested global existed or not either append or create
            // the section
            if (hasNestedProjectGlobal)
            {
                // This happens when the Dependencies folder already existed
                bool insertionPointReached = false;

                foreach (string existingLine in existingSolutionLines)
                {
                    yield return existingLine;

                    if (insertionPointReached == false && existingLine.Trim().Equals(GLOBAL_SECTION_SENTINEL))
                    {
                        foreach (string dependencyFolderItem in dependencyFolderItems)
                        {
                            yield return $"\t\t{dependencyFolderItem} = {dependenciesFolderGuid}";
                        }
                    }
                }
            }
            else
            {
                // This is performed when we need to create the Dependencies Folder
                bool insertionPointReached = false;

                foreach (string existingLine in existingSolutionLines)
                {
                    if (insertionPointReached == false && existingLine.Trim().Equals(END_GLOBAL_SENTINEL))
                    {
                        yield return $"\t{GLOBAL_SECTION_SENTINEL}";
                        foreach (string dependencyFolderItem in dependencyFolderItems)
                        {
                            yield return $"\t\t{dependencyFolderItem} = {dependenciesFolderGuid}";
                        }
                        yield return $"\t{END_GLOBALSECTION_SENTINEL}";
                    }

                    yield return existingLine;
                }
            }
        }
    }
}
