using ActioNator.Areas.Admin.Controllers;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.FileServices;
using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.ViewModels.CoachVerification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;

namespace WebTests.Controllers
{
    [TestFixture]
    public class AdminCoachVerificationControllerTests
    {
        private Mock<ICoachVerificationService> _coachServiceMock = null!;
        private Mock<IFileStorageService> _fileServiceMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;

        [SetUp]
        public void SetUp()
        {
            _coachServiceMock = new Mock<ICoachVerificationService>(MockBehavior.Strict);
            _fileServiceMock = new Mock<IFileStorageService>(MockBehavior.Strict);
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        private CoachVerificationController CreateController(bool ajax = false, IHeaderDictionary? headers = null)
        {
            var controller = new CoachVerificationController(_coachServiceMock.Object, _fileServiceMock.Object, _userManagerMock.Object);
            var httpContext = new DefaultHttpContext();
            if (headers != null)
            {
                foreach (var kv in headers)
                {
                    httpContext.Request.Headers[kv.Key] = kv.Value;
                }
            }
            if (ajax)
            {
                httpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
            }

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            return controller;
        }

        [Test]
        public async Task Index_ReturnsView_WithVerificationRequests()
        {
            var list = new List<CoachVerificationUserViewModel> { new CoachVerificationUserViewModel { UserId = "u1" } };
            _coachServiceMock.Setup(s => s.GetAllVerificationRequestsAsync()).ReturnsAsync(list);

            var controller = CreateController();
            var result = await controller.Index() as ViewResult;

            Assert.IsNotNull(result);
            var model = result!.Model as List<CoachVerificationUserViewModel>;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model!.Count);
            _coachServiceMock.VerifyAll();
        }

        [Test]
        public async Task UserVerificationPartial_EmptyUserId_ReturnsBadRequest()
        {
            var controller = CreateController(ajax: true);
            var result = await controller.UserVerificationPartial("") as BadRequestObjectResult;
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task UserVerificationPartial_NonAjax_RedirectsToIndex()
        {
            var controller = CreateController(ajax: false);
            var result = await controller.UserVerificationPartial("user1") as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result!.ActionName);
        }

        [Test]
        public async Task UserVerificationPartial_Ajax_ReturnsPartial_WithModel()
        {
            var docs = new List<CoachDocumentViewModel> { new CoachDocumentViewModel { RelativePath = "p", FileName = "p", FileType = "image/png" } };
            _coachServiceMock.Setup(s => s.GetDocumentsForUserAsync("user1")).ReturnsAsync(docs);

            var controller = CreateController(ajax: true);
            var result = await controller.UserVerificationPartial("user1") as PartialViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("_UserVerificationPartial", result!.ViewName);
            var model = result.Model as CoachVerificationUserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("user1", model!.UserId);
            Assert.AreEqual(1, model.Documents.Count);
            _coachServiceMock.VerifyAll();
        }

        [Test]
        public async Task ViewDocument_InvalidParams_ReturnsBadRequest()
        {
            var controller = CreateController();
            Assert.IsInstanceOf<BadRequestObjectResult>(await controller.ViewDocument("", "path"));
            Assert.IsInstanceOf<BadRequestObjectResult>(await controller.ViewDocument("u", ""));
            Assert.IsInstanceOf<BadRequestObjectResult>(await controller.ViewDocument("u", "../etc"));
        }

        [Test]
        public async Task ViewDocument_ReturnsInlineFile_WhenDownloadFalse()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            _fileServiceMock.Setup(s => s.GetFileAsync("files/doc.png", "u1", It.IsAny<CancellationToken>())).ReturnsAsync((stream, "image/png"));
            var controller = CreateController();

            var result = await controller.ViewDocument("u1", "files/doc.png", download: false) as FileStreamResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("image/png", result!.ContentType);
            Assert.IsTrue(string.IsNullOrEmpty(result.FileDownloadName));
            _fileServiceMock.VerifyAll();
        }

        [Test]
        public async Task ViewDocument_ReturnsAttachment_WhenDownloadTrue()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            _fileServiceMock.Setup(s => s.GetFileAsync("files/doc.pdf", "u1", It.IsAny<CancellationToken>())).ReturnsAsync((stream, "application/pdf"));
            var controller = CreateController();

