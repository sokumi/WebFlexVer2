using Microsoft.AspNetCore.Mvc;

namespace WebFlex.UI.Controllers.Test;

[Route("test/[action]")]
public class TestController : Controller {
    [HttpGet, ActionName("tst1000")]
    public IActionResult Login() {
        return View(MVCPath.Test.TST1000);
    }


    [HttpGet, ActionName("summary")]
    public IActionResult Summary() {
        return Json(new {
            success = true,
            message = "조회되었습니다.",
            data = new {
                deviceCount = 12,
                tagCount = 1280,
                goodCount = 1248,
                badCount = 32
            }
        });
    }

    [HttpGet, ActionName("grid")]
    public IActionResult Grid() {
        var rows = Enumerable.Range(1, 30)
            .Select(i => new {
                id = $"TAG{i:0000}",
                groupName = i % 2 == 0 ? "PRESS" : "WELDING",
                tagName = $"Sensor_{i:000}",
                value = Math.Round(20 + Random.Shared.NextDouble() * 80, 2),
                status = i % 7 == 0 ? 1 : 0,
                updatedAt = DateTime.Now.AddSeconds(-i)
            })
            .ToList();

        return Json(new {
            success = true,
            message = "조회되었습니다.",
            data = rows
        });
    }

    [HttpPost, ActionName("save")]
    public IActionResult Save([FromBody] TestSaveRequest request) {
        if (string.IsNullOrWhiteSpace(request.Name)) {
            return Json(new {
                success = false,
                message = "이름을 입력해 주세요."
            });
        }

        return Json(new {
            success = true,
            message = $"{request.Name} 저장 테스트가 완료되었습니다.",
            data = request
        });
    }
}

public class TestSaveRequest {
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
}