using HRMS.Web.Data;
using HRMS.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Web.Services;

public class ContractDocumentService(HrmsContext db)
{
    public async Task<ContractDocument?> Build(string number)
    {
        var contract=await db.Contracts.AsNoTracking().Include(x=>x.Employee).Include(x=>x.Company)
            .SingleOrDefaultAsync(x=>x.SOHD==number);
        if(contract==null) return null;
        var employee=contract.Employee;
        var document=new ContractDocument {
            Number=contract.SOHD,EmployeeId=contract.MANV,PeriodId=contract.IDDOT,
            Deleted=contract.DELETED_DATE!=null||contract.DELETED_BY!=null,
            EmployeeName=employee?.HOTEN,BirthDate=employee?.NGAYSINH?.ToString("dd/MM/yyyy"),
            Gender=employee?.GIOITINH is {} gender ? gender?"Nam":"Nữ" : null,
            IdentityNumber=employee?.CCCD,EmployeeAddress=employee?.DIACHI,EmployeePhone=employee?.DIENTHOAI,
            CompanyName=contract.Company?.TENCTY,CompanyAddress=contract.Company?.DIACHI,
            CompanyPhone=contract.Company?.DIENTHOAI,CompanyEmail=contract.Company?.EMAIL,
            SignedOn=contract.NGAYKY,Start=contract.NGAYBATDAU,End=contract.NGAYKETTHUC,Duration=contract.THOIHAN,
            Signing=contract.LANKY,SalaryCoefficient=contract.HESOLUONG,BasicSalary=contract.LUONGCOBAN
        };
        if(employee==null) document.Warnings.Add("Hợp đồng chưa liên kết hồ sơ nhân viên; các thông tin thiếu được để trống.");
        if(contract.IDDOT is {} periodId && contract.NGAYBATDAU is {} start)
        {
            var period=await db.WorkPeriods.AsNoTracking().SingleOrDefaultAsync(x=>x.IDDOT==periodId);
            if(period!=null && period.MANV==contract.MANV && period.MACTY==contract.MACTY)
            {
                var at=DateOnly.FromDateTime(start);
                var histories=await db.WorkHistorys.AsNoTracking().Include(x=>x.Department).Include(x=>x.Section).Include(x=>x.Position)
                    .Where(x=>x.IDDOT==periodId && x.TUNGAY<=at && (x.DENNGAY==null||x.DENNGAY>=at)).Take(2).ToListAsync();
                if(histories.Count==1 && period.NGAYBATDAU<=at && (period.NGAYKETTHUC==null||period.NGAYKETTHUC>=at))
                {
                    document.Department=histories[0].Department?.TENPB;document.Section=histories[0].Section?.TENBP;
                    document.Position=histories[0].Position?.TENCV;
                }
                else document.Warnings.Add("Chưa xác định duy nhất tổ chức tại ngày bắt đầu hợp đồng; không lấy phòng ban/chức vụ hiện tại thay cho lịch sử.");
            }
            else document.Warnings.Add("Liên kết đợt không khớp nhân viên/công ty của hợp đồng; thông tin tổ chức được để trống để xác minh.");
        }
        else document.Warnings.Add("Hợp đồng thiếu đợt hoặc ngày bắt đầu; chưa đủ căn cứ điền tổ chức trong quá khứ.");
        if(document.Deleted) document.Warnings.Add("Hợp đồng đã được đánh dấu xóa mềm; đây là bản lưu lịch sử.");
        if(!ContractDocumentTemplate.IsTemplate(contract.NOIDUNG))
        {
            if(ContractContentText.TryRead(contract.NOIDUNG,out var previous)) document.PreviousContent=previous;
            else document.Warnings.Add(ContractContentText.InvalidContentMessage);
        }
        document.Body=ContractDocumentTemplate.Render(ContractDocumentTemplate.Editable(contract.NOIDUNG),document);
        return document;
    }
    public async Task<EmployeeContractPrintList?> ForEmployee(int employeeId)
    {
        var employee=await db.Employees.AsNoTracking().Where(x=>x.MANV==employeeId).Select(x=>new{x.MANV,x.HOTEN}).SingleOrDefaultAsync();
        if(employee==null) return null;
        return new EmployeeContractPrintList {EmployeeId=employee.MANV,EmployeeName=employee.HOTEN,
            Contracts=await db.Contracts.AsNoTracking().Where(x=>x.MANV==employeeId&&x.DELETED_DATE==null&&x.DELETED_BY==null)
                .OrderByDescending(x=>x.NGAYKY).ThenByDescending(x=>x.NGAYBATDAU).ThenBy(x=>x.SOHD)
                .Select(x=>new ContractPrintChoice(x.SOHD,x.NGAYKY,x.NGAYBATDAU,x.NGAYKETTHUC,x.Company==null?null:x.Company.TENCTY)).ToListAsync()};
    }
}
