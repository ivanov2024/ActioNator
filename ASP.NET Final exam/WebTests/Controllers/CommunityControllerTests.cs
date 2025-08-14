using ActioNator.Areas.User.Controllers;
using ActioNator.Data.Models;
using ActioNator.Hubs;
using ActioNator.Services.Interfaces.Community;
using ActioNator.ViewModels.Community;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebTests.Controllers
{
    [TestFixture]
    public class CommunityControllerTests
    {
        private Mock<ICommunityService> _service = null!;
        private Mock<IHubContext<CommunityHub>> _hubContext = null!;
        private Mock<IHubClients> _hubClients = null!;
        private Mock<IClientProxy> _clientProxy = null!;
        private Mock<ILogger<CommunityController>> _logger = null!;
        private Mock<UserManager<ApplicationUser>> _userManager = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new Mock<ICommunityService>(MockBehavior.Strict);
            _hubContext = new Mock<IHubContext<CommunityHub>>(MockBehavior.Loose);
            _hubClients = new Mock<IHubClients>(MockBehavior.Loose);
            _clientProxy = new Mock<IClientProxy>(MockBehavior.Loose);
            _logger = new Mock<ILogger<CommunityController>>();

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            _hubContext.SetupGet(h => h.Clients).Returns(_hubClients.Object);
            _hubClients.SetupGet(c => c.All).Returns(_clientProxy.Object);
        }

        private CommunityController CreateController(bool authenticated = true, Guid? userId = null)
        {
            var controller = new CommunityController(
                _service.Object,
                _hubContext.Object,
                _logger.Object,
                _userManager.Object);

            var http = new DefaultHttpContext();
            if (authenticated)
            {
                userId ??= Guid.NewGuid();
                var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) };
                http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            }
            else
            {
                http.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            controller.ControllerContext = new ControllerContext { HttpContext = http };
            return controller;
        }

        // ToggleLikeComment
        [Test]
        public async Task ToggleLikeComment_EmptyId_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.ToggleLikeComment(new CommentLikeRequest { CommentId = Guid.Empty });
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task ToggleLikeComment_Success_ReturnsLikesAndBroadcasts()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.ToggleLikeCommentAsync(commentId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(3);

            _clientProxy
                .Setup(c => c.SendCoreAsync(
                    "ReceiveCommentUpdate",
                    It.Is<object[]>(arr => (Guid)arr[0] == commentId && (int)arr[1] == 3),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await controller.ToggleLikeComment(new CommentLikeRequest { CommentId = commentId }) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(el.GetProperty("success").GetBoolean());
            Assert.AreEqual(3, el.GetProperty("likesCount").GetInt32());

            _service.VerifyAll();
            _clientProxy.VerifyAll();
        }

        // ToggleLike (Post)
        [Test]
        public async Task ToggleLike_EmptyId_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.ToggleLike(new PostLikeRequest { PostId = Guid.Empty });
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task ToggleLike_Unauthenticated_ReturnsUnauthorized()
        {
            var controller = CreateController(authenticated: false);
            var result = await controller.ToggleLike(new PostLikeRequest { PostId = Guid.NewGuid() });
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task ToggleLike_Success_ReturnsLikesAndBroadcasts()
        {
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.ToggleLikePostAsync(postId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(5);

            _clientProxy
                .Setup(c => c.SendCoreAsync(
                    "ReceivePostUpdate",
                    It.Is<object[]>(arr => (Guid)arr[0] == postId && (int)arr[1] == 5),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await controller.ToggleLike(new PostLikeRequest { PostId = postId }) as JsonResult;

            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(el.GetProperty("success").GetBoolean());
            Assert.AreEqual(5, el.GetProperty("likesCount").GetInt32());

            _service.VerifyAll();
            _clientProxy.VerifyAll();
        }

        // AddComment
        [Test]
        public async Task AddComment_InvalidPayload_ReturnsBadRequest()
        {
            var controller = CreateController();
            var bad1 = await controller.AddComment(new CommentRequest { PostId = Guid.Empty, Content = "hi" });
            var bad2 = await controller.AddComment(new CommentRequest { PostId = Guid.NewGuid(), Content = "  " });
            Assert.IsInstanceOf<BadRequestObjectResult>(bad1);
            Assert.IsInstanceOf<BadRequestObjectResult>(bad2);
        }

        [Test]
        public async Task AddComment_Unauthenticated_ReturnsUnauthorized()
        {
            var controller = CreateController(authenticated: false);
            var result = await controller.AddComment(new CommentRequest { PostId = Guid.NewGuid(), Content = "hey" });
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task AddComment_Success_ReturnsJsonWithComment()
        {
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            var commentVm = new PostCommentViewModel
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                Content = "hello",
                LikesCount = 0,
                IsLikedByCurrentUser = false
            };

            _service
                .Setup(s => s.AddCommentAsync(postId, "hello", userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(commentVm);

            var result = await controller.AddComment(new CommentRequest { PostId = postId, Content = "hello" }) as JsonResult;

            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(el.GetProperty("success").GetBoolean());
            Assert.AreEqual(commentVm.Id, el.GetProperty("comment").GetProperty("Id").GetGuid());

            _service.VerifyAll();
        }

        // GetComment
        [Test]
        public async Task GetComment_EmptyId_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.GetComment(Guid.Empty);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task GetComment_Unauthenticated_ReturnsUnauthorized()
        {
            var controller = CreateController(authenticated: false);
            var result = await controller.GetComment(Guid.NewGuid());
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task GetComment_NotFound_ReturnsNotFound()
        {
            var controller = CreateController();
            var id = Guid.NewGuid();
            _service.Setup(s => s.GetCommentByIdAsync(id, It.IsAny<Guid>())).ReturnsAsync((PostCommentViewModel)null!);

            var result = await controller.GetComment(id);
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
        }

        [Test]
        public async Task GetComment_Success_ReturnsPartialView_WithModel()
        {
            var controller = CreateController();
            var id = Guid.NewGuid();
            var vm = new PostCommentViewModel { Id = id, PostId = Guid.NewGuid(), Content = "c" };
            _service.Setup(s => s.GetCommentByIdAsync(id, It.IsAny<Guid>())).ReturnsAsync(vm);

            var result = await controller.GetComment(id) as PartialViewResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("~/Views/Shared/_CommentItemPartial.cshtml", result!.ViewName);
            Assert.AreSame(vm, result.Model);
        }

        // CreatePost
        [Test]
        public async Task CreatePost_EmptyContentAndNoImages_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.CreatePost("   ", images: new List<IFormFile>());
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task CreatePost_OversizedImage_ReturnsBadRequest()
        {
            var controller = CreateController();
            var bigImage = new Mock<IFormFile>();
            bigImage.SetupGet(f => f.Length).Returns(6 * 1024 * 1024); // 6MB
            bigImage.SetupGet(f => f.ContentType).Returns("image/jpeg");

            var result = await controller.CreatePost("post", new List<IFormFile> { bigImage.Object });
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task CreatePost_InvalidImageType_ReturnsBadRequest()
        {
            var controller = CreateController();
            var img = new Mock<IFormFile>();
            img.SetupGet(f => f.Length).Returns(100);
            img.SetupGet(f => f.ContentType).Returns("application/pdf");

            var result = await controller.CreatePost("post", new List<IFormFile> { img.Object });
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task CreatePost_Success_ReturnsJsonWithPostId()
        {
            var userId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);
            var vm = new PostCardViewModel { Id = Guid.NewGuid(), Content = "post" };

            _service
                .Setup(s => s.CreatePostAsync("post", userId, It.IsAny<CancellationToken>(), It.IsAny<List<IFormFile>>()))
                .ReturnsAsync(vm);

            var result = await controller.CreatePost("post", new List<IFormFile>()) as JsonResult;
            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(el.GetProperty("success").GetBoolean());
            Assert.AreEqual(vm.Id, el.GetProperty("postId").GetGuid());
            _service.VerifyAll();
        }

        [Test]
        public async Task CreatePost_OnArgumentException_ReturnsBadRequest()
        {
            var userId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.CreatePostAsync("p", userId, It.IsAny<CancellationToken>(), It.IsAny<List<IFormFile>>()))
                .ThrowsAsync(new ArgumentException("bad"));

            var result = await controller.CreatePost("p", new List<IFormFile>()) as BadRequestObjectResult;
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task CreatePost_OnException_ReturnsServerError()
        {
            var userId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.CreatePostAsync("p", userId, It.IsAny<CancellationToken>(), It.IsAny<List<IFormFile>>()))
                .ThrowsAsync(new Exception("boom"));

            var result = await controller.CreatePost("p", new List<IFormFile>()) as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(500, result!.StatusCode);
        }

        // DeletePost
        [Test]
        public async Task DeletePost_EmptyId_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.DeletePost(Guid.Empty);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task DeletePost_Unauthenticated_ReturnsUnauthorized()
        {
            var controller = CreateController(authenticated: false);
            var result = await controller.DeletePost(Guid.NewGuid());
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task DeletePost_Success_ReturnsJsonAndBroadcasts()
        {
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.DeletePostAsync(postId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _clientProxy
                .Setup(c => c.SendCoreAsync(
                    "ReceivePostDeletion",
                    It.Is<object[]>(arr => (Guid)arr[0] == postId),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await controller.DeletePost(postId) as JsonResult;
            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(el.GetProperty("success").GetBoolean());

            _service.VerifyAll();
            _clientProxy.VerifyAll();
        }

        [Test]
        public async Task DeletePost_ServiceFalse_ReturnsJsonFalse_NoBroadcast()
        {
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.DeletePostAsync(postId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await controller.DeletePost(postId) as JsonResult;
            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsFalse(el.GetProperty("success").GetBoolean());

            _clientProxy.Verify(c => c.SendCoreAsync(
                "ReceivePostDeletion",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Never);

            _service.VerifyAll();
        }

        [Test]
        public async Task DeletePost_OnException_ReturnsServerError()
        {
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.DeletePostAsync(postId, userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));

            var result = await controller.DeletePost(postId) as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(500, result!.StatusCode);
        }

        // DeleteComment
        [Test]
        public async Task DeleteComment_EmptyCommentId_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.DeleteComment(Guid.Empty, new CommentDeleteRequest { PostId = Guid.NewGuid() });
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task DeleteComment_EmptyPostId_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.DeleteComment(Guid.NewGuid(), new CommentDeleteRequest { PostId = Guid.Empty });
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task DeleteComment_Unauthenticated_ReturnsUnauthorized()
        {
            var controller = CreateController(authenticated: false);
            var result = await controller.DeleteComment(Guid.NewGuid(), new CommentDeleteRequest { PostId = Guid.NewGuid() });
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task DeleteComment_Success_ReturnsJsonAndBroadcasts()
        {
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.DeleteCommentAsync(commentId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _clientProxy
                .Setup(c => c.SendCoreAsync(
                    "ReceiveCommentDeletion",
                    It.Is<object[]>(arr => (Guid)arr[0] == commentId && (Guid)arr[1] == postId),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await controller.DeleteComment(commentId, new CommentDeleteRequest { PostId = postId }) as JsonResult;
            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(el.GetProperty("success").GetBoolean());

            _service.VerifyAll();
            _clientProxy.VerifyAll();
        }

        [Test]
        public async Task DeleteComment_ServiceFalse_ReturnsJsonFalse_NoBroadcast()
        {
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.DeleteCommentAsync(commentId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await controller.DeleteComment(commentId, new CommentDeleteRequest { PostId = postId }) as JsonResult;
            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsFalse(el.GetProperty("success").GetBoolean());

            _clientProxy.Verify(c => c.SendCoreAsync(
                "ReceiveCommentDeletion",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()), Times.Never);

            _service.VerifyAll();
        }

        [Test]
        public async Task DeleteComment_OnException_ReturnsServerError()
        {
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.DeleteCommentAsync(commentId, userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));

            var result = await controller.DeleteComment(commentId, new CommentDeleteRequest { PostId = postId }) as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(500, result!.StatusCode);
        }

        // ReportPost
        [Test]
        public async Task ReportPost_EmptyId_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.ReportPost(Guid.Empty, "reason");
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task ReportPost_DefaultReasonUsed_WhenEmpty()
        {
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.ReportPostAsync(
                    postId,
                    It.Is<string>(r => r == "Inappropriate content"),
                    userId,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>()))
                .ReturnsAsync(true);

            var result = await controller.ReportPost(postId, "");
            Assert.IsInstanceOf<JsonResult>(result);
            _service.VerifyAll();
        }

        [Test]
        public async Task ReportPost_Success_ReturnsJsonTrue()
        {
            var userId = Guid.NewGuid();
            var postId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.ReportPostAsync(postId, It.IsAny<string>(), userId, It.IsAny<CancellationToken>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var result = await controller.ReportPost(postId, "spam") as JsonResult;
            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(el.GetProperty("success").GetBoolean());
            _service.VerifyAll();
        }

        // ReportComment
        [Test]
        public async Task ReportComment_EmptyId_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.ReportComment(Guid.Empty, new CommentReportRequest { Reason = "r" });
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task ReportComment_DefaultReasonUsed_WhenNull()
        {
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.ReportCommentAsync(
                    commentId,
                    It.Is<string>(r => r == "Inappropriate content"),
                    userId,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>()))
                .ReturnsAsync(true);

            var result = await controller.ReportComment(commentId, new CommentReportRequest { Reason = null });
            Assert.IsInstanceOf<JsonResult>(result);
            _service.VerifyAll();
        }

        [Test]
        public async Task ReportComment_Success_ReturnsJsonTrue()
        {
            var userId = Guid.NewGuid();
            var commentId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: userId);

            _service
                .Setup(s => s.ReportCommentAsync(commentId, It.IsAny<string>(), userId, It.IsAny<CancellationToken>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var result = await controller.ReportComment(commentId, new CommentReportRequest { Reason = "spam" }) as JsonResult;
            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(el.GetProperty("success").GetBoolean());
            _service.VerifyAll();
        }

        // ReportUser
        [Test]
        public async Task ReportUser_EmptyId_ReturnsBadRequest()
        {
            var controller = CreateController(authenticated: true);
            var result = await controller.ReportUser(Guid.Empty, new UserReportRequest { Reason = "r" });
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task ReportUser_Unauthenticated_ReturnsUnauthorized()
        {
            var controller = CreateController(authenticated: false);
            var result = await controller.ReportUser(Guid.NewGuid(), new UserReportRequest { Reason = "r" });
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task ReportUser_DefaultReasonUsed_WhenEmpty()
        {
            var currentUserId = Guid.NewGuid();
            var reportedUserId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: currentUserId);

            _service
                .Setup(s => s.ReportUserAsync(
                    reportedUserId,
                    It.Is<string>(r => r == "Inappropriate content"),
                    currentUserId,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>()))
                .ReturnsAsync(true);

            var result = await controller.ReportUser(reportedUserId, new UserReportRequest { Reason = "" });
            Assert.IsInstanceOf<JsonResult>(result);
            _service.VerifyAll();
        }

        [Test]
        public async Task ReportUser_ServiceFalse_ReturnsJsonWithMessage()
        {
            var currentUserId = Guid.NewGuid();
            var reportedUserId = Guid.NewGuid();
            var controller = CreateController(authenticated: true, userId: currentUserId);

            _service
                .Setup(s => s.ReportUserAsync(reportedUserId, It.IsAny<string>(), currentUserId, It.IsAny<CancellationToken>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var result = await controller.ReportUser(reportedUserId, new UserReportRequest { Reason = "r" }) as JsonResult;
            Assert.IsNotNull(result);
            var el = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsFalse(el.GetProperty("success").GetBoolean());
            Assert.AreEqual("You have already reported this user or you are not allowed to report this user.", el.GetProperty("message").GetString());
            _service.VerifyAll();
        }
    }
}
