using NadekoBot.Services.Database.Models;
using System.Collections.Generic;

namespace NadekoBot.Services.Database.Repositories
{
    public interface ISelfAssignedRanksRepository : IRepository<SelfAssignedRank>
    {
        bool DeleteByGuildAndRoleId(ulong guildId, ulong roleId);
        IEnumerable<SelfAssignedRank> GetFromGuild(ulong guildId);
    }
}
