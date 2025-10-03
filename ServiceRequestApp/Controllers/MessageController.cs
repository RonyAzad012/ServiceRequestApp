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

        // GET: Message/Index - Main messaging dashboard
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Get all messages for the current user
            var messages = await _dbContext.Messages
                .Where(m => m.SenderId == currentUser.Id || m.ReceiverId == currentUser.Id)
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.ServiceRequest)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            // Group messages by conversation (ServiceRequest + Other User)
            var conversations = messages
                .GroupBy(m => new { 
                    ServiceRequestId = m.ServiceRequestId,
                    OtherUserId = m.SenderId == currentUser.Id ? m.ReceiverId : m.SenderId
                })
                .Select(g => new {
                    ServiceRequestId = g.Key.ServiceRequestId,
                    OtherUserId = g.Key.OtherUserId,
                    OtherUser = g.Key.OtherUserId == currentUser.Id ? g.First().Sender : g.First().Receiver,
                    ServiceRequest = g.First().ServiceRequest,
                    LastMessage = g.OrderByDescending(m => m.SentAt).First(),
                    UnreadCount = g.Count(m => m.ReceiverId == currentUser.Id && !m.IsRead)
                })
                .OrderByDescending(c => c.LastMessage.SentAt)
                .ToList();

            ViewBag.CurrentUser = currentUser;
            return View(conversations);
        }

        // GET: Message/Thread/{serviceRequestId}?withUserId={userId}
        public async Task<IActionResult> Thread(int serviceRequestId, string withUserId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            // Mark messages as read
            var unreadMessages = await _dbContext.Messages
                .Where(m => m.ServiceRequestId == serviceRequestId && 
                           m.ReceiverId == currentUser.Id && 
                           m.SenderId == withUserId && 
                           !m.IsRead)
                .ToListAsync();
            
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync();

            var messages = await _dbContext.Messages
                .Where(m => m.ServiceRequestId == serviceRequestId &&
                    ((m.SenderId == currentUser.Id && m.ReceiverId == withUserId) ||
                     (m.SenderId == withUserId && m.ReceiverId == currentUser.Id)))
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var serviceRequest = await _dbContext.ServiceRequests
                .Include(sr => sr.Requester)
                .Include(sr => sr.Provider)
                .FirstOrDefaultAsync(sr => sr.Id == serviceRequestId);

            ViewBag.ServiceRequestId = serviceRequestId;
            ViewBag.WithUserId = withUserId;
            ViewBag.WithUser = await _userManager.FindByIdAsync(withUserId);
            ViewBag.ServiceRequest = serviceRequest;
            ViewBag.CurrentUser = currentUser;
            
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
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Thread", new { serviceRequestId, withUserId = receiverId });
        }

        // API: Send message via AJAX
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return Json(new { success = false, message = "Message cannot be empty." });
                }

                var message = new Message
                {
                    ServiceRequestId = request.ServiceRequestId,
                    SenderId = currentUser.Id,
                    ReceiverId = request.ReceiverId,
                    Content = request.Content,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _dbContext.Messages.Add(message);
                await _dbContext.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = new {
                        id = message.Id,
                        content = message.Content,
                        sentAt = message.SentAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                        senderId = message.SenderId,
                        senderName = currentUser.FirstName + " " + currentUser.LastName
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error sending message: " + ex.Message });
            }
        }

        // API: Get messages for a thread
        [HttpGet]
        public async Task<IActionResult> GetMessages(int serviceRequestId, string withUserId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var messages = await _dbContext.Messages
                    .Where(m => m.ServiceRequestId == serviceRequestId &&
                        ((m.SenderId == currentUser.Id && m.ReceiverId == withUserId) ||
                         (m.SenderId == withUserId && m.ReceiverId == currentUser.Id)))
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .OrderBy(m => m.SentAt)
                    .Select(m => new {
                        id = m.Id,
                        content = m.Content,
                        sentAt = m.SentAt,
                        isRead = m.IsRead,
                        senderId = m.SenderId,
                        senderName = m.Sender.FirstName + " " + m.Sender.LastName,
                        isOwn = m.SenderId == currentUser.Id
                    })
                    .ToListAsync();

                return Json(new { success = true, messages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error loading messages: " + ex.Message });
            }
        }

        // API: Mark messages as read
        [HttpPost]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var messages = await _dbContext.Messages
                    .Where(m => m.ServiceRequestId == request.ServiceRequestId && 
                               m.ReceiverId == currentUser.Id && 
                               m.SenderId == request.SenderId && 
                               !m.IsRead)
                    .ToListAsync();

                foreach (var message in messages)
                {
                    message.IsRead = true;
                    message.ReadAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error marking messages as read: " + ex.Message });
            }
        }
    }

    // Request models for API endpoints
    public class SendMessageRequest
    {
        public int ServiceRequestId { get; set; }
        public string ReceiverId { get; set; }
        public string Content { get; set; }
    }

    public class MarkAsReadRequest
    {
        public int ServiceRequestId { get; set; }
        public string SenderId { get; set; }
    }
}
