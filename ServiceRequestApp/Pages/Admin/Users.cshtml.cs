using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceRequestApp.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceRequestApp.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UsersModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        public IList<ApplicationUser> Users { get; set; }
        public async Task OnGetAsync()
        {
            Users = await Task.FromResult(_userManager.Users.ToList());
        }
    }
}