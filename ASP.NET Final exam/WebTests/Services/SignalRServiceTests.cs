using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using ActioNator.Hubs;
using ActioNator.Services.Implementations.Communication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    public class SignalRServiceTests
    {
        [Test]
        public async Task SendToAllAsync_Calls_Client_All_SendAsync()
        {
            var mockClients = new Mock<IHubClients>();
            var mockAll = new Mock<IClientProxy>();
            mockClients.Setup(c => c.All).Returns(mockAll.Object);

            var mockHub = new Mock<IHubContext<CommunityHub>>();
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

            var logger = Mock.Of<ILogger<SignalRService>>();
            var service = new SignalRService(mockHub.Object, logger);

            await service.SendToAllAsync("Notify", "x", 1);

            mockAll.Verify(c => c.SendCoreAsync(
                It.Is<string>(m => m == "Notify"),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()), Times.Once);

            var inv = mockAll.Invocations.Single(i => i.Method.Name == nameof(IClientProxy.SendCoreAsync));
            var outer = (object?[])inv.Arguments[1]!;
            Assert.That(outer.Length, Is.EqualTo(1));
            var inner = (object?[])outer[0]!;
            Assert.That(inner.Length, Is.EqualTo(2));
            Assert.That((string)inner[0]!, Is.EqualTo("x"));
            Assert.That((int)inner[1]!, Is.EqualTo(1));
        }

        [Test]
        public async Task SendToGroupAsync_Calls_Group_SendAsync_WhenContextAvailable()
        {
            var mockClients = new Mock<IHubClients>();
            var mockGroupProxy = new Mock<IClientProxy>();
            mockClients.Setup(c => c.Group("g1")).Returns(mockGroupProxy.Object);

            var mockHub = new Mock<IHubContext<CommunityHub>>();
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

            var logger = Mock.Of<ILogger<SignalRService>>();
            var service = new SignalRService(mockHub.Object, logger);

            await service.SendToGroupAsync("g1", "Notify", 42);

            mockGroupProxy.Verify(c => c.SendCoreAsync(
                It.Is<string>(m => m == "Notify"),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()), Times.Once);

            var invG = mockGroupProxy.Invocations.Single(i => i.Method.Name == nameof(IClientProxy.SendCoreAsync));
            var outerG = (object?[])invG.Arguments[1]!;
            Assert.That(outerG.Length, Is.EqualTo(1));
            var innerG = (object?[])outerG[0]!;
            Assert.That(innerG.Length, Is.EqualTo(1));
            Assert.That((int)innerG[0]!, Is.EqualTo(42));
        }

        [Test]
        public async Task SendToUserAsync_Calls_User_SendAsync_WhenContextAvailable()
        {
            var mockClients = new Mock<IHubClients>();
            var mockUserProxy = new Mock<IClientProxy>();
            mockClients.Setup(c => c.User("u1")).Returns(mockUserProxy.Object);

            var mockHub = new Mock<IHubContext<CommunityHub>>();
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

            var logger = Mock.Of<ILogger<SignalRService>>();
            var service = new SignalRService(mockHub.Object, logger);

            await service.SendToUserAsync("u1", "Notify", "hello");

            mockUserProxy.Verify(c => c.SendCoreAsync(
                It.Is<string>(m => m == "Notify"),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()), Times.Once);

            var invU = mockUserProxy.Invocations.Single(i => i.Method.Name == nameof(IClientProxy.SendCoreAsync));
            var outerU = (object?[])invU.Arguments[1]!;
            Assert.That(outerU.Length, Is.EqualTo(1));
            var innerU = (object?[])outerU[0]!;
            Assert.That(innerU.Length, Is.EqualTo(1));
            Assert.That((string)innerU[0]!, Is.EqualTo("hello"));
        }

        [Test]
        public void SendToAllAsync_Throws_On_Empty_MethodName()
        {
            var logger = Mock.Of<ILogger<SignalRService>>();
            var service = new SignalRService(null, logger);

            Assert.ThrowsAsync<ArgumentException>(async () => await service.SendToAllAsync(" "));
        }

        [Test]
        public void SendToAllAsync_DoesNotThrow_WhenHubContextNull()
        {
            var logger = Mock.Of<ILogger<SignalRService>>();
            var service = new SignalRService(null, logger);
            Assert.DoesNotThrowAsync(async () => await service.SendToAllAsync("Notify", 1));
        }
    }
}
