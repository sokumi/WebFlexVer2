using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Common;

public abstract class WebFlexController : Controller {
    protected IActionResult Success(string message, object? data = null) {
        return Json(new {
            success = true,
            message,
            data
        });
    }

    protected IActionResult ErrorData(string message, object? data = null) {
        return Json(new {
            success = false,
            message,
            data
        });
    }

    protected static string GetErrorMessage(Exception ex) {
        return ex.InnerException?.Message ?? ex.Message;
    }
}

public abstract class WebFlexApiController : ControllerBase {
    protected IActionResult Success(string message, object? data = null) {
        return Ok(new {
            success = true,
            message,
            data
        });
    }

    protected IActionResult ErrorData(string message, object? data = null) {
        return Ok(new {
            success = false,
            message,
            data
        });
    }

    protected static string GetErrorMessage(Exception ex) {
        return ex.InnerException?.Message ?? ex.Message;
    }
}