using ActioNator.Services.Implementations.Communication;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    [TestFixture]
    public class NullSignalRServiceTests
    {
        [Test]
        public async Task Methods_CompleteWithoutError_AndLogDebug()
        {
            var logger = new Mock<ILogger<NullSignalRService>>();
            var svc = new NullSignalRService(logger.Object);

            // Calls should not throw
            await svc.SendToAllAsync("MethodA", 1, "two");
            await svc.SendToGroupAsync("Group1", "MethodB", new { X = 1 });
            await svc.SendToUserAsync("User42", "MethodC", 3.14);

            // Verify logging was called at least once
            logger.Verify(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.AtLeastOnce);
        }
    }
}
