using Microsoft.AspNetCore.Identity;
namespace HRMS.Web.Security;
// No employee fields or SQL tables participate in authentication.
public sealed class DemoAccountService(IConfiguration config)
{
    private readonly PasswordHasher<string> hasher = new();
    public string Username => config["DemoAccount:Username"] ?? "";
    public string Hash => config["DemoAccount:PasswordHash"] ?? "";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Hash);
    public bool Verify(string username, string password)
    {
        if (!IsConfigured) return false;
        try
        {
            var result = hasher.VerifyHashedPassword(Username, Hash, password);
            return string.Equals(username, Username, StringComparison.Ordinal) && result != PasswordVerificationResult.Failed;
        }
        catch (FormatException) { return false; }
        catch (ArgumentException) { return false; }
    }
}
