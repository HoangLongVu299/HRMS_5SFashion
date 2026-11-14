namespace HRMS.Web.ViewModels;

public class ContractDocument
{
    public string Number { get; set; }="";
    public int? EmployeeId { get; set; }
    public int? PeriodId { get; set; }
    public bool Deleted { get; set; }
    public string? EmployeeName { get; set; }
    public string? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? IdentityNumber { get; set; }
    public string? EmployeeAddress { get; set; }
    public string? EmployeePhone { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyEmail { get; set; }
    public string? Department { get; set; }
    public string? Section { get; set; }
    public string? Position { get; set; }
    public DateTime? SignedOn { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public string? Duration { get; set; }
    public int? Signing { get; set; }
    public double? SalaryCoefficient { get; set; }
    public int? BasicSalary { get; set; }
    public string Body { get; set; }="";
    public string? PreviousContent { get; set; }
    public List<string> Warnings { get; set; }=[];
}

public class EmployeeContractPrintList
{
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public List<ContractPrintChoice> Contracts { get; set; }=[];
}
public record ContractPrintChoice(string Number,DateTime? SignedOn,DateTime? Start,DateTime? End,string? Company);
