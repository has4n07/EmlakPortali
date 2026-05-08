using System.Security.Claims;
using EmlakPortali.Api.Data;
using EmlakPortali.Api.Models;
using EmlakPortali.Api.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmlakPortali.Api.Controllers;

public class ContactMessageCreateDto
{
    public int ListingId { get; set; }
    public string SenderName { get; set; } = null!;
    public string SenderEmail { get; set; } = null!;
    public string SenderPhone { get; set; } = null!;
    public string Message { get; set; } = null!;
}

public class ContactMessageReplyDto
{
    public int ListingId { get; set; }
    public string ReceiverEmail { get; set; } = null!;
    public string Message { get; set; } = null!;
}

[Route("api/[controller]")]
[ApiController]
public class ContactMessagesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ContactMessagesController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize]
    [HttpPost]
    public async Task<Result> SendMessage(ContactMessageCreateDto dto)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return new Result { Status = false, Message = "Kullanıcı bilgisi alınamadı." };

        var senderUser = await _userManager.FindByIdAsync(userId.ToString());
        if (senderUser == null)
            return new Result { Status = false, Message = "Gönderen kullanıcı bulunamadı." };

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == dto.ListingId);
        if (listing == null)
            return new Result { Status = false, Message = "İlan bulunamadı." };

        var message = new ContactMessage
        {
            ListingId = dto.ListingId,
            ReceiverId = listing.OwnerUserId,
            SenderName = string.IsNullOrWhiteSpace(dto.SenderName) ? (senderUser.FullName ?? senderUser.Email ?? "Kullanıcı") : dto.SenderName.Trim(),
            SenderEmail = senderUser.Email ?? dto.SenderEmail.Trim(),
            SenderPhone = string.IsNullOrWhiteSpace(dto.SenderPhone) ? (senderUser.PhoneNumber ?? "") : dto.SenderPhone.Trim(),
            Message = dto.Message.Trim(),
            IsRead = false,
            Created = DateTime.UtcNow,
            IsActive = true
        };

        _db.ContactMessages.Add(message);
        await _db.SaveChangesAsync();

        return new Result { Status = true, Message = "Mesajınız başarıyla gönderildi." };
    }

    [Authorize]
    [HttpPost("reply")]
    public async Task<Result> Reply(ContactMessageReplyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ReceiverEmail) || string.IsNullOrWhiteSpace(dto.Message))
            return new Result { Status = false, Message = "Alıcı ve mesaj zorunludur." };

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return new Result { Status = false, Message = "Kullanıcı bilgisi alınamadı." };

        var senderUser = await _userManager.FindByIdAsync(userId.ToString());
        if (senderUser == null)
            return new Result { Status = false, Message = "Kullanıcı bulunamadı." };

        var receiver = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == dto.ReceiverEmail.Trim());
        if (receiver == null)
            return new Result { Status = false, Message = "Alıcı kullanıcı bulunamadı." };

        var message = new ContactMessage
        {
            ListingId = dto.ListingId,
            ReceiverId = receiver.Id,
            SenderName = senderUser.FullName ?? senderUser.Email ?? "Kullanıcı",
            SenderEmail = senderUser.Email ?? "",
            SenderPhone = senderUser.PhoneNumber ?? "",
            Message = dto.Message.Trim(),
            IsRead = false,
            Created = DateTime.UtcNow,
            IsActive = true
        };

        _db.ContactMessages.Add(message);
        await _db.SaveChangesAsync();

        return new Result { Status = true, Message = "Mesaj gönderildi." };
    }

    [Authorize]
    [HttpGet("conversations")]
    public async Task<DataResult<object>> GetConversations()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return new DataResult<object> { Status = false, Message = "Kullanıcı bilgisi alınamadı." };

        var me = await _userManager.FindByIdAsync(userId.ToString());
        if (me?.Email == null)
            return new DataResult<object> { Status = false, Message = "Kullanıcı e-posta bilgisi alınamadı." };

        var myEmail = me.Email;

        var received = await _db.ContactMessages
            .Include(x => x.Listing)
            .Where(x => x.ReceiverId == userId)
            .Select(x => new
            {
                x.Id,
                x.ListingId,
                ListingTitle = x.Listing != null ? x.Listing.Title : "-",
                CounterpartEmail = x.SenderEmail,
                CounterpartName = x.SenderName,
                LastMessage = x.Message,
                x.Created,
                IsRead = x.IsRead,
                UnreadCount = !x.IsRead ? 1 : 0
            })
            .ToListAsync();

        var sent = await _db.ContactMessages
            .Include(x => x.Listing)
            .Where(x => x.SenderEmail == myEmail)
            .Select(x => new
            {
                x.Id,
                x.ListingId,
                ListingTitle = x.Listing != null ? x.Listing.Title : "-",
                CounterpartEmail = x.Receiver.Email ?? "",
                CounterpartName = x.Receiver.FullName ?? x.Receiver.Email ?? "Kullanıcı",
                LastMessage = x.Message,
                x.Created,
                IsRead = true,
                UnreadCount = 0
            })
            .ToListAsync();

        var merged = received
            .Concat(sent)
            .GroupBy(x => new { x.ListingId, x.CounterpartEmail })
            .Select(g =>
            {
                var latest = g.OrderByDescending(x => x.Created).First();
                return new
                {
                    latest.ListingId,
                    latest.ListingTitle,
                    latest.CounterpartEmail,
                    latest.CounterpartName,
                    latest.LastMessage,
                    latest.Created,
                    UnreadCount = g.Sum(x => x.UnreadCount)
                };
            })
            .OrderByDescending(x => x.Created)
            .ToList();

        return new DataResult<object> { Status = true, Message = "OK", Data = merged };
    }

    [Authorize]
    [HttpGet("thread")]
    public async Task<DataResult<object>> GetThread([FromQuery] int listingId, [FromQuery] string counterpartEmail)
    {
        if (string.IsNullOrWhiteSpace(counterpartEmail))
            return new DataResult<object> { Status = false, Message = "Karşı taraf bilgisi zorunludur." };

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return new DataResult<object> { Status = false, Message = "Kullanıcı bilgisi alınamadı." };

        var me = await _userManager.FindByIdAsync(userId.ToString());
        if (me?.Email == null)
            return new DataResult<object> { Status = false, Message = "Kullanıcı e-posta bilgisi alınamadı." };

        var myEmail = me.Email;
        var normalizedCounterpartEmail = counterpartEmail.Trim();
        var counterpart = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == normalizedCounterpartEmail);
        var counterpartId = counterpart?.Id;

        var thread = await _db.ContactMessages
            .Where(x => x.ListingId == listingId &&
                        ((x.ReceiverId == userId && x.SenderEmail == normalizedCounterpartEmail) ||
                         (x.SenderEmail == myEmail && counterpartId.HasValue && x.ReceiverId == counterpartId.Value)))
            .OrderBy(x => x.Created)
            .Select(x => new
            {
                x.Id,
                x.ListingId,
                x.SenderName,
                x.SenderEmail,
                x.SenderPhone,
                x.Message,
                x.IsRead,
                x.Created,
                IsMine = x.SenderEmail == myEmail
            })
            .ToListAsync();

        var unread = await _db.ContactMessages
            .Where(x => x.ListingId == listingId && x.ReceiverId == userId && x.SenderEmail == normalizedCounterpartEmail && !x.IsRead)
            .ToListAsync();
        if (unread.Count > 0)
        {
            foreach (var item in unread) item.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return new DataResult<object> { Status = true, Message = "OK", Data = thread };
    }

    [Authorize]
    [HttpGet]
    public async Task<DataResult<object>> GetMessages()
    {
        var isAdmin = User.IsInRole("Admin");
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return new DataResult<object> { Status = false, Message = "Kullanıcı bilgisi alınamadı." };

        var query = _db.ContactMessages
            .Include(x => x.Listing)
            .AsNoTracking()
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(x => x.ReceiverId == userId);
        }

        var messages = await query.OrderByDescending(x => x.Created).ToListAsync();

        return new DataResult<object>
        {
            Status = true,
            Message = "OK",
            Data = messages.Select(m => new
            {
                m.Id,
                m.SenderName,
                m.SenderEmail,
                m.SenderPhone,
                m.Message,
                m.IsRead,
                m.Created,
                ListingTitle = m.Listing?.Title,
                ListingId = m.ListingId
            })
        };
    }

    [Authorize]
    [HttpPut("{id:int}/read")]
    public async Task<Result> MarkAsRead(int id)
    {
        var isAdmin = User.IsInRole("Admin");
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return new Result { Status = false, Message = "Kullanıcı bilgisi alınamadı." };

        var message = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id);
        if (message == null) return new Result { Status = false, Message = "Mesaj bulunamadı." };

        if (!isAdmin && message.ReceiverId != userId)
            return new Result { Status = false, Message = "Yetkiniz yok." };

        message.IsRead = true;
        await _db.SaveChangesAsync();

        return new Result { Status = true, Message = "Okundu olarak işaretlendi." };
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<Result> DeleteMessage(int id)
    {
        var isAdmin = User.IsInRole("Admin");
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return new Result { Status = false, Message = "Kullanıcı bilgisi alınamadı." };

        var message = await _db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id);
        if (message == null) return new Result { Status = false, Message = "Mesaj bulunamadı." };

        if (!isAdmin && message.ReceiverId != userId)
            return new Result { Status = false, Message = "Yetkiniz yok." };

        _db.ContactMessages.Remove(message);
        await _db.SaveChangesAsync();

        return new Result { Status = true, Message = "Mesaj silindi." };
    }
}
