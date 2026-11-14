using Microsoft.AspNetCore.Mvc;
namespace HRMS.Web.Controllers;
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
[IgnoreAntiforgeryToken]
public class HomeController : Controller
{
    public IActionResult Error()
    {
        Response.StatusCode=500;
        ViewData["Message"]="Ứng dụng không thể hoàn thành yêu cầu. Hãy kiểm tra SQL Server đang chạy, tên máy chủ và database trong cấu hình hiện tại; xem nhật ký để biết chi tiết.";
        return View("Message");
    }
    public IActionResult Status(int code)
    {
        Response.StatusCode=code;
        ViewData["Message"]=code switch {404=>"Không tìm thấy hồ sơ hoặc trang được yêu cầu.",400=>"Yêu cầu không hợp lệ hoặc phiên biểu mẫu đã hết hiệu lực. Hãy mở lại trang và thử lại.",413=>"Yêu cầu tải lên quá lớn. Mỗi ảnh tối đa 2 MB.",_=>$"Không thể thực hiện yêu cầu (mã {code})."};
        return View("Message");
    }
}
