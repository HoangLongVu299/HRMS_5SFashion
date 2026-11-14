using HRMS.Web.Data;
using HRMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace HRMS.Web.Services;
public class LookupService(HrmsContext db)
{
    public async Task<Lookups> LoadAsync() => new()
    {
        Companies = await db.Companys.AsNoTracking().OrderBy(x=>x.TENCTY).Select(x=>new SelectListItem(x.TENCTY ?? "Công ty #" + x.MACTY, x.MACTY.ToString())).ToListAsync(),
        Departments = await db.Departments.AsNoTracking().OrderBy(x=>x.TENPB).Select(x=>new SelectListItem(x.TENPB ?? "Phòng ban #" + x.IDPB, x.IDPB.ToString())).ToListAsync(),
        Sections = await db.Sections.AsNoTracking().OrderBy(x=>x.TENBP).Select(x=>new SelectListItem(x.TENBP ?? "Bộ phận #" + x.IDBP, x.IDBP.ToString())).ToListAsync(),
        Positions = await db.Positions.AsNoTracking().OrderBy(x=>x.TENCV).Select(x=>new SelectListItem(x.TENCV ?? "Chức vụ #" + x.IDCV, x.IDCV.ToString())).ToListAsync(),
        Educations = await db.Educations.AsNoTracking().OrderBy(x=>x.TENTD).Select(x=>new SelectListItem(x.TENTD ?? "Trình độ #" + x.IDTD, x.IDTD.ToString())).ToListAsync(),
        Ethnicities = await db.Ethnicitys.AsNoTracking().OrderBy(x=>x.TENDT).Select(x=>new SelectListItem(x.TENDT ?? "Dân tộc #" + x.ID, x.ID.ToString())).ToListAsync(),
        Religions = await db.Religions.AsNoTracking().OrderBy(x=>x.TENTG).Select(x=>new SelectListItem(x.TENTG ?? "Tôn giáo #" + x.ID, x.ID.ToString())).ToListAsync()
    };
}
