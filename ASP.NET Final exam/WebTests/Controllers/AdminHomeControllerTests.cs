using ActioNator.Areas.Admin.Controllers;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.ReportVerificationService;
using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace WebTests.Controllers
{
    [TestFixture]
    public class AdminHomeControllerTests
    {
        private Mock<ICoachVerificationService> _coachServiceMock = null!;
        private Mock<IReportReviewService> _reportServiceMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;

        [SetUp]
        public void SetUp()
        {
            _coachServiceMock = new Mock<ICoachVerificationService>(MockBehavior.Strict);
            _reportServiceMock = new Mock<IReportReviewService>(MockBehavior.Strict);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        private HomeController CreateController()
            => new HomeController(_userManagerMock.Object, _coachServiceMock.Object, _reportServiceMock.Object);

        [Test]
        public async Task Index_ReturnsView_WithCorrectCounts()
        {
            // Arrange
            _coachServiceMock.Setup(s => s.GetPendingVerificationsCountAsync()).ReturnsAsync(7);
            _reportServiceMock.Setup(s => s.GetPendingPostReportsCountAsync()).ReturnsAsync(5);
            _reportServiceMock.Setup(s => s.GetPendingCommentReportsCountAsync()).ReturnsAsync(3);

            var controller = CreateController();

            // Act
            var result = await controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result!.Model as AdminDashboardCountsViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(7, model!.PendingCoachVerifications);
            Assert.AreEqual(5, model.PendingPostReports);
            Assert.AreEqual(3, model.PendingCommentReports);
            Assert.AreEqual(0, model.PendingUserReports);

            _coachServiceMock.VerifyAll();
            _reportServiceMock.VerifyAll();
        }

        [Test]
        public void Controller_HasAreaAdmin_AndAuthorize()
        {
            var t = typeof(HomeController);
            var area = t.GetCustomAttribute<AreaAttribute>();
            Assert.IsNotNull(area);
            Assert.AreEqual("Admin", area!.RouteValue);

            var authorizeAttrs = t.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false);
            Assert.IsTrue(authorizeAttrs != null && authorizeAttrs.Length > 0);
        }

        [Test]
        public void Index_Has_HttpGetAttribute()
        {
            var mi = typeof(HomeController).GetMethod(nameof(HomeController.Index));
            Assert.IsNotNull(mi);
            Assert.IsTrue(mi!.GetCustomAttributes(typeof(HttpGetAttribute), false).Any());
        }
    }
}
