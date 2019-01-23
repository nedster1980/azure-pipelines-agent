using System;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Moq;
using Xunit;
using ITestRunPublisher = Agent.Plugins.Log.TestResultParser.Contracts.ITestRunPublisher;

namespace Test.L0.Plugin.TestResultParser
{
    class TestRunManagerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestRunManager_PublishTestRun()
        {
            var logger = new Mock<ITraceLogger>();
            var publisher = new Mock<ITestRunPublisher>();
            var runManager = new TestRunManager(publisher.Object, logger.Object);
            var fakeRun = new TestRun("mocha/1", 1)
            {
                TestRunSummary = new TestRunSummary()
                {
                    TotalPassed = 5,
                    TotalSkipped = 1,
                    TotalFailed = 1,
                    TotalExecutionTime = TimeSpan.FromMinutes(1),
                    TotalTests = 7
                }
            };

            publisher.Setup(x => x.PublishAsync(It.IsAny<TestRun>())).Returns(Task.CompletedTask);

            await runManager.PublishAsync(fakeRun);

            publisher.Verify(x => x.PublishAsync(It.IsAny<TestRun>()));
        }
    }
}
