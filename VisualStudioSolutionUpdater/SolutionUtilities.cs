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
                dependenciesFolderGuid = "{DA34CE5D-031A-4C97-8DE8-A81F98C0288A}";
                dependenciesFolderFound = false;
            }

            return dependenciesFolderFound;
        }

        internal static IEnumerable<string> GetProjectsFromSolution(SolutionFile solution)
        {
            return
                solution
                .ProjectsInOrder
                .Where(project => project.ProjectType != SolutionProjectType.SolutionFolder)
                .Select(project => Path.GetFullPath(project.AbsolutePath));
        }

        internal static IEnumerable<string> GetSolutionConfigurations(SolutionFile solution)
        {
            foreach (SolutionConfigurationInSolution config in solution.SolutionConfigurations)
            {
                yield return config.FullName;
            }
        }
    }
}
