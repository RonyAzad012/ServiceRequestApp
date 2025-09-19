using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceRequestApp.Data;
using ServiceRequestApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceRequestApp.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        public MessageController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // GET: Message/Thread/{serviceRequestId}?withUserId={userId}
        public async Task<IActionResult> Thread(int serviceRequestId, string withUserId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var messages = await _dbContext.Messages
                .Where(m => m.ServiceRequestId == serviceRequestId &&
                    ((m.SenderId == currentUser.Id && m.ReceiverId == withUserId) ||
                     (m.SenderId == withUserId && m.ReceiverId == currentUser.Id)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
            ViewBag.ServiceRequestId = serviceRequestId;
            ViewBag.WithUserId = withUserId;
            ViewBag.WithUser = await _userManager.FindByIdAsync(withUserId);
            return View(messages);
        }

        // POST: Message/Send
        [HttpPost]
        public async Task<IActionResult> Send(int serviceRequestId, string receiverId, string content)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["MessageError"] = "Message cannot be empty.";
                return RedirectToAction("Thread", new { serviceRequestId, withUserId = receiverId });
            }
            var message = new Message
            {
                ServiceRequestId = serviceRequestId,
                SenderId = currentUser.Id,
                ReceiverId = receiverId,
                Content = content,
                SentAt = DateTime.UtcNow
            };
            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Thread", new { serviceRequestId, withUserId = receiverId });
        }
    }
}
