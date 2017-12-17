using Microsoft.EntityFrameworkCore;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace NadekoBot.Services.Database.Repositories.Impl
{
    public class DailyChannelsRepository : Repository<DailyChannel>, IDailyChannelsRepository
    {
        public DailyChannelsRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<DailyChannel> GetAllChannels()
        {
            return _set;
        }

        public void RemoveChannel(int id)
        {
            var p = _set.FirstOrDefaultAsync(x => x.Id == id);
            p.RunSynchronously();
            _set.Remove(p.Result);
        }
    }
}
