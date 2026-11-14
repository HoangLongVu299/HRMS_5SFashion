using System.Globalization;
using System.Text;
using ReasonableRTF;
using ReasonableRTF.Enums;

namespace HRMS.Web.Services;

/// <summary>
/// Reads legacy NOIDUNG RTF alongside newer plain text. Never returns HTML;
/// Razor must continue to encode the result. Reading does not update the database.
/// </summary>
public static class ContractContentText
{
    public const string InvalidContentMessage =
        "Không đọc được định dạng nội dung hợp đồng. Dữ liệu gốc vẫn được giữ nguyên; cần kiểm tra trước khi chỉnh sửa.";

    public static bool TryRead(string? stored, out string? text)
    {
        text = stored;
        if (stored is null) return true;
        var rtf = stored.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (!rtf.StartsWith(@"{\rtf", StringComparison.OrdinalIgnoreCase)) return true;

        // NOIDUNG is nvarchar(max), whereas an RTF parser consumes bytes.
        // Express any literal Unicode as RTF escapes instead of losing Vietnamese
        // characters through ASCII replacement or choosing the wrong ANSI code page.
        var ascii = new StringBuilder(rtf.Length);
        foreach (var c in rtf)
        {
            if (c < 128) ascii.Append(c);
            else ascii.Append(@"{\uc1\u")
                .Append(unchecked((short)c).ToString(CultureInfo.InvariantCulture)).Append("?}");
        }

        // Converter instances contain parser state; do not share one across requests.
        var result = new RtfToTextConverter().Convert(Encoding.ASCII.GetBytes(ascii.ToString()));
        if (result.Error != RtfError.OK)
        {
            text = null;
            return false;
        }
        text = result.Text.ReplaceLineEndings("\n").TrimEnd('\n');
        return true;
    }

    public static string? ForSave(string? original, string? submitted)
    {
        if (!TryRead(submitted, out var plain))
            throw new BusinessException(InvalidContentMessage, "NOIDUNG");

        // A browser normalizes textarea line endings. Saving salary/dates alone
        // must not silently discard the original RTF formatting in an old contract.
        if (TryRead(original, out var oldPlain) &&
            string.Equals(oldPlain?.ReplaceLineEndings("\n"), plain?.ReplaceLineEndings("\n"), StringComparison.Ordinal))
            return original;
        return plain;
    }
}
