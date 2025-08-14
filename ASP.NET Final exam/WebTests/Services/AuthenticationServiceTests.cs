using System;
using System.Threading;
using System.Threading.Tasks;
using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.GCommon;
using ActioNator.Services.Implementations.AuthenticationService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    public class AuthenticationServiceTests
    {
        private static AuthenticationService CreateSut(
            out Mock<UserManager<ApplicationUser>> userManager,
            out Mock<IUserEmailStore<ApplicationUser>> emailStore,
            ActioNatorDbContext? dbContext = null)
        {
            emailStore = new Mock<IUserEmailStore<ApplicationUser>>();

            userManager = new Mock<UserManager<ApplicationUser>>(
                emailStore.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

            // Ensure email support so GetEmailStore() works
            userManager.SetupGet(m => m.SupportsUserEmail).Returns(true);

            var signInManager = (SignInManager<ApplicationUser>)null!; // not used in these tests
            var logger = Mock.Of<ILogger<AuthenticationService>>();

            dbContext ??= new TestInMemoryActioNatorDbContext(Guid.NewGuid().ToString());

            return new AuthenticationService(
                userManager.Object,
                signInManager,
                emailStore.Object,
                logger,
                dbContext);
        }

        [Test]
        public void ValidateAndSanitizeReturnUrl_ReturnsDefault_ForNullEmptyOrExternal()
        {
            var sut = CreateSut(out var um, out var store);
            var def = "/home";

            Assert.That(sut.ValidateAndSanitizeReturnUrl(null!, def), Is.EqualTo(def));
            Assert.That(sut.ValidateAndSanitizeReturnUrl(string.Empty, def), Is.EqualTo(def));
            Assert.That(sut.ValidateAndSanitizeReturnUrl("https://evil.com", def), Is.EqualTo(def));
            Assert.That(sut.ValidateAndSanitizeReturnUrl("http://localhost:5001/page", def), Is.EqualTo(def));
        }

        [Test]
        public void ValidateAndSanitizeReturnUrl_ReturnsUrl_ForLocalPaths()
        {
            var sut = CreateSut(out var um, out var store);
            var def = "/home";

            Assert.That(sut.ValidateAndSanitizeReturnUrl("/dashboard", def), Is.EqualTo("/dashboard"));
            Assert.That(sut.ValidateAndSanitizeReturnUrl("~/profile", def), Is.EqualTo("~/profile"));
        }

        [Test]
        public async Task GetRoleBasedRedirectPathAsync_Admin_TakesPrecedence()
        {
            var sut = CreateSut(out var um, out var store);
            var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "a@b" };

            um.Setup(m => m.IsInRoleAsync(user, RoleConstants.Admin)).ReturnsAsync(true);
            um.Setup(m => m.IsInRoleAsync(user, RoleConstants.Coach)).ReturnsAsync(false);

            var path = await sut.GetRoleBasedRedirectPathAsync(user);
            Assert.That(path, Is.EqualTo(RedirectPathConstants.AdminHome));
        }

        [Test]
        public async Task GetRoleBasedRedirectPathAsync_Coach_WhenNotAdmin()
        {
            var sut = CreateSut(out var um, out var store);
            var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "c@d" };

            um.Setup(m => m.IsInRoleAsync(user, RoleConstants.Admin)).ReturnsAsync(false);
            um.Setup(m => m.IsInRoleAsync(user, RoleConstants.Coach)).ReturnsAsync(true);

            var path = await sut.GetRoleBasedRedirectPathAsync(user);
            Assert.That(path, Is.EqualTo(RedirectPathConstants.CoachHome));
        }

        [Test]
        public async Task GetRoleBasedRedirectPathAsync_Defaults_To_UserHome()
        {
            var sut = CreateSut(out var um, out var store);
            var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "u@u" };

            um.Setup(m => m.IsInRoleAsync(user, RoleConstants.Admin)).ReturnsAsync(false);
            um.Setup(m => m.IsInRoleAsync(user, RoleConstants.Coach)).ReturnsAsync(false);

            var path = await sut.GetRoleBasedRedirectPathAsync(user);
            Assert.That(path, Is.EqualTo(RedirectPathConstants.UserHome));
        }
    }
}
