using ActioNator.Areas.User.Controllers;
using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Infrastructure.Attributes;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.Services.Interfaces.JournalService;
using ActioNator.ViewModels.Journal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace WebTests.Controllers
{
    [TestFixture]
    public class UserJournalControllerTests
    {
        private Mock<IJournalService> _journalServiceMock = null!;
        private Mock<IInputSanitizationService> _sanitizationMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;

        [SetUp]
        public void SetUp()
        {
            _journalServiceMock = new Mock<IJournalService>(MockBehavior.Strict);
            _sanitizationMock = new Mock<IInputSanitizationService>(MockBehavior.Strict);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        private JournalController CreateController(bool ajax = false)
        {
            var controller = new JournalController(_journalServiceMock.Object, _sanitizationMock.Object, _userManagerMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            if (ajax)
            {
                controller.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
            }

            var tempDataProvider = new Mock<ITempDataProvider>();
            controller.TempData = new TempDataDictionary(controller.HttpContext, tempDataProvider.Object);
            return controller;
        }

        [Test]
        public async Task Index_NoSearch_ReturnsView_WithPagedModel()
        {
            var entries = Enumerable.Range(1, 5)
                .Select(i => new JournalEntryViewModel { Id = Guid.NewGuid(), Title = $"T{i}", Content = "C", MoodTag = "Happy", CreatedAt = DateTime.UtcNow })
                .ToList();
            _journalServiceMock.Setup(s => s.GetAllEntriesAsync()).ReturnsAsync(entries);

            var controller = CreateController();
            var result = await controller.Index(null, page: 2, pageSize: 3) as ViewResult;

            Assert.IsNotNull(result);
            var vm = result!.Model as JournalEntriesListViewModel;
            Assert.IsNotNull(vm);
            Assert.AreEqual(2, vm!.Page);
            Assert.AreEqual(3, vm.PageSize);
            Assert.AreEqual(5, vm.TotalCount);
            Assert.AreEqual(2, vm.TotalPages);
            Assert.AreEqual(2, vm.Entries.Count); // items 4-5

            _journalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Index_WithSearch_UsesSanitizedTerm_AndSearchService()
        {
            _sanitizationMock.Setup(s => s.SanitizeString("  test  ")).Returns("test");
            var entries = new List<JournalEntryViewModel> {
                new JournalEntryViewModel { Id = Guid.NewGuid(), Title = "A", Content = "B", MoodTag = "Calm", CreatedAt = DateTime.UtcNow }
            };
            _journalServiceMock.Setup(s => s.SearchEntriesAsync("test")).ReturnsAsync(entries);

            var controller = CreateController();
            var result = await controller.Index("  test  ") as ViewResult;

            Assert.IsNotNull(result);
            var vm = result!.Model as JournalEntriesListViewModel;
            Assert.IsNotNull(vm);
            Assert.AreEqual("test", vm!.SearchTerm);
            Assert.AreEqual(1, vm.Entries.Count);

            _sanitizationMock.VerifyAll();
            _journalServiceMock.VerifyAll();
        }

        [Test]
        public async Task List_NoSearch_ReturnsJson_AllEntries()
        {
            var entries = new List<JournalEntryViewModel> { new JournalEntryViewModel { Id = Guid.NewGuid(), Title = "A" } };
            _journalServiceMock.Setup(s => s.GetAllEntriesAsync()).ReturnsAsync(entries);

            var controller = CreateController();
            var result = await controller.List(null) as JsonResult;

            Assert.IsNotNull(result);
            var data = result!.Value as IEnumerable<JournalEntryViewModel>;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data!.Count());

            _journalServiceMock.VerifyAll();
        }

        [Test]
        public async Task List_WithSearch_ReturnsJson_Filtered()
        {
            _sanitizationMock.Setup(s => s.SanitizeString("q")).Returns("q");
            var entries = new List<JournalEntryViewModel> { new JournalEntryViewModel { Id = Guid.NewGuid(), Title = "Q" } };
            _journalServiceMock.Setup(s => s.SearchEntriesAsync("q")).ReturnsAsync(entries);

            var controller = CreateController();
            var result = await controller.List("q") as JsonResult;
            Assert.IsNotNull(result);
            var data = result!.Value as IEnumerable<JournalEntryViewModel>;
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data!.Count());

            _sanitizationMock.VerifyAll();
            _journalServiceMock.VerifyAll();
        }

        [Test]
        public async Task GetJournalPartial_ReturnsPartial_WithVm()
        {
            var entries = Enumerable.Range(1, 4).Select(i => new JournalEntryViewModel { Id = Guid.NewGuid(), Title = $"T{i}" });
            _journalServiceMock.Setup(s => s.GetAllEntriesAsync()).ReturnsAsync(entries);

            var controller = CreateController();
            var result = await controller.GetJournalPartial(null, page: 1, pageSize: 3) as PartialViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("_JournalEntriesPartial", result!.ViewName);
            var vm = result.Model as JournalEntriesListViewModel;
            Assert.IsNotNull(vm);
            Assert.AreEqual(3, vm!.Entries.Count);
            Assert.AreEqual(2, vm.TotalPages);

            _journalServiceMock.VerifyAll();
        }

        [Test]
        public void Create_RedirectsToIndex()
        {
            var controller = CreateController();
            var result = controller.Create() as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result!.ActionName);
        }

        [Test]
        public async Task Edit_NotFound_WhenMissing()
        {
            _journalServiceMock.Setup(s => s.GetEntryByIdAsync(It.IsAny<Guid>())).ReturnsAsync((JournalEntryViewModel?)null);
            var controller = CreateController();
            var result = await controller.Edit(Guid.NewGuid());
            Assert.IsInstanceOf<NotFoundResult>(result);
            _journalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Edit_Found_RedirectsToIndex()
        {
            _journalServiceMock.Setup(s => s.GetEntryByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new JournalEntryViewModel { Id = Guid.NewGuid(), Title = "T" });
            var controller = CreateController();
            var result = await controller.Edit(Guid.NewGuid()) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result!.ActionName);
            _journalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Save_InvalidModel_ReturnsBadRequest_WithErrors()
        {
            var controller = CreateController(ajax: true);
            controller.ModelState.AddModelError("Title", "Required");
            var model = new JournalEntryViewModel();

            var result = await controller.Save(model) as BadRequestObjectResult;
            Assert.IsNotNull(result);
            // basic shape
            Assert.IsNotNull(result!.Value);
        }

        [Test]
        public async Task Save_Create_Ajax_ReturnsJson_SetsTempData()
        {
            var model = new JournalEntryViewModel { Id = Guid.Empty, Title = "T", Content = "<b>c</b>", MoodTag = "m" };
            _sanitizationMock.Setup(s => s.SanitizeString("T")).Returns("T");
            _sanitizationMock.Setup(s => s.SanitizeHtml("<b>c</b>")).Returns("c");
            _sanitizationMock.Setup(s => s.SanitizeString("m")).Returns("m");
            _journalServiceMock.Setup(s => s.CreateEntryAsync(model, It.IsAny<Guid?>())).Returns(Task.CompletedTask);

            var controller = CreateController(ajax: true);
            var result = await controller.Save(model) as JsonResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(controller.TempData.ContainsKey("SuccessMessage"));
            var dict = result!.Value as IDictionary<string, object>;
            // In ASP.NET Core, anonymous type becomes a reflection-based object; do a basic assertion
            Assert.IsNotNull(result.Value);

            _sanitizationMock.VerifyAll();
            _journalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Save_Create_NonAjax_Redirects_WithTempData()
        {
            var model = new JournalEntryViewModel { Id = Guid.Empty, Title = "T", Content = "c", MoodTag = "m" };
            _sanitizationMock.Setup(s => s.SanitizeString("T")).Returns("T");
            _sanitizationMock.Setup(s => s.SanitizeHtml("c")).Returns("c");
            _sanitizationMock.Setup(s => s.SanitizeString("m")).Returns("m");
            _journalServiceMock.Setup(s => s.CreateEntryAsync(model, It.IsAny<Guid?>())).Returns(Task.CompletedTask);

            var controller = CreateController(ajax: false);
            var result = await controller.Save(model) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result!.ActionName);
            Assert.IsTrue(controller.TempData.ContainsKey("SuccessMessage"));

            _sanitizationMock.VerifyAll();
            _journalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Save_Update_Ajax_ReturnsJson_SetsTempData()
        {
            var model = new JournalEntryViewModel { Id = Guid.NewGuid(), Title = "T", Content = "c", MoodTag = "m" };
            _sanitizationMock.Setup(s => s.SanitizeString("T")).Returns("T");
            _sanitizationMock.Setup(s => s.SanitizeHtml("c")).Returns("c");
            _sanitizationMock.Setup(s => s.SanitizeString("m")).Returns("m");
            _journalServiceMock.Setup(s => s.UpdateEntryAsync(model)).Returns(Task.CompletedTask);

            var controller = CreateController(ajax: true);
            var result = await controller.Save(model) as JsonResult;

            Assert.IsNotNull(result);
            Assert.IsTrue(controller.TempData.ContainsKey("SuccessMessage"));

            _sanitizationMock.VerifyAll();
            _journalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Save_Exception_Ajax_ReturnsBadRequest()
        {
            var model = new JournalEntryViewModel { Id = Guid.Empty, Title = "T", Content = "c", MoodTag = "m" };
            _sanitizationMock.Setup(s => s.SanitizeString("T")).Returns("T");
            _sanitizationMock.Setup(s => s.SanitizeHtml("c")).Returns("c");
            _sanitizationMock.Setup(s => s.SanitizeString("m")).Returns("m");
            _journalServiceMock.Setup(s => s.CreateEntryAsync(model, It.IsAny<Guid?>())).ThrowsAsync(new Exception("boom"));

            var controller = CreateController(ajax: true);
            var result = await controller.Save(model);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Save_Exception_NonAjax_ReturnsIndexView_WithModel()
        {
            var model = new JournalEntryViewModel { Id = Guid.Empty, Title = "T", Content = "c", MoodTag = "m" };
            _sanitizationMock.Setup(s => s.SanitizeString("T")).Returns("T");
            _sanitizationMock.Setup(s => s.SanitizeHtml("c")).Returns("c");
            _sanitizationMock.Setup(s => s.SanitizeString("m")).Returns("m");
            var fallback = new List<JournalEntryViewModel> { new JournalEntryViewModel { Id = Guid.NewGuid(), Title = "F" } };
            _journalServiceMock.Setup(s => s.CreateEntryAsync(model, It.IsAny<Guid?>())).ThrowsAsync(new Exception("boom"));
            _journalServiceMock.Setup(s => s.GetAllEntriesAsync()).ReturnsAsync(fallback);

            var controller = CreateController(ajax: false);
            var result = await controller.Save(model) as ViewResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result!.ViewName);
            Assert.IsNotNull(result.Model);

            _journalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Delete_NotFound_Ajax_ReturnsNotFoundJson()
        {
            _journalServiceMock.Setup(s => s.DeleteEntryAsync(It.IsAny<Guid>())).ReturnsAsync(false);
            var controller = CreateController(ajax: true);
            var dto = new DeleteJournalRequest { Id = Guid.NewGuid() };

            var result = await controller.Delete(dto);
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
            Assert.IsFalse(controller.TempData.ContainsKey("SuccessMessage"));
        }

        [Test]
        public async Task Delete_NotFound_NonAjax_Redirects_WithErrorTempData()
        {
            _journalServiceMock.Setup(s => s.DeleteEntryAsync(It.IsAny<Guid>())).ReturnsAsync(false);
            var controller = CreateController(ajax: false);
            var dto = new DeleteJournalRequest { Id = Guid.NewGuid() };

            var result = await controller.Delete(dto) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result!.ActionName);
            Assert.IsTrue(controller.TempData.ContainsKey("ErrorMessage"));
        }

        [Test]
        public async Task Delete_Success_Ajax_ReturnsJson_SetsTempData()
        {
            _journalServiceMock.Setup(s => s.DeleteEntryAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            var controller = CreateController(ajax: true);
            var dto = new DeleteJournalRequest { Id = Guid.NewGuid() };

            var result = await controller.Delete(dto) as JsonResult;
            Assert.IsNotNull(result);
            Assert.IsTrue(controller.TempData.ContainsKey("SuccessMessage"));
        }

        [Test]
        public async Task Delete_Success_NonAjax_Redirects_SetsTempData()
        {
            _journalServiceMock.Setup(s => s.DeleteEntryAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            var controller = CreateController(ajax: false);
            var dto = new DeleteJournalRequest { Id = Guid.NewGuid() };

            var result = await controller.Delete(dto) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result!.ActionName);
            Assert.IsTrue(controller.TempData.ContainsKey("SuccessMessage"));
        }

        [Test]
        public void Controller_HasAreaUser()
        {
            var t = typeof(JournalController);
            var area = t.GetCustomAttribute<AreaAttribute>();
            Assert.IsNotNull(area);
            Assert.AreEqual("User", area!.RouteValue);
        }

        [Test]
        public void Save_And_Delete_Have_Post_And_AntiForgeryJson()
        {
            var save = typeof(JournalController).GetMethod(nameof(JournalController.Save));
            var delete = typeof(JournalController).GetMethod(nameof(JournalController.Delete));
            Assert.IsNotNull(save);
            Assert.IsNotNull(delete);

            Assert.IsTrue(save!.GetCustomAttributes(typeof(HttpPostAttribute), false).Any());
            Assert.IsTrue(delete!.GetCustomAttributes(typeof(HttpPostAttribute), false).Any());
            Assert.IsTrue(save.GetCustomAttributes(typeof(ValidateAntiForgeryTokenFromJsonAttribute), false).Any());
            Assert.IsTrue(delete.GetCustomAttributes(typeof(ValidateAntiForgeryTokenFromJsonAttribute), false).Any());
        }
    }
}
