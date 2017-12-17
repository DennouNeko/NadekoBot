using NadekoBot.Core.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IDailyChannels : IRepository<DailyChannel>
    {
        IEnumerable<DailyChannel> GetAllChannels();
        void RemoveChannel(int id);
    }
}
