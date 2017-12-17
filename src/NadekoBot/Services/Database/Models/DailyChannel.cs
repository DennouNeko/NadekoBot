using System;
using System.Collections.Generic;
using System.Text;

namespace NadekoBot.Services.Database.Models
{
    public class DailyChannel : DbEntity
    {
        public ulong ChannelId { get; set; }
        public ulong ServerId { get; set; }
    }
}
