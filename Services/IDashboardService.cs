using System.Threading.Tasks;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(string userId, string role, bool isElevated);
    }
}
