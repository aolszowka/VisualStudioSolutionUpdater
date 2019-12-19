// -----------------------------------------------------------------------
// <copyright file="SolutionUpdater.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018-2019. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace VisualStudioSolutionUpdater
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class SolutionUpdater
    {
        /// <summary>
        /// Given a Visual Studio Solution (sln) add any missing N-Order Project References.
        /// </summary>
        /// <param name="targetSolution">The solution file to update</param>
        /// <returns><c>true</c> if the solution was updated; otherwise, <c>false</c>.</returns>
        public static bool Update(string targetSolution, bool saveChanges)
        {
            bool solutionModified = false;

            // Get a list of projects in the solution
            string[] existingProjects = SolutionUtilities.GetProjectsFromSolution(targetSolution).ToArray();

            // Get an updated list of dependencies
            string[] resolvedNOrderReferences = MSBuildUtilities.ProjectsIncludingNOrderDependencies(existingProjects).ToArray();

            // Filter to only new projects
            string[] newReferences = resolvedNOrderReferences.Except(existingProjects, StringComparer.InvariantCultureIgnoreCase).ToArray();

            if (newReferences.Length == 0)
            {
                solutionModified = false;
            }
            else
            {
                solutionModified = true;

                if (saveChanges)
                {
                    _InsertNewProjects(targetSolution, newReferences);
                }
            }

            return solutionModified;
        }

        internal static void _InsertNewProjects(string targetSolution, IEnumerable<string> newReferences)
        {
            string solutionRoot = PathUtilities.AddTrailingSlash(Path.GetDirectoryName(targetSolution));
            List<string> projectFragmentsToInsert = new List<string>();
            List<string> dependencyFolderItems = new List<string>();

            // Perform all the generation of the elements to insert
            string dependenciesFolderGuid;
            bool dependenciesFolderExists = SolutionUtilities.TryGetDepedenciesFolderGuid(targetSolution, out dependenciesFolderGuid);

            if (dependenciesFolderExists != true)
            {
                projectFragmentsToInsert.Add(SolutionGenerationUtilities.FragmentForSolutionFolder("Dependencies", dependenciesFolderGuid));
            }

            foreach (string newReference in newReferences)
            {
                (string ProjectFragment, string ProjectGuid) solutionFragment = SolutionGenerationUtilities.FragmentForProject(solutionRoot, newReference);
                projectFragmentsToInsert.Add(solutionFragment.ProjectFragment);
                dependencyFolderItems.Add(solutionFragment.ProjectGuid);
            }

            // We only want to read the file once
            string[] solutionLines = File.ReadLines(targetSolution).ToArray();

            // Now each of these build upon each other; we need to evaluate
            // each of the actions completely prior to the next step.
            solutionLines = _InsertProjectFragments(solutionLines, projectFragmentsToInsert).ToArray();
            solutionLines = _InsertDependencyFolderItems(solutionLines, dependenciesFolderGuid, dependencyFolderItems).ToArray();

            // Finally write it out once
            SolutionGenerationUtilities.WriteSolutionFileToDisk(targetSolution, solutionLines);
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

            // This really stinks because we have to scan the whole file to see if it has a (NestedProjects) section
            bool hasNestedProjectGlobal = existingSolutionLines.AsParallel().Any(lineInSolution => lineInSolution.Trim().Equals(GLOBAL_SECTION_SENTINEL));

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
                        yield return "\tGlobalSection(NestedProjects) = preSolution";
                        foreach (string dependencyFolderItem in dependencyFolderItems)
                        {
                            yield return $"\t\t{dependencyFolderItem} = {dependenciesFolderGuid}";
                        }
                        yield return "\tEndGlobalSection";
                    }

                    yield return existingLine;
                }
            }
        }
    }
}
