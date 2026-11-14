using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Identity;
namespace HRMS.Web.Security;
public static class DemoAccountSetup
{
    public static int Run()
    {
        if (Console.IsInputRedirected) { Console.Error.WriteLine("Hãy chạy tiện ích trong Terminal tương tác để nhập mật khẩu có che ký tự."); return 1; }
        Console.Write("Tên tài khoản: "); var username = Console.ReadLine() ?? "";
        Console.Write("Mật khẩu: "); var password = ReadPassword();
        Console.Write("Nhập lại mật khẩu: "); var confirmation = ReadPassword();
        if (string.IsNullOrWhiteSpace(username) || username.Length > 100 || string.IsNullOrWhiteSpace(password) || password.Length > 256 || password != confirmation)
        { Console.Error.WriteLine("Thông tin trống, quá dài hoặc mật khẩu xác nhận không khớp. Chưa lưu cấu hình."); return 1; }
        var basePath = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets");
        var directory = Path.Combine(basePath, "hrms-longc-local-demo-6b189036");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "secrets.json");
        var json = File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path))!.AsObject() : new JsonObject();
        json.Remove("DemoAccount");
        json["DemoAccount:Username"] = username;
        json["DemoAccount:PasswordHash"] = new PasswordHasher<string>().HashPassword(username, password);
        var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
        File.WriteAllText(temporary, json.ToJsonString());
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(temporary, path, true);
        Console.WriteLine("Đã lưu tên tài khoản và password hash trong User Secrets. Không hiển thị mật khẩu/hash.");
        return 0;
    }
    private static string ReadPassword()
    {
        var text = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return text.ToString(); }
            if (key.Key == ConsoleKey.Backspace) { if (text.Length > 0) { text.Length--; Console.Write("\b \b"); } }
            else if (!char.IsControl(key.KeyChar) && text.Length < 257) { text.Append(key.KeyChar); Console.Write("*"); }
        }
    }
}
