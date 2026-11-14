using System.ComponentModel.DataAnnotations;
namespace HRMS.Web.ViewModels;
public class LoginViewModel
{
    [Required(ErrorMessage="Vui lòng nhập tên tài khoản."), StringLength(100, ErrorMessage="Tên tài khoản tối đa 100 ký tự.")]
    [Display(Name="Tên tài khoản")] public string Username { get; set; } = "";
    [Required(ErrorMessage="Vui lòng nhập mật khẩu."), StringLength(256, ErrorMessage="Mật khẩu tối đa 256 ký tự.")]
    [DataType(DataType.Password), Display(Name="Mật khẩu")] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}
