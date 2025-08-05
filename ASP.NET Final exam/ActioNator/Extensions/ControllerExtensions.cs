using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Text;

namespace ActioNator.Extensions
{
    public static class ControllerExtensions
    {
        /// <summary>
        /// Renders a view component to string for AJAX responses
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <param name="componentName">Name of the view component to render</param>
        /// <param name="arguments">Arguments to pass to the view component</param>
        /// <returns>HTML string of the rendered view component</returns>
        public static async Task<string> RenderViewComponentToStringAsync(this Controller controller, string componentName, object arguments)
        {
            // Get the ViewComponentResult from the component
            var helper = controller.HttpContext.RequestServices.GetService(typeof(IViewComponentHelper)) as IViewComponentHelper;
            if (helper == null)
            {
                throw new InvalidOperationException("IViewComponentHelper service not found");
            }

            // Initialize the helper with the controller's ViewContext
            (helper as IViewContextAware)?.Contextualize(new ViewContext
            {
                HttpContext = controller.HttpContext,
                RouteData = controller.RouteData,
                ViewData = controller.ViewData,
                TempData = controller.TempData
            });

            // Invoke the view component and get its result
            var result = await helper.InvokeAsync(componentName, arguments);

            // Render the result to string
            using var writer = new StringWriter();
            result.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
            return writer.ToString();
        }

        /// <summary>
        /// Renders a partial view to string for AJAX responses
        /// </summary>
        /// <param name="controller">The controller instance</param>
        /// <param name="viewName">Name of the partial view to render</param>
        /// <param name="model">Model to pass to the view</param>
        /// <returns>HTML string of the rendered partial view</returns>
        public static async Task<string> RenderPartialViewToStringAsync(this Controller controller, string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;
            }

            controller.ViewData.Model = model;

            using var writer = new StringWriter();
            var viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
            
            if (viewEngine == null)
            {
                throw new InvalidOperationException("ICompositeViewEngine service not found");
            }

            var viewResult = viewEngine.FindView(controller.ControllerContext, viewName, false);

            if (viewResult.View == null)
            {
                throw new ArgumentNullException($"{viewName} does not match any available view");
            }

            var viewContext = new ViewContext(
                controller.ControllerContext,
                viewResult.View,
                controller.ViewData,
                controller.TempData,
                writer,
                new HtmlHelperOptions()
            );

            await viewResult.View.RenderAsync(viewContext);
            return writer.GetStringBuilder().ToString();
        }
    }
}