            var result = await controller.ViewDocument("u1", "files/doc.pdf", download: true) as FileStreamResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("application/pdf", result!.ContentType);
            Assert.AreEqual("doc.pdf", result.FileDownloadName);
            _fileServiceMock.VerifyAll();
        }

        [Test]
        public async Task ViewDocument_OnException_ReturnsNotFound()
        {
            _fileServiceMock.Setup(s => s.GetFileAsync("p", "u1", It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));
            var controller = CreateController();
            var result = await controller.ViewDocument("u1", "p");
            Assert.IsInstanceOf<NotFoundResult>(result);
            _fileServiceMock.VerifyAll();
        }

        [Test]
        public async Task ApproveVerification_EmptyUserId_SetsTempDataError_AndRedirects()
        {
            var controller = CreateController();
            var result = await controller.ApproveVerification("") as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result!.ActionName);
            Assert.IsTrue(controller.TempData.ContainsKey("Error"));
        }

        [Test]
        public async Task ApproveVerification_UserNotFound_SetsError_AndRedirects()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync((ApplicationUser?)null);
            var controller = CreateController();
            var result = await controller.ApproveVerification("u1") as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result!.ActionName);
            Assert.IsTrue(controller.TempData.ContainsKey("Error"));
        }

        [Test]
        public async Task ApproveVerification_RemoveRolesFails_EarlyError_NoServiceCall()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid() };
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User", "Member" });
            _userManagerMock.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "remove failed" }));

            var controller = CreateController();
            var result = await controller.ApproveVerification("u1") as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(controller.TempData.ContainsKey("Error"));
            _coachServiceMock.Verify(s => s.ApproveVerificationAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task ApproveVerification_Success_AddCoachAndFlagUpdated_SetsSuccess()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid() };
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _userManagerMock.Setup(m => m.RemoveFromRolesAsync(user, It.Is<IEnumerable<string>>(r => r.Contains("User"))))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.IsInRoleAsync(user, It.IsAny<string>())).ReturnsAsync(false);
            _userManagerMock.Setup(m => m.AddToRoleAsync(user, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            _coachServiceMock.Setup(s => s.ApproveVerificationAsync("u1")).ReturnsAsync(true);

            var controller = CreateController();
            var result = await controller.ApproveVerification("u1") as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(controller.TempData.ContainsKey("Success"));
            _coachServiceMock.VerifyAll();
        }

        [Test]
        public async Task ApproveVerification_ServiceFails_SetsError()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid() };
            _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { });
            _userManagerMock.Setup(m => m.IsInRoleAsync(user, It.IsAny<string>())).ReturnsAsync(true);
            _coachServiceMock.Setup(s => s.ApproveVerificationAsync("u1")).ReturnsAsync(false);

            var controller = CreateController();
            var result = await controller.ApproveVerification("u1") as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(controller.TempData.ContainsKey("Error"));
            _coachServiceMock.VerifyAll();
        }

        [Test]
        public async Task RejectVerification_EmptyUserId_SetsError_AndRedirects()
        {
            var controller = CreateController();
            var result = await controller.RejectVerification("") as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.IsTrue(controller.TempData.ContainsKey("Error"));
        }

        [Test]
        public async Task RejectVerification_SetsTempData_ByServiceResult()
        {
            _coachServiceMock.Setup(s => s.RejectVerificationAsync("u1", It.IsAny<string>())).ReturnsAsync(true);
            var controller = CreateController();
            var ok = await controller.RejectVerification("u1");
            Assert.IsInstanceOf<RedirectToActionResult>(ok);
            Assert.IsTrue(controller.TempData.ContainsKey("Success"));

            // failure path
            _coachServiceMock.Reset();
            _coachServiceMock.Setup(s => s.RejectVerificationAsync("u2", It.IsAny<string>())).ReturnsAsync(false);
            controller = CreateController();
            var fail = await controller.RejectVerification("u2");
            Assert.IsInstanceOf<RedirectToActionResult>(fail);
            Assert.IsTrue(controller.TempData.ContainsKey("Error"));
        }

        [Test]
        public void Controller_HasAreaAdmin_AndAuthorizeRoleAdmin()
        {
            var t = typeof(CoachVerificationController);
            var area = t.GetCustomAttribute<AreaAttribute>();
            Assert.IsNotNull(area);
            Assert.AreEqual("Admin", area!.RouteValue);

            var authorize = t.GetCustomAttribute<AuthorizeAttribute>();
            Assert.IsNotNull(authorize);
            Assert.AreEqual("Admin", authorize!.Roles);
        }

        [Test]
        public void PostActions_Have_ValidateAntiForgeryToken()
        {
            var t = typeof(CoachVerificationController);
            var postMethods = new[]
            {
                nameof(CoachVerificationController.ApproveVerification),
                nameof(CoachVerificationController.RejectVerification)
            };
            foreach (var name in postMethods)
            {
                var mi = t.GetMethod(name);
                Assert.IsNotNull(mi);
                Assert.IsTrue(mi!.GetCustomAttributes(typeof(HttpPostAttribute), false).Any());
                Assert.IsTrue(mi.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).Any());
            }
        }

        [Test]
        public void GetActions_Have_HttpGetAttribute()
        {
            var t = typeof(CoachVerificationController);
            var getMethods = new[]
            {
                nameof(CoachVerificationController.UserVerificationPartial),
                nameof(CoachVerificationController.ViewDocument)
            };
            foreach (var name in getMethods)
            {
                var mi = t.GetMethod(name);
                Assert.IsNotNull(mi);
                Assert.IsTrue(mi!.GetCustomAttributes(typeof(HttpGetAttribute), false).Any());
            }
        }
    }
}
