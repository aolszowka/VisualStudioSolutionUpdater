namespace VisualStudioSolutionUpdaterUnitTests
{
    using System.IO;
    using NUnit.Framework;
    using VisualStudioSolutionUpdater;

    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class SolutionUpdaterTests
    {
        [Test]
        public void Update_ModifiesProject()
        {
            // Give it a file that would be updated
            string targetSolution = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestProjects\SimpleDependency\FromPerspective_A_Unpopulated.sln");
            bool actual = SolutionUpdater.Update(targetSolution, false);

            Assert.That(actual, Is.EqualTo(true), "The solution should have been updated");
        }

        [Test]
        public void Update_ShouldNotModifyProject()
        {
            // Give it a file that would NOT be updated
            string targetSolution = Path.Combine(TestContext.CurrentContext.TestDirectory, @"TestProjects\SimpleDependency\AllProjects.sln");
            bool actual = SolutionUpdater.Update(targetSolution, false);

            Assert.That(actual, Is.EqualTo(false), "The solution should NOT have been updated");
        }
    }
}
