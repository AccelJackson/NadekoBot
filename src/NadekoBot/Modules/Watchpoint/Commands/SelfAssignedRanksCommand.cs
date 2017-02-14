using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Watchpoint
{
    public partial class Watchpoint
    {
        [Group]
        public class SelfAssignedRanksCommands : ModuleBase
        {
            
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Rankautodelete()
            {
                bool newval;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    newval = config.AutoDeleteSelfAssignedRankMessages = !config.AutoDeleteSelfAssignedRankMessages;
                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                await Context.Channel.SendConfirmAsync($"ℹ️ Automatic deleting of `rank` and `clear` confirmations has been {(newval ? "**enabled**" : "**disabled**")}.")
                             .ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task Rankadd([Remainder] IRole role)
            {
                IEnumerable<SelfAssignedRank> roles;

                string msg;
                using (var uow = DbHandler.UnitOfWork())
                {
                    roles = uow.SelfAssignedRanks.GetFromGuild(Context.Guild.Id);
                    if (roles.Any(s => s.RoleId == role.Id && s.GuildId == role.Guild.Id))
                    {
                        await Context.Channel.SendMessageAsync($"💢 Rank **{role.Name}** is already in the list.").ConfigureAwait(false);
                        return;
                    }
                    else
                    {
                        uow.SelfAssignedRanks.Add(new SelfAssignedRank {
                            RoleId = role.Id,
                            GuildId = role.Guild.Id
                        });
                        await uow.CompleteAsync();
                        msg = $"🆗 Rank **{role.Name}** added to the list.";
                    }
                }
                await Context.Channel.SendConfirmAsync(msg.ToString()).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task Rankremove([Remainder] IRole role)
            {
                //var channel = (ITextChannel)Context.Channel;

                bool success;
                using (var uow = DbHandler.UnitOfWork())
                {
                    success = uow.SelfAssignedRanks.DeleteByGuildAndRoleId(role.Guild.Id, role.Id);
                    await uow.CompleteAsync();
                }
                if (!success)
                {
                    await Context.Channel.SendErrorAsync("❎ That rank is not self-assignable.").ConfigureAwait(false);
                    return;
                }
                await Context.Channel.SendConfirmAsync($"🗑 **{role.Name}** has been removed from the list of self-assignable ranks.").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Ranklist()
            {
                //var channel = (ITextChannel)Context.Channel;

                var toRemove = new ConcurrentHashSet<SelfAssignedRank>();
                var removeMsg = new StringBuilder();
                var msg = new StringBuilder();
                var roleCnt = 0;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var roleModels = uow.SelfAssignedRanks.GetFromGuild(Context.Guild.Id).ToList();
                    roleCnt = roleModels.Count;
                    msg.AppendLine();
                    
                    foreach (var roleModel in roleModels)
                    {
                        var role = Context.Guild.Roles.FirstOrDefault(r => r.Id == roleModel.RoleId);
                        if (role == null)
                        {
                            uow.SelfAssignedRanks.Remove(roleModel);
                        }
                        else
                        {
                            msg.Append($"**{role.Name}**, ");
                        }
                    }
                    foreach (var role in toRemove)
                    {
                        removeMsg.AppendLine($"`{role.RoleId} not found. Cleaned up.`");
                    }
                    await uow.CompleteAsync();
                }
                await Context.Channel.SendConfirmAsync($"ℹ️ There are `{roleCnt}` self assignable ranks:", msg.ToString() + "\n\n" + removeMsg.ToString()).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles)]
            public async Task Rankexclusive()
            {
                //var channel = (ITextChannel)Context.Channel;

                bool areExclusive;
                using (var uow = DbHandler.UnitOfWork())
                {
                    var config = uow.GuildConfigs.For(Context.Guild.Id, set => set);

                    areExclusive = config.ExclusiveSelfAssignedRanks = !config.ExclusiveSelfAssignedRanks;
                    await uow.CompleteAsync();
                }
                string exl = areExclusive ? "**exclusive**." : "**not exclusive**.";
                await Context.Channel.SendConfirmAsync("ℹ️ Self assigned ranks are now " + exl);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Rankset([Remainder] IRole role)
            {
                //var channel = (ITextChannel)Context.Channel;
                var guildUser = (IGuildUser)Context.User;

                GuildConfig conf;
                IEnumerable<SelfAssignedRank> roles;
                using (var uow = DbHandler.UnitOfWork())
                {
                    conf = uow.GuildConfigs.For(Context.Guild.Id, set => set);
                    roles = uow.SelfAssignedRanks.GetFromGuild(Context.Guild.Id);
                }
                SelfAssignedRank roleModel;
                if ((roleModel = roles.FirstOrDefault(r=>r.RoleId == role.Id)) == null)
                {
                    await Context.Channel.SendErrorAsync("❎ That rank is not self-assignable.").ConfigureAwait(false);
                    return;
                }
                if (guildUser.RoleIds.Contains(role.Id))
                {
                    await Context.Channel.SendErrorAsync($"❎ You already have **{role.Name}** rank.").ConfigureAwait(false);
                    return;
                }

                if (conf.ExclusiveSelfAssignedRanks)
                {
                    var sameRoleId = guildUser.RoleIds.Where(r => roles.Select(sar => sar.RoleId).Contains(r)).FirstOrDefault();
                    var sameRole = Context.Guild.GetRole(sameRoleId);
                    if (sameRoleId != default(ulong))
                    {
                        await Context.Channel.SendErrorAsync($"❎ You already have **{sameRole?.Name}** rank.\n\nClear it with **.clear {sameRole?.Name}** before continuing.").ConfigureAwait(false);
                        return;
                    }
                }
                try
                {
                    await guildUser.AddRolesAsync(role).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendErrorAsync($"⚠️ I am unable to add that rank to you. `I can't add roles to owners or other roles higher than my role in the role hierarchy.`").ConfigureAwait(false);
                    Console.WriteLine(ex);
                    return;
                }
                var msg = await Context.Channel.SendConfirmAsync($"🆗 You now have **{role.Name}** rank.").ConfigureAwait(false);

                if (conf.AutoDeleteSelfAssignedRankMessages)
                {
                    msg.DeleteAfter(3);
                    Context.Message.DeleteAfter(3);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Rankclear([Remainder] IRole role)
            {
                var guildUser = (IGuildUser)Context.User;

                bool autoDeleteSelfAssignedRankMessages;
                IEnumerable<SelfAssignedRank> roles;
                using (var uow = DbHandler.UnitOfWork())
                {
                    autoDeleteSelfAssignedRankMessages = uow.GuildConfigs.For(Context.Guild.Id, set => set).AutoDeleteSelfAssignedRankMessages;
                    roles = uow.SelfAssignedRanks.GetFromGuild(Context.Guild.Id);
                }
                SelfAssignedRank roleModel;
                if ((roleModel = roles.FirstOrDefault(r => r.RoleId == role.Id)) == null)
                {
                    await Context.Channel.SendErrorAsync("💢 That rank is not self-assignable.").ConfigureAwait(false);
                    return;
                }
                if (!guildUser.RoleIds.Contains(role.Id))
                {
                    await Context.Channel.SendErrorAsync($"❎ You don't have **{role.Name}** rank.").ConfigureAwait(false);
                    return;
                }
                try
                {
                    await guildUser.RemoveRolesAsync(role).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    await Context.Channel.SendErrorAsync($"⚠️ I am unable to add that rank to you. `I can't remove roles to owners or other roles higher than my role in the role hierarchy.`").ConfigureAwait(false);
                    return;
                }
                var msg = await Context.Channel.SendConfirmAsync($"🆗 You no longer have **{role.Name}** rank.").ConfigureAwait(false);

                if (autoDeleteSelfAssignedRankMessages)
                {
                    msg.DeleteAfter(3);
                    Context.Message.DeleteAfter(3);
                }
            }
        }
    }
}
