using Microsoft.AspNetCore.Mvc.Rendering;
namespace HRMS.Web.Services;
public static class RewardTypes
{
    // Evidence: five BACPAC decisions; QĐKT=1 (2 rows), QĐKL=2 (3 rows).
    // See docs/MO_RONG_DANH_MUC_KHEN_THUONG.md. No interpretation of unknown values.
    public const int Reward=1, Discipline=2;
    public static bool IsKnown(int? value) => value is Reward or Discipline;
    public static string Label(int? value) => value switch { Reward=>"Khen thưởng", Discipline=>"Kỷ luật", _=>"Chưa xác định" };
    public static string Badge(int? value) => value switch { Reward=>"text-bg-success", Discipline=>"text-bg-danger", _=>"text-bg-secondary" };
    public static List<SelectListItem> Options() => [new(Label(Reward), Reward.ToString()), new(Label(Discipline), Discipline.ToString())];
    public static List<SelectListItem> Filters() => [new("Tất cả loại", ""), ..Options(), new("Chưa xác định", "unknown")];
}
