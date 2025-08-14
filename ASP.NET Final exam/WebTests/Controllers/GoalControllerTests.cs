using ActioNator.Areas.User.Controllers;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.GoalService;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.ViewModels.Goal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace WebTests.Controllers
{
    [TestFixture]
    public class GoalControllerTests
    {
        private Mock<IGoalService> _goalServiceMock = null!;
        private Mock<IInputSanitizationService> _sanitizationMock = null!;
        private Mock<ILogger<GoalController>> _loggerMock = null!;
        private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;

        [SetUp]
        public void SetUp()
        {
            _goalServiceMock = new Mock<IGoalService>(MockBehavior.Strict);
            _sanitizationMock = new Mock<IInputSanitizationService>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<GoalController>>();

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null);
        }

        private GoalController CreateController(bool authenticated = true, Guid? userId = null)
        {
            var controller = new GoalController(
                _goalServiceMock.Object,
                _sanitizationMock.Object,
                _loggerMock.Object,
                _userManagerMock.Object);

            var httpContext = new DefaultHttpContext();
            if (authenticated)
            {
                userId ??= Guid.NewGuid();
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
                };
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            }
            else
            {
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        private static List<GoalViewModel> MakeGoals(int count)
            => Enumerable.Range(1, count).Select(i => new GoalViewModel
            {
                Id = Guid.NewGuid(),
                Title = $"Title {i}",
                Description = $"Desc {i}",
                Completed = false,
                DueDate = DateTime.UtcNow.AddDays(i)
            }).ToList();

        [Test]
        public async Task Index_ReturnsView_WithPagedGoals()
        {
            // Arrange
            var allGoals = MakeGoals(5);
            _goalServiceMock
                .Setup(s => s.GetUserGoalsAsync(It.IsAny<Guid?>(), "all", It.IsAny<CancellationToken>()))
                .ReturnsAsync(allGoals);

            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.Index(filter: "all", page: 1, pageSize: 3) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var vm = result!.Model as GoalsListViewModel;
            Assert.IsNotNull(vm);
            Assert.AreEqual(3, vm!.Goals.Count());
            Assert.AreEqual(5, vm.TotalCount);
            Assert.AreEqual(2, vm.TotalPages);

            _goalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Index_OnException_ReturnsEmptyViewModel()
        {
            // Arrange
            _goalServiceMock
                .Setup(s => s.GetUserGoalsAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));

            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.Index() as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            var vm = result!.Model as GoalsListViewModel;
            Assert.IsNotNull(vm);
            Assert.AreEqual(0, vm!.TotalCount);

            _goalServiceMock.VerifyAll();
        }

        [Test]
        public async Task GetGoals_ReturnsGoals_AsJson()
        {
            // Arrange
            var goals = MakeGoals(2);
            _goalServiceMock
                .Setup(s => s.GetUserGoalsAsync(It.IsAny<Guid?>(), "all", It.IsAny<CancellationToken>()))
                .ReturnsAsync(goals);

            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.GetGoals("all") as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            var value = result!.Value as IEnumerable<GoalViewModel>;
            Assert.IsNotNull(value);
            Assert.AreEqual(2, value!.Count());

            _goalServiceMock.VerifyAll();
        }

        [Test]
        public async Task GetGoals_OnException_ReturnsEmptyList_AsJson()
        {
            // Arrange
            _goalServiceMock
                .Setup(s => s.GetUserGoalsAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));

            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.GetGoals("all") as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            var value = result!.Value as IEnumerable<GoalViewModel>;
            Assert.IsNotNull(value);
            Assert.AreEqual(0, value!.Count());

            _goalServiceMock.VerifyAll();
        }

        [Test]
        public async Task GetGoalPartial_ReturnsPartialView_WithPagedGoals()
        {
            // Arrange
            var goals = MakeGoals(4);
            _goalServiceMock
                .Setup(s => s.GetUserGoalsAsync(It.IsAny<Guid?>(), "all", It.IsAny<CancellationToken>()))
                .ReturnsAsync(goals);

            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.GetGoalPartial("all", page: 2, pageSize: 2) as PartialViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("_GoalsPartial", result!.ViewName);
            var vm = result!.Model as GoalsListViewModel;
            Assert.IsNotNull(vm);
            Assert.AreEqual(2, vm!.Goals.Count());
            Assert.AreEqual(2, vm.Page);
            Assert.AreEqual(2, vm.TotalPages);

            _goalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Create_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController(authenticated: true);
            controller.ModelState.AddModelError("Title", "Required");

            // Act
            var result = await controller.Create(new GoalViewModel(), CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Create_Valid_SanitizesAndCreates_ReturnsSuccessJson()
        {
            // Arrange
            var input = new GoalViewModel { Title = "<b>t</b>", Description = "<i>d</i>" };
            var sanitizedTitle = "t";
            var sanitizedDesc = "d";

            _sanitizationMock.Setup(s => s.SanitizeHtml("<b>t</b>")).Returns(sanitizedTitle);
            _sanitizationMock.Setup(s => s.SanitizeHtml("<i>d</i>")).Returns(sanitizedDesc);

            var created = new GoalViewModel { Id = Guid.NewGuid(), Title = sanitizedTitle, Description = sanitizedDesc };
            _goalServiceMock
                .Setup(s => s.CreateGoalAsync(It.Is<GoalViewModel>(m => m.Title == sanitizedTitle && m.Description == sanitizedDesc), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(created);

            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.Create(input, CancellationToken.None) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            var element = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(element.GetProperty("success").GetBoolean());
            Assert.AreEqual(created.Id, element.GetProperty("goal").GetProperty("Id").GetGuid());

            _sanitizationMock.VerifyAll();
            _goalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Update_UnauthorizedAccess_Returns403()
        {
            // Arrange
            var model = new GoalViewModel { Id = Guid.NewGuid(), Title = "t" };
            _sanitizationMock.Setup(s => s.SanitizeHtml(It.IsAny<string>())).Returns<string>(s => s);
            _goalServiceMock
                .Setup(s => s.VerifyGoalOwnershipAsync(model.Id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _goalServiceMock
                .Setup(s => s.UpdateGoalAsync(It.IsAny<GoalViewModel>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("forbidden"));

            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.Update(model, CancellationToken.None) as ObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(403, result!.StatusCode);

            _sanitizationMock.VerifyAll();
            _goalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Delete_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.Delete(null!, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Delete_Unauthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var controller = CreateController(authenticated: false);

            // Act
            var result = await controller.Delete(new DeleteGoalRequest { Id = Guid.NewGuid() }, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
        }

        [Test]
        public async Task Delete_NotOwner_Returns403()
        {
            // Arrange
            var id = Guid.NewGuid();
            _goalServiceMock
                .Setup(s => s.VerifyGoalOwnershipAsync(id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.Delete(new DeleteGoalRequest { Id = id }, CancellationToken.None) as ObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(403, result!.StatusCode);

            _goalServiceMock.VerifyAll();
        }

        [Test]
        public async Task Delete_Success_ReturnsJsonSuccess()
        {
            // Arrange
            var id = Guid.NewGuid();
            _goalServiceMock
                .Setup(s => s.VerifyGoalOwnershipAsync(id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _goalServiceMock
                .Setup(s => s.DeleteGoalAsync(id, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.Delete(new DeleteGoalRequest { Id = id }, CancellationToken.None) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            var element = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(element.GetProperty("success").GetBoolean());

            _goalServiceMock.VerifyAll();
        }

        [Test]
        public async Task ToggleComplete_EmptyId_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.ToggleComplete(Guid.Empty, CancellationToken.None);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task ToggleComplete_NotOwner_Returns403()
        {
            // Arrange
            var id = Guid.NewGuid();
            _goalServiceMock
                .Setup(s => s.VerifyGoalOwnershipAsync(id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.ToggleComplete(id, CancellationToken.None) as ObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(403, result!.StatusCode);

            _goalServiceMock.VerifyAll();
        }

        [Test]
        public async Task ToggleComplete_Success_ReturnsUpdatedGoal()
        {
            // Arrange
            var id = Guid.NewGuid();
            var updated = new GoalViewModel { Id = id, Title = "t", Completed = true };
            _goalServiceMock
                .Setup(s => s.VerifyGoalOwnershipAsync(id, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _goalServiceMock
                .Setup(s => s.ToggleGoalCompletionAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updated);

            var controller = CreateController(authenticated: true);

            // Act
            var result = await controller.ToggleComplete(id, CancellationToken.None) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            var element = JsonSerializer.SerializeToElement(result!.Value!);
            Assert.IsTrue(element.GetProperty("success").GetBoolean());
            Assert.AreEqual(id, element.GetProperty("goal").GetProperty("Id").GetGuid());

            _goalServiceMock.VerifyAll();
        }
    }
}
