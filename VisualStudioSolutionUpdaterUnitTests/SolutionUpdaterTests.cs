// -----------------------------------------------------------------------
// <copyright file="SolutionUpdaterTests.cs" company="Ace Olszowka">
//  Copyright (c) Ace Olszowka 2019-2020. All rights reserved.
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

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class SolutionUpdaterTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Update_ModifiesProject(bool filterConditionalReferences)
        {
            // Give it a file that would be updated
            string targetSolution = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "SimpleDependency", "FromPerspective_A_Unpopulated.sln");
            bool actual = SolutionUpdater.Update(targetSolution, filterConditionalReferences, false);

            Assert.That(actual, Is.EqualTo(true), "The solution should have been updated");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Update_ShouldNotModifyProject(bool filterConditionalReferences)
        {
            // Give it a file that would NOT be updated
            string targetSolution = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "SimpleDependency", "AllProjects.sln");
            bool actual = SolutionUpdater.Update(targetSolution, filterConditionalReferences, false);

            Assert.That(actual, Is.EqualTo(false), "The solution should NOT have been updated");
        }

        [TestCaseSource(typeof(GetNewDependenciesInSolution_ValidArguments_Tests))]
        public void GetNewDependenciesInSolution_ValidArguments(SolutionFile solution, bool filterConditionalReferences, string[] expected)
        {
            string[] actual = SolutionUpdater.GetNewDependenciesInSolution(solution, filterConditionalReferences);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCaseSource(typeof(_InsertNewProjectsInternal_ValidArguments_Tests))]
        public void _InsertNewProjectsInternal_ValidArguments(string targetSolution, IEnumerable<string> newReferences, string expectedSolution)
        {
            // First load up the expected solution into a file
            string[] expected = File.ReadAllLines(expectedSolution);

            // Arrange the call
            SolutionFile solution = SolutionFile.Parse(targetSolution);
            string[] actual = SolutionUpdater._InsertNewProjectsInternal(targetSolution, solution, newReferences);

            // Assert; note we have to use EquivalentTo here because the ordering is not defined
            Assert.That(actual, Is.EquivalentTo(expected));
        }
    }

    internal class GetNewDependenciesInSolution_ValidArguments_Tests : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return new TestCaseData
                (
                    SolutionFile.Parse(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "SimpleDependency", "AllProjects.sln")),
                    false,
                    new string[0]
                ).SetArgDisplayNames("FilterFalse-AllProjects.sln");
            yield return new TestCaseData
                (
                    SolutionFile.Parse(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "SimpleDependency", "AllProjects.sln")),
                    true,
                    new string[0]
                ).SetArgDisplayNames("FilterTrue-AllProjects.sln");
            yield return new TestCaseData
                (
                    SolutionFile.Parse(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "SimpleDependency", "FromPerspective_A_Unpopulated.sln")),
                    false,
                    new string[]
                    {
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","SimpleDependency","B","B.csproj"),
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","SimpleDependency","C","C.csproj"),
                    }
                ).SetArgDisplayNames("FilterFalse-FromPerspective_A_Unpopulated.sln");
            yield return new TestCaseData
                (
                    SolutionFile.Parse(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "SimpleDependency", "FromPerspective_A_Unpopulated.sln")),
                    true,
                    new string[]
                    {
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","SimpleDependency","B","B.csproj"),
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","SimpleDependency","C","C.csproj"),
                    }
                ).SetArgDisplayNames("FilterTrue-FromPerspective_A_Unpopulated.sln");
            yield return new TestCaseData
                (
                    SolutionFile.Parse(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "ConditionalReferenceFiltering", "FromPerspective_Primary_Unpopulated.sln")),
                    false,
                    new string[]
                    {
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","ConditionalReferenceFiltering","A","A.csproj"),
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","ConditionalReferenceFiltering","B","B.csproj"),
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","ConditionalReferenceFiltering","C","C.csproj"),
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","ConditionalReferenceFiltering","D","D.csproj"),
                    }
                    ).SetArgDisplayNames("FilterFalse-FromPerspective_Primary_Unpopulated.sln");
            yield return new TestCaseData
                (
                    SolutionFile.Parse(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "ConditionalReferenceFiltering", "FromPerspective_Primary_Unpopulated.sln")),
                    true,
                    new string[]
                    {
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","ConditionalReferenceFiltering","B","B.csproj"),
                    }
                    ).SetArgDisplayNames("FilterTrue-FromPerspective_Primary_Unpopulated.sln");
        }
    }

    internal class _InsertNewProjectsInternal_ValidArguments_Tests : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return new TestCaseData
                (
                    Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "SimpleDependency", "FromPerspective_A_Unpopulated.sln"),
                    new string[]
                    {
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","SimpleDependency","B","B.csproj"),
                        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects","SimpleDependency","C","C.csproj"),
                    },
                    Path.Combine(TestContext.CurrentContext.TestDirectory, "TestProjects", "SimpleDependency", "FromPerspective_A.sln")
                ).SetArgDisplayNames("FromPerspective_A_Unpopulated.sln");
        }
    }
}
