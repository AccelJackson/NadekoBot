namespace NadekoBot.Services.Database.Models
{
    public class SelfAssignedRank : DbEntity
    {
        public ulong GuildId { get; set; }
        public ulong RoleId { get; set; }
    }
}
