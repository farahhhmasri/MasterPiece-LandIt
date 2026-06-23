using LandIt.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace LandIt.Services
{
    public class NavigationService
    {
        private readonly UserManager<AppUser> _userManager;

        public NavigationService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<string> GetHomeRouteAsync(AppUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Recruiter"))
                return "/Recruiter/Dashboard";

            if (await _userManager.IsInRoleAsync(user, "Candidate"))
                return "/Account/Dashboard";

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return "/Admin/Admin/Index";

            return "/";
        }

        public async Task<string> GetProfileRouteAsync(AppUser user)
        {
            if (await _userManager.IsInRoleAsync(user, "Recruiter"))
                return "/Recruiter/Profile";

            if (await _userManager.IsInRoleAsync(user, "Candidate"))
                return "/Account/Profile";

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return "/Admin/Admin/Profile";

            return "/";
        }
    }
}