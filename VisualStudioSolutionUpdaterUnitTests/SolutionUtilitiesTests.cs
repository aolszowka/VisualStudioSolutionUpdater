// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2019. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace VisualStudioSolutionUpdaterUnitTests
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Build.Construction;
    using NUnit.Framework;
    using VisualStudioSolutionUpdater;

    /// <summary>
    /// Unit Tests for the <see cref="SolutionUtilities"/> class.
    /// </summary>
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class SolutionUtilitiesTests
    {
        [TestCaseSource(typeof(GetProjectsFromSolution_ValidArguments_Tests))]
        public void GetProjectsFromSolution_ValidArguments(SolutionFile solution, IEnumerable<string> expected)
        {
            IEnumerable<string> actual = SolutionUtilities.GetProjectsFromSolution(solution);

            Assert.That(actual, Is.EquivalentTo(expected));
        }

        [TestCaseSource(typeof(TryGetDepedenciesFolderGuid_ValidArguments_Tests))]
        public void TryGetDepedenciesFolderGuid_ValidArguments(SolutionFile solution, string expected)
        {
            string actual;
            SolutionUtilities.TryGetDepedenciesFolderGuid(solution, out actual);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    internal class GetProjectsFromSolution_ValidArguments_Tests : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return new TestCaseData
                (
                    SolutionFile.Parse(Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestProjects\SimpleDependency\AllProjects.sln")),
                    new string[]
                    {
                        Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestProjects\SimpleDependency\A\A.csproj"),
                        Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestProjects\SimpleDependency\B\B.csproj"),
                        Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestProjects\SimpleDependency\C\C.csproj"),
                    }
                ).SetArgDisplayNames("AllProjects.sln");
            yield return new TestCaseData
                (
                    SolutionFile.Parse(Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestProjects\SimpleDependency\FromPerspective_A_Unpopulated.sln")),
                    new string[]
                    {
                        Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestProjects\SimpleDependency\A\A.csproj"),
                    }
                ).SetArgDisplayNames("FromPerspective_A_Unpopulated.sln");
        }
    }

    internal class TryGetDepedenciesFolderGuid_ValidArguments_Tests : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return new TestCaseData
                (
                    SolutionFile.Parse(Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestProjects\SimpleDependency\AllProjects.sln")),
                    "{DA34CE5D-031A-4C97-8DE8-A81F98C0288A}"
                ).SetArgDisplayNames("AllProjects.sln");
        }
    }
}
