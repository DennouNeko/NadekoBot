using NadekoBot.Core.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NadekoBot.Services.Database.Repositories
{
    public interface IDailyChannelsRepository : IRepository<DailyChannel>
    {
        IEnumerable<DailyChannel> GetAllChannels();
        void RemoveChannel(int id);
    }
}
