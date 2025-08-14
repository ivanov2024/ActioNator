using ActioNator.Areas.Admin.Controllers;
using ActioNator.Services.Interfaces.ReportVerificationService;
using ActioNator.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebTests.Controllers
{
    [TestFixture]
    public class ReportReviewControllerTests
    {
        private Mock<IReportReviewService> _serviceMock = null!;

        [SetUp]
        public void SetUp()
        {
            _serviceMock = new Mock<IReportReviewService>(MockBehavior.Strict);
        }

        private ReportReviewController CreateController()
            => new ReportReviewController(_serviceMock.Object);

        private static List<ReportedPostViewModel> MakeReportedPosts(int n)
            => Enumerable.Range(1, n).Select(i => new ReportedPostViewModel
            {
                PostId = Guid.NewGuid(),
                ContentPreview = $"Post {i}",
                AuthorUserName = $"author{i}",
                ReportReason = "spam",
                ReporterUserName = $"rep{i}",
                TotalReports = i,
                ReportedAt = DateTime.UtcNow
            }).ToList();

        private static List<ReportedCommentViewModel> MakeReportedComments(int n)
            => Enumerable.Range(1, n).Select(i => new ReportedCommentViewModel
            {
                CommentId = Guid.NewGuid(),
                ContentPreview = $"Comment {i}",
                AuthorUserName = $"author{i}",
                ReportReason = "abuse",
                ReporterUserName = $"rep{i}",
                TotalReports = i,
                ReportedAt = DateTime.UtcNow
            }).ToList();

        private static List<ReportedUserViewModel> MakeReportedUsers(int n)
            => Enumerable.Range(1, n).Select(i => new ReportedUserViewModel
            {
                UserId = Guid.NewGuid(),
                UserName = $"user{i}",
                ReportReason = "harassment",
                ReporterUserName = $"rep{i}",
                TotalReports = i,
                ReportedAt = DateTime.UtcNow
            }).ToList();

        [Test]
        public async Task Index_ReturnsView_WithAggregatedModel()
        {
            // Arrange
            var posts = MakeReportedPosts(2);
            var comments = MakeReportedComments(3);
            var users = MakeReportedUsers(1);

            _serviceMock.Setup(s => s.GetReportedPostsAsync()).ReturnsAsync(posts);
            _serviceMock.Setup(s => s.GetReportedCommentsAsync()).ReturnsAsync(comments);
            _serviceMock.Setup(s => s.GetReportedUsersAsync()).ReturnsAsync(users);

            var controller = CreateController();

            // Act
            var result = await controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var model = result!.Model as ReportReviewPageViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model!.ReportedPosts.Count);
            Assert.AreEqual(3, model.ReportedComments.Count);
            Assert.AreEqual(1, model.ReportedUsers.Count);
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DeletePost_ReturnsJson_SuccessTrue()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeletePostAsync(id)).ReturnsAsync(true);
            var controller = CreateController();

            var result = await controller.DeletePost(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DeletePost_ReturnsJson_SuccessFalse()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeletePostAsync(id)).ReturnsAsync(false);
            var controller = CreateController();

            var result = await controller.DeletePost(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsFalse(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DeleteComment_ReturnsJson_SuccessTrue()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteCommentAsync(id)).ReturnsAsync(true);
            var controller = CreateController();

            var result = await controller.DeleteComment(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DeleteComment_ReturnsJson_SuccessFalse()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteCommentAsync(id)).ReturnsAsync(false);
            var controller = CreateController();

            var result = await controller.DeleteComment(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsFalse(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DismissPostReport_ReturnsJson_SuccessTrue()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DismissPostReportAsync(id)).ReturnsAsync(true);
            var controller = CreateController();

            var result = await controller.DismissPostReport(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DismissPostReport_ReturnsJson_SuccessFalse()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DismissPostReportAsync(id)).ReturnsAsync(false);
            var controller = CreateController();

            var result = await controller.DismissPostReport(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsFalse(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DismissCommentReport_ReturnsJson_SuccessTrue()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DismissCommentReportAsync(id)).ReturnsAsync(true);
            var controller = CreateController();

            var result = await controller.DismissCommentReport(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DismissCommentReport_ReturnsJson_SuccessFalse()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DismissCommentReportAsync(id)).ReturnsAsync(false);
            var controller = CreateController();

            var result = await controller.DismissCommentReport(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsFalse(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DeleteUser_ReturnsJson_SuccessTrue()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteUserAsync(id)).ReturnsAsync(true);
            var controller = CreateController();

            var result = await controller.DeleteUser(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DeleteUser_ReturnsJson_SuccessFalse()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DeleteUserAsync(id)).ReturnsAsync(false);
            var controller = CreateController();

            var result = await controller.DeleteUser(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsFalse(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DismissUserReport_ReturnsJson_SuccessTrue()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DismissUserReportAsync(id)).ReturnsAsync(true);
            var controller = CreateController();

            var result = await controller.DismissUserReport(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task DismissUserReport_ReturnsJson_SuccessFalse()
        {
            var id = Guid.NewGuid();
            _serviceMock.Setup(s => s.DismissUserReportAsync(id)).ReturnsAsync(false);
            var controller = CreateController();

            var result = await controller.DismissUserReport(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsFalse(json.GetProperty("success").GetBoolean());
            _serviceMock.VerifyAll();
        }

        [Test]
        public async Task ViewPost_ReturnsPlaceholderJson_WithId()
        {
            var id = Guid.NewGuid();
            var controller = CreateController();

            var result = await controller.ViewPost(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.AreEqual(id, json.GetProperty("id").GetGuid());
            Assert.IsTrue(json.GetProperty("content").GetString()!.Contains("Full post content"));
        }

        [Test]
        public async Task ViewComment_ReturnsPlaceholderJson_WithId()
        {
            var id = Guid.NewGuid();
            var controller = CreateController();

            var result = await controller.ViewComment(id) as JsonResult;

            Assert.IsNotNull(result);
            var json = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.AreEqual(id, json.GetProperty("id").GetGuid());
            Assert.IsTrue(json.GetProperty("content").GetString()!.Contains("Full comment content"));
        }

        [Test]
        public void Controller_HasAdminArea_And_AuthorizeRoleAdmin()
        {
            var t = typeof(ReportReviewController);
            var area = t.GetCustomAttribute<AreaAttribute>();
            Assert.IsNotNull(area);
            Assert.AreEqual("Admin", area!.RouteValue);

            var authorize = t.GetCustomAttribute<AuthorizeAttribute>();
            Assert.IsNotNull(authorize);
            Assert.AreEqual("Admin", authorize!.Roles);
        }

        [Test]
        public void PostActions_Have_HttpPost_And_ValidateAntiForgeryToken()
        {
            var t = typeof(ReportReviewController);
            var postMethods = new[]
            {
                nameof(ReportReviewController.DeletePost),
                nameof(ReportReviewController.DeleteComment),
                nameof(ReportReviewController.DismissPostReport),
                nameof(ReportReviewController.DismissCommentReport),
                nameof(ReportReviewController.DeleteUser),
                nameof(ReportReviewController.DismissUserReport),
            };

            foreach (var name in postMethods)
            {
                var mi = t.GetMethod(name);
                Assert.IsNotNull(mi, $"Method {name} not found");
                Assert.IsTrue(mi!.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false).Any(), $"{name} missing HttpPost");
                Assert.IsTrue(mi.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), inherit: false).Any(), $"{name} missing ValidateAntiForgeryToken");
            }
        }
    }
}
