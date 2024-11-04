using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TicketAdminChat.Data;
using TicketAdminChat.ViewModel;
using TicketApplication.Models;

namespace UserChatManagement.Controllers
{
    public class RoomDAO
    {
        private readonly ApplicationDbContext _context;

        public RoomDAO(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Room> StartChatAsync(string userName, string adminUserName)
        {
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == adminUserName && u.Role == "Admin");
            if (admin == null)
            {
                return null;
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userName);
            if (user == null)
            {
                return null;
            }

            var roomName = GenerateRoomName(admin.Email, user.Email);
            var room = await _context.Rooms.Include(r => r.Messages)
                                            .FirstOrDefaultAsync(r => r.Name == roomName);

            if (room == null)
            {
                room = new Room
                {
                    Name = roomName,
                    AdminId = admin.Id,
                    UserId = user.Id,
                };
                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();
            }

            return room;
        }

        private string GenerateRoomName(string adminName, string userName)
        {
            var sortedUsernames = new List<string> { adminName, userName };
            sortedUsernames.Sort();
            return $"private_{sortedUsernames[0]}_{sortedUsernames[1]}";
        }

        public async Task<MessageViewModel> SaveMessageAsync(string roomName, string userName, string content)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userName);
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Name == roomName);

            if (room == null || user == null)
            {
                return null;
            }

            var msg = new Message()
            {
                Content = Regex.Replace(content, @"<.*?>", string.Empty),
                FromUserId = user.Id,
                ToRoomId = room.Id,
                Timestamp = DateTime.Now
            };

            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            var createdMessage = new MessageViewModel
            {
                Id = msg.Id,
                Content = msg.Content,
                Timestamp = msg.Timestamp,
                FromUserName = user.Email,
                FromFullName = user.Name,
                Room = room.Name,
            };

            return createdMessage;
        }
    }
}
