using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketAdminChat.ViewModel
{
    public class MessageViewModel
    {
        public string? Id { get; set; }
        public string? Content { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? FromUserName { get; set; }
        public string? FromFullName { get; set; }
        public string? Room { get; set; }
    }
}