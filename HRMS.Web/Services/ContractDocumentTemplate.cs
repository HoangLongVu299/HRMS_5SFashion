using System.Globalization;
using System.Text.RegularExpressions;
using HRMS.Web.ViewModels;

namespace HRMS.Web.Services;

public static class ContractDocumentTemplate
{
    // Versioned plain text in NOIDUNG; existing unmarked content is never bulk rewritten.
    public const string Marker="[[HRMS_CONTRACT_TEMPLATE_V1]]\n";
    public const string Blank="..............................";
    public const string DefaultBody="""
Điều 1: Thời hạn và công việc hợp đồng
- Loại hợp đồng lao động: {{LoaiHopDong}}
- Thời hạn: {{ThoiHan}}
- Ký lần thứ: {{LanKy}}
- Từ ngày {{NgayBatDau}} đến ngày {{NgayKetThuc}}
- Địa điểm làm việc: ..............................
- Chức danh chuyên môn: ..............................  Chức vụ (nếu có): {{ChucVu}}
- Công việc phải làm: theo sự phân công của đơn vị và quy định ngạch.

Điều 2: Chế độ làm việc
- Thời giờ làm việc: Theo sự phân công của đơn vị.
- Được cấp phát những dụng cụ làm việc gồm: Tự túc hoặc theo sự bàn giao của đơn vị.

Điều 3: Quyền lợi và nghĩa vụ của người lao động
1. Quyền lợi
- Phương tiện đi lại làm việc: Tự túc.
- Mức lương chính hoặc tiền công: {{LuongCoBan}} đồng/tháng.
  Tên ngạch: ..............................
  Mã ngạch: ..............................  Hệ số: {{HeSoLuong}}
  Mốc tính nâng bậc lương tiếp theo: ..............................
- Hình thức trả lương: Chuyển khoản.
- Phụ cấp gồm: Theo quy chế chi tiêu nội bộ.
- Được trả lương vào các ngày: Đầu hàng tháng.
- Tiền thưởng: Theo quy chế chi tiêu nội bộ.
- Chế độ nâng lương: Theo quy định của Nhà nước.
- Được trang bị bảo hộ lao động gồm: ..............................
- Chế độ nghỉ ngơi (nghỉ hàng tuần, phép năm, lễ tết...): 12 ngày nghỉ phép và các ngày lễ tết theo quy định của Nhà nước.
- Bảo hiểm xã hội và bảo hiểm y tế: Người lao động trả: 10,5%.
  Người sử dụng lao động trả: 21,5%.
- Chế độ đào tạo: ..............................
- Những thỏa thuận khác: ..............................
2. Nghĩa vụ
- Hoàn thành những công việc đã cam kết trong hợp đồng lao động.
- Chấp hành lệnh điều hành sản xuất - kinh doanh, nội quy kỷ luật lao động, an toàn lao động...
- Bồi thường vi phạm và vật chất: Theo Luật lao động.

Điều 4: Nghĩa vụ và quyền hạn của người sử dụng lao động
1. Nghĩa vụ
- Bảo đảm việc làm và thực hiện đầy đủ những điều đã cam kết trong hợp đồng lao động.
- Thanh toán đầy đủ, đúng thời hạn các chế độ và quyền lợi cho người lao động theo hợp đồng lao động, thỏa ước lao động tập thể (nếu có).
2. Quyền hạn
- Điều hành người lao động hoàn thành công việc theo hợp đồng (bố trí, điều chuyển, tạm ngừng việc).
- Tạm hoãn, chấm dứt hợp đồng lao động, kỷ luật người lao động theo quy định của pháp luật, thỏa ước lao động tập thể (nếu có) và nội quy lao động của doanh nghiệp.

Điều 5: Điều khoản thi hành
- Những vấn đề về lao động không ghi trong hợp đồng lao động này thì áp dụng quy định của thỏa ước tập thể; trường hợp chưa có thỏa ước tập thể thì áp dụng quy định của pháp luật lao động.
""";

    private static readonly Regex Tokens=new(@"\{\{([A-Za-z][A-Za-z0-9]*)\}\}",RegexOptions.CultureInvariant);
    public static string Value(string? value)=>string.IsNullOrWhiteSpace(value)?Blank:value.Trim();
    public static string DateWords(DateTime? date)=>date is {} d
        ? $"{d.Day:00} tháng {d.Month:00} năm {d.Year:0000}" : "...... tháng ...... năm ........";
    public static bool IsTemplate(string? content)=>content?.ReplaceLineEndings("\n").StartsWith(Marker,StringComparison.Ordinal)==true;
    public static string Editable(string? content)=>IsTemplate(content)?content!.ReplaceLineEndings("\n")[Marker.Length..]:DefaultBody;
    public static Dictionary<string,string> Values(ContractDocument document)
    {
        var vi=CultureInfo.GetCultureInfo("vi-VN");
        string type=document.Duration?.Contains("không xác định",StringComparison.OrdinalIgnoreCase)==true ? "Không xác định thời hạn"
            : document.End!=null ? "Có thời hạn" : Value(document.Duration);
        return new(StringComparer.Ordinal) {
            ["SoHopDong"]=document.Number,["HoTenNhanVien"]=Value(document.EmployeeName),["TenCongTy"]=Value(document.CompanyName),
            ["NgayKy"]=DateWords(document.SignedOn),["NgayBatDau"]=DateWords(document.Start),["NgayKetThuc"]=DateWords(document.End),
            ["LoaiHopDong"]=type,["ThoiHan"]=Value(document.Duration),["LanKy"]=document.Signing?.ToString(vi)??Blank,
            ["PhongBan"]=Value(document.Department),["BoPhan"]=Value(document.Section),["ChucVu"]=Value(document.Position),
            ["HeSoLuong"]=document.SalaryCoefficient?.ToString("0.################",vi)??Blank,
            ["LuongCoBan"]=document.BasicSalary?.ToString("N0",vi)??Blank,
            ["CCCD"]=Value(document.IdentityNumber),["DiaChiNhanVien"]=Value(document.EmployeeAddress),["DienThoaiNhanVien"]=Value(document.EmployeePhone)
        };
    }
    public static string Render(string body,ContractDocument document)
    {
        var values=Values(document);
        // One pass: employee data containing braces is data, never another template.
        return Tokens.Replace(body,m=>values.TryGetValue(m.Groups[1].Value,out var value)?value:Blank);
    }
    public static string ForStorage(string? submitted)
    {
        if(!ContractContentText.TryRead(submitted,out var plain)) throw new BusinessException(ContractContentText.InvalidContentMessage,"NOIDUNG");
        string body=IsTemplate(plain)?Editable(plain):string.IsNullOrWhiteSpace(plain)?DefaultBody:plain!.ReplaceLineEndings("\n");
        var known=Values(new ContractDocument());
        foreach(Match token in Tokens.Matches(body))
            if(!known.ContainsKey(token.Groups[1].Value)) throw new BusinessException("Ô tự điền chưa được hỗ trợ: "+token.Value,"NOIDUNG");
        return Marker+body;
    }
}
