// -----------------------------------------------------------------------
// <copyright file="SolutionGenerationUtilities.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2018-2020. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace VisualStudioSolutionUpdater
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public static class SolutionGenerationUtilities
    {
        const string SOLUTION_FOLDER_TYPE_GUID = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

        static Dictionary<string, string> SUPPORTED_PROJECT_TYPES =
            new Dictionary<string, string>
            {
                { ".csproj", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}" },
                { ".sqlproj", "{00D1A9C2-B5F0-4AF3-8072-F6C62B433612}"},
                { ".synproj", "{BBD0F5D1-1CC4-42FD-BA4C-A96779C64378}"},
            };

        /// <summary>
        /// Generates a "Solution Fragment" that contains the correct syntax
        /// for adding a Project to a Visual Studio Solution.
        /// </summary>
        /// <param name="solutionRoot">The Directory that contains the solution file. This is used to generate the relative path to the CSPROJ File.</param>
        /// <param name="pathToProjFile">The fully qualified path to the Project File.</param>
        /// <returns>A named Tuple where the first element is the projectFragment lines (in order) for a Visual Studio Solution and the second is the projectGuid.</returns>
        internal static (IEnumerable<string> ProjectFragment, string ProjectGuid) FragmentForProject(string solutionRoot, string pathToProjFile)
        {
            string relativePathToProject = PathUtilities.GetRelativePath(solutionRoot, pathToProjFile);
            string projectTypeGuid = GetProjectTypeGuid(pathToProjFile);
            string projectName = Path.GetFileNameWithoutExtension(pathToProjFile);
            string projectGuid = MSBuildUtilities.GetMSBuildProjectGuid(pathToProjFile);

            // Fix up relative paths to use backslash instead of forward slash
            relativePathToProject = relativePathToProject.Replace('/', '\\');

            string[] fragment =
                new string[]
                {
                    $"Project(\"{projectTypeGuid}\") = \"{projectName}\", \"{relativePathToProject}\", \"{projectGuid}\"",
                    $"EndProject",
        };
            return (fragment, projectGuid);
        }

        /// <summary>
        ///    Generates a "Configuration Fragment" that contains the lines to
        /// add to a Visual Studio Solution file when adding a new project.
        /// </summary>
        /// <param name="projectGuid">The GUID of the project being added.</param>
        /// <param name="solutionConfigurations">The configurations for this solution.</param>
        /// <returns>The lines (in order) that would need to be added to the Solution Configuration section for this Project.</returns>
        internal static IEnumerable<string> FragmentForSolutionConfiguration(string projectGuid, IEnumerable<string> solutionConfigurations)
        {
            foreach(var config in solutionConfigurations)
            {
                yield return $"\t\t{projectGuid}.{config}.ActiveCfg = {config}";
                yield return $"\t\t{projectGuid}.{config}.Build.0 = {config}";
            }
        }

        /// <summary>
        /// Generates a "Solution Folder Fragment" that contains the correct
        /// syntax for adding a Project Folder to a Visual Studio Solution.
        /// </summary>
        /// <param name="solutionFolderName">The name of the folder to add.</param>
        /// <param name="solutionFolderGuid">The GUID of the Solution; including the {}'s</param>
        /// <returns>An <see cref="IEnumerable{string}"/> that represents a Solution Folder for a Visual Studio Solution to be inserted in order.</returns>
        internal static IEnumerable<string> FragmentForSolutionFolder(string solutionFolderName, string solutionFolderGuid)
        {
            string[] fragment =
                new string[]
                {
                    $"Project(\"{SOLUTION_FOLDER_TYPE_GUID}\") = \"{solutionFolderName}\", \"{solutionFolderName}\", \"{solutionFolderGuid}\"",
                    $"EndProject"
                };

            return fragment;
        }

        /// <summary>
        /// Returns the Project Type Guid for this project type.
        /// </summary>
        /// <param name="pathToProjFile">The path to the project file.</param>
        /// <returns>The Guid to be used in the Solution File.</returns>
        internal static string GetProjectTypeGuid(string pathToProjFile)
        {
            string result = string.Empty;

            string projectExtension = Path.GetExtension(pathToProjFile).ToLowerInvariant();

            if (SUPPORTED_PROJECT_TYPES.ContainsKey(projectExtension))
            {
                result = SUPPORTED_PROJECT_TYPES[projectExtension];
            }
            else
            {
                string message = $"The extension `{projectExtension}` was not recognized by this tool.";
                throw new NotSupportedException(message);
            }

            return result;
        }

        /// <summary>
        /// Given a Path and the lines of a Solution file, write them to disk.
        /// </summary>
        /// <param name="solutionFilePath">The path to the solution.</param>
        /// <param name="solutionLines">The lines to write.</param>
        public static void WriteSolutionFileToDisk(string solutionFilePath, IEnumerable<string> solutionLines)
        {
            // A 32kb Buffer seems to be about the best trade off
            using (StreamWriter sw = new StreamWriter(solutionFilePath, false, new UTF8Encoding(true, true), 32768))
            {
                foreach (string solutionLine in solutionLines)
                {
                    sw.WriteLine(solutionLine);
                }
            }
        }
    }
}
