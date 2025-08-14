using System;
using System.Threading;
using System.Threading.Tasks;
using ActioNator.Data.Models;
using ActioNator.Services.Configuration;
using ActioNator.Services.Implementations.Cloud;
using ActioNator.Services.Interfaces.Cloud;
using ActioNator.Services.Interfaces.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    public class DropboxTokenProviderTests
    {
        private static DropboxTokenProvider CreateSut(
            DropboxOptions options,
            out Mock<IDropboxOAuthService> oauth,
            out Mock<ITokenProtector> protector,
            out Mock<UserManager<ApplicationUser>> userManager,
            ILogger<DropboxTokenProvider>? logger = null)
        {
            var env = Mock.Of<IWebHostEnvironment>();

            var optSnap = new Mock<IOptionsSnapshot<DropboxOptions>>();
            optSnap.Setup(o => o.Value).Returns(options);

            var store = new Mock<IUserStore<ApplicationUser>>();
            userManager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

            protector = new Mock<ITokenProtector>(MockBehavior.Strict);
            oauth = new Mock<IDropboxOAuthService>(MockBehavior.Strict);
            logger ??= Mock.Of<ILogger<DropboxTokenProvider>>();

            return new DropboxTokenProvider(env, optSnap.Object, userManager.Object, protector.Object, oauth.Object, logger);
        }

        [Test]
        public async Task GetAccessTokenAsync_UsesSharedRefreshToken_WhenConfigured()
        {
            // Arrange
            var options = new DropboxOptions { AppKey = "APPKEY", SharedRefreshToken = "REFRESH" };
            var sut = CreateSut(options, out var oauth, out var protector, out var userManager);
            protector.Setup(p => p.Protect(It.IsAny<string>())).Returns<string>(s => $"prot:{s}"); // may be used on rotation log path only
            oauth
                .Setup(o => o.RefreshAccessTokenAsync("APPKEY", "REFRESH", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IDropboxOAuthService.RefreshResult { AccessToken = "AT", RefreshToken = "REFRESH_ROT" });

            // Act
            var result = await sut.GetAccessTokenAsync(Guid.NewGuid());

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.AccessToken, Is.EqualTo("AT"));
            Assert.That(result.UsedSharedToken, Is.True);
            oauth.VerifyAll();
        }

        [Test]
        public async Task GetAccessTokenAsync_SharedRefreshToken_NoAppKey_ReturnsError()
        {
            // Arrange
            var options = new DropboxOptions { AppKey = null, SharedRefreshToken = "REFRESH" };
            var sut = CreateSut(options, out var oauth, out var protector, out var userManager);

            // Act
            var result = await sut.GetAccessTokenAsync(Guid.NewGuid());

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("AppKey is not configured"));
        }

        [Test]
        public async Task GetAccessTokenAsync_SharedAccessToken_Used_WhenNoSharedRefresh()
        {
            // Arrange
            var options = new DropboxOptions { SharedAccessToken = "SAT" };
            var sut = CreateSut(options, out var oauth, out var protector, out var userManager);

            // Act
            var result = await sut.GetAccessTokenAsync(Guid.NewGuid());

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.AccessToken, Is.EqualTo("SAT"));
            Assert.That(result.UsedSharedToken, Is.True);
        }

        [Test]
        public async Task GetAccessTokenAsync_PerUser_NoRefreshToken_RequiresConsent()
        {
            // Arrange
            var options = new DropboxOptions { AppKey = "APP" };
            var sut = CreateSut(options, out var oauth, out var protector, out var userManager);
            var userId = Guid.NewGuid();
            userManager
                .Setup(m => m.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new ApplicationUser { Id = userId, DropboxRefreshTokenEncrypted = null });

            // Act
            var result = await sut.GetAccessTokenAsync(userId);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.RequiresUserConsent, Is.True);
        }

        [Test]
        public async Task GetAccessTokenAsync_PerUser_WithRefreshToken_Refreshes_AndRotates()
        {
            // Arrange
            var options = new DropboxOptions { AppKey = "APP" };
            var sut = CreateSut(options, out var oauth, out var protector, out var userManager);
            var userId = Guid.NewGuid();
            var user = new ApplicationUser { Id = userId, DropboxRefreshTokenEncrypted = "enc" };
            userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            protector.Setup(p => p.Unprotect("enc")).Returns("plainRefresh");
            oauth
                .Setup(o => o.RefreshAccessTokenAsync("APP", "plainRefresh", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IDropboxOAuthService.RefreshResult { AccessToken = "AT2", RefreshToken = "NEWREF" });
            protector.Setup(p => p.Protect("NEWREF")).Returns("enc2");
            userManager.Setup(m => m.UpdateAsync(It.Is<ApplicationUser>(u => u.DropboxRefreshTokenEncrypted == "enc2")))
                       .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await sut.GetAccessTokenAsync(userId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.UsedSharedToken, Is.False);
            Assert.That(result.AccessToken, Is.EqualTo("AT2"));
            oauth.VerifyAll();
            protector.VerifyAll();
            userManager.VerifyAll();
        }

        [Test]
        public async Task GetAccessTokenAsync_PerUser_UserNotFound_ReturnsError()
        {
            // Arrange
            var options = new DropboxOptions { AppKey = "APP" };
            var sut = CreateSut(options, out var oauth, out var protector, out var userManager);
            var userId = Guid.NewGuid();
            userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await sut.GetAccessTokenAsync(userId);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("User not found"));
        }

        [Test]
        public async Task GetAccessTokenAsync_PerUser_AppKeyMissing_ReturnsError()
        {
            // Arrange
            var options = new DropboxOptions { AppKey = null };
            var sut = CreateSut(options, out var oauth, out var protector, out var userManager);
            var userId = Guid.NewGuid();
            userManager
                .Setup(m => m.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new ApplicationUser { Id = userId, DropboxRefreshTokenEncrypted = "x" });

            // Act
            var result = await sut.GetAccessTokenAsync(userId);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("AppKey is not configured"));
        }

        [Test]
        public async Task GetAccessTokenAsync_SharedRefresh_Failure_ReturnsError()
        {
            // Arrange
            var options = new DropboxOptions { AppKey = "APP", SharedRefreshToken = "REFRESH" };
            var sut = CreateSut(options, out var oauth, out var protector, out var userManager);
            oauth
                .Setup(o => o.RefreshAccessTokenAsync("APP", "REFRESH", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));

            // Act
            var result = await sut.GetAccessTokenAsync(Guid.NewGuid());

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("Failed to obtain Dropbox access token"));
        }
    }
}
