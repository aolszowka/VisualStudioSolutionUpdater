// -----------------------------------------------------------------------
// <copyright file="SolutionUtilities.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018-2019. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace VisualStudioSolutionUpdater
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Construction;

    internal static class SolutionUtilities
    {
        internal static bool TryGetDepedenciesFolderGuid(SolutionFile targetSolution, out string dependenciesFolderGuid)
        {
            bool dependenciesFolderFound = false;
            dependenciesFolderGuid = string.Empty;

            ProjectInSolution[] dependenciesFolders =
                targetSolution.ProjectsInOrder.Where(project => project.ProjectType == SolutionProjectType.SolutionFolder).Where(projectSolutionFolder => projectSolutionFolder.ProjectName.Equals("Dependencies")).ToArray();

            if (dependenciesFolders.Length == 1)
            {
                // Best case is a folder already exist with this project; return its Guid
                dependenciesFolderGuid = dependenciesFolders.First().ProjectGuid;
                dependenciesFolderFound = true;
            }
            else if (dependenciesFolders.Length > 1)
            {
                string message = $"There were {dependenciesFolders.Length} Dependencies Folders Found; This is unexpected";
                throw new InvalidOperationException(message);
            }
            else
            {
                dependenciesFolderGuid = Guid.NewGuid().ToString("B");
                dependenciesFolderFound = false;
            }

            return dependenciesFolderFound;
        }

        internal static bool TryGetDepedenciesFolderGuid(string targetSolution, out string dependenciesFolderGuid)
        {
            SolutionFile solution = SolutionFile.Parse(targetSolution);
            return TryGetDepedenciesFolderGuid(solution, out dependenciesFolderGuid);
        }

        internal static IEnumerable<string> GetProjectsFromSolution(string targetSolution)
        {
            SolutionFile solution = SolutionFile.Parse(targetSolution);
            string solutionFolder = Path.GetDirectoryName(targetSolution);

            return
                solution
                .ProjectsInOrder
                .Where(project => project.ProjectType != SolutionProjectType.SolutionFolder)
                .Select(project => project.RelativePath)
                .Select(projectRelativePath => PathUtilities.ResolveRelativePath(solutionFolder, projectRelativePath));
        }
    }
}
