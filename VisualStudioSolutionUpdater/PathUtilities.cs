﻿// -----------------------------------------------------------------------
// <copyright file="PathUtilities.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2017-2020. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace VisualStudioSolutionUpdater
{
    using System;
    using System.IO;

    /// <summary>
    /// Utility class for dealing with Paths.
    /// </summary>
    public static class PathUtilities
    {
        /// <summary>
        /// Appends a trailing slash to any path given to this function.
        /// </summary>
        /// <param name="directory">A directory that may or may not contain a trailing slash.</param>
        /// <returns>The given directory path; with a trailing slash appended if need be.</returns>
        public static string AddTrailingSlash(string directory)
        {
            string returnValue = directory;

            if (!returnValue.EndsWith(Path.DirectorySeparatorChar))
            {
                returnValue = returnValue + Path.DirectorySeparatorChar;
            }

            return returnValue;
        }

        /// <summary>
        ///     Given two paths return the relative path of
        /// <paramref name="path2"/> in terms of <paramref name="path1"/>.
        /// </summary>
        /// <param name="path1">The path to make relative to.</param>
        /// <param name="path2">The path to get the relative path for.</param>
        /// <returns>The relative path to <paramref name="path2"/> in terms of <paramref name="path1"/></returns>
        public static string GetRelativePath(string path1, string path2)
        {
            return GetRelativePath(new FileInfo(path1), new FileInfo(path2));
        }

        /// <summary>
        ///     Given two paths return the relative path of
        /// <paramref name="path2"/> in terms of <paramref name="path1"/>.
        /// </summary>
        /// <param name="path1">The path to make relative to.</param>
        /// <param name="path2">The path to get the relative path for.</param>
        /// <returns>The relative path to <paramref name="path2"/> in terms of <paramref name="path1"/></returns>
        public static string GetRelativePath(FileSystemInfo path1, FileSystemInfo path2)
        {
            if (path1 == null) throw new ArgumentNullException("path1");
            if (path2 == null) throw new ArgumentNullException("path2");

            Func<FileSystemInfo, string> getFullName = delegate (FileSystemInfo path)
            {
                string fullName = path.FullName;

                if (path is DirectoryInfo)
                {
                    if (fullName[fullName.Length - 1] != Path.DirectorySeparatorChar)
                    {
                        fullName += Path.DirectorySeparatorChar;
                    }
                }
                return fullName;
            };

            string path1FullName = getFullName(path1);
            string path2FullName = getFullName(path2);

            Uri uri1 = new Uri(path1FullName);
            Uri uri2 = new Uri(path2FullName);
            Uri relativeUri = uri1.MakeRelativeUri(uri2);

            return Uri.UnescapeDataString(relativeUri.OriginalString);
        }

        /// <summary>
        /// Resolve a relative path given the base directory in which it is based.
        /// </summary>
        /// <param name="baseDirectory">The base path.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <returns>The "expanded" path.</returns>
        public static string ResolveRelativePath(string baseDirectory, string relativePath)
        {
            string absolutePath = Path.GetFullPath(Path.Combine(baseDirectory, FixUpPathDelimiter(relativePath)));
            return absolutePath;
        }

        /// <summary>
        /// MSBuild uses a Windows Style Delimiter for its relative path
        /// However in Other OSes (Linux/macOS) this is not correct perform
        /// a fixup prior to load.
        /// </summary>
        /// <param name="path">The path, usually relative, from MSBuild</param>
        /// <returns>The path with the correct DirectorySeparatorChar</returns>
        public static string FixUpPathDelimiter(string path)
        {
            path = path.Replace('\\', Path.DirectorySeparatorChar);
            return path;
        }
    }
}
