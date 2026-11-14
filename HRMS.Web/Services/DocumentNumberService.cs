using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using HRMS.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Web.Services;

public static class DocumentNumber
{
    // Preserve the observed No05 format. Counters are separate by document type and year.
    public static string Next(IEnumerable<string> existing, int year, string suffix)
    {
        if(year is <1753 or >9999) throw new BusinessException("Năm văn bản phải từ 1753 đến 9999.");
        if(suffix is not ("HĐLĐ" or "QĐDC" or "QĐTV" or "QĐKT" or "QĐKL")) throw new ArgumentException("Unknown document type",nameof(suffix));
        var pattern=new Regex(@"^([0-9]+)/"+year.ToString("D4",CultureInfo.InvariantCulture)+"/"+Regex.Escape(suffix)+"$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
        BigInteger max=0;
        foreach(var value in existing)
        {
            var match=pattern.Match(value.Trim().Normalize(NormalizationForm.FormC));
            if(match.Success && BigInteger.TryParse(match.Groups[1].Value,NumberStyles.None,CultureInfo.InvariantCulture,out var n) && n>max) max=n;
        }
        var result=(max+1).ToString("D5",CultureInfo.InvariantCulture)+"/"+year.ToString("D4",CultureInfo.InvariantCulture)+"/"+suffix;
        if(result.Length>50) throw new BusinessException("Dãy số văn bản đã vượt giới hạn 50 ký tự; cần kiểm tra dữ liệu.");
        return result;
    }
}

public class DocumentNumberService(HrmsContext db)
{
    // GET uses this only for a preview. The writer calls it again after obtaining the
    // transaction-owned HRMS.Personnel.Write lock and before inserting the document.
    // Include soft-deleted rows: document primary keys must never be reused.
    public async Task<string> NextContract(DateTime? signedOn=null)
    {
        var numbers=await db.Contracts.AsNoTracking().Select(x=>x.SOHD).ToListAsync();
        int year=(signedOn??DateTime.Today).Year;
        var next=DocumentNumber.Next(numbers,year,"HĐLĐ");
        while(await db.Contracts.AnyAsync(x=>x.SOHD==next))
        {
            numbers.Add(next);next=DocumentNumber.Next(numbers,year,"HĐLĐ");
        }
        return next;
    }
    public async Task<string> NextTransfer(DateTime? decidedOn=null)
    {
        var numbers=await db.Transfers.AsNoTracking().Select(x=>x.SOQD).ToListAsync();
        int year=(decidedOn??DateTime.Today).Year;
        var next=DocumentNumber.Next(numbers,year,"QĐDC");
        while(await db.Transfers.AnyAsync(x=>x.SOQD==next))
        {
            numbers.Add(next);next=DocumentNumber.Next(numbers,year,"QĐDC");
        }
        return next;
    }
    public async Task<string> NextTermination(DateTime? leaveOn=null)
    {
        var numbers=await db.Terminations.AsNoTracking().Select(x=>x.SOQD).ToListAsync();
        int year=(leaveOn??DateTime.Today).Year;
        var next=DocumentNumber.Next(numbers,year,"QĐTV");
        while(await db.Terminations.AnyAsync(x=>x.SOQD==next))
        {numbers.Add(next);next=DocumentNumber.Next(numbers,year,"QĐTV");}
        return next;
    }
    public async Task<string> NextReward(int? type,DateTime? decidedOn=null)
    {
        if(!RewardTypes.IsKnown(type)) throw new BusinessException("Chọn loại Khen thưởng hoặc Kỷ luật để tạo số quyết định.","Type");
        string suffix=type==RewardTypes.Reward?"QĐKT":"QĐKL";
        var numbers=await db.RewardDecisions.AsNoTracking().Select(x=>x.SOQUYETDINH).ToListAsync();
        int year=(decidedOn??DateTime.Today).Year;
        var next=DocumentNumber.Next(numbers,year,suffix);
        while(await db.RewardDecisions.AnyAsync(x=>x.SOQUYETDINH==next))
        {numbers.Add(next);next=DocumentNumber.Next(numbers,year,suffix);}
        return next;
    }
}
