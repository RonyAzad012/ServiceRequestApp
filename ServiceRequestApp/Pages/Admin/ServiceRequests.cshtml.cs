using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using System.Collections.Generic;
using System.Linq;

namespace ServiceRequestApp.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ServiceRequestsModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;
        public ServiceRequestsModel(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IList<ServiceRequest> Requests { get; set; }
        public void OnGet()
        {
            Requests = _dbContext.ServiceRequests.ToList();
        }
    }
}