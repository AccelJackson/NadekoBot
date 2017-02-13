using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NadekoBot.Services;
using NadekoBot.Attributes;
using Discord.WebSocket;
using NadekoBot.Services.Database.Models;
using System.Net.Http;
using System.IO;
using static NadekoBot.Modules.Permissions.Permissions;
using System.Collections.Concurrent;
using NLog;

namespace NadekoBot.Modules.Watchpoint
{
    [NadekoModule("Watchpoint", ".")]
    public partial class Watchpoint : DiscordModule
    {

        [NadekoCommand, Usage, Description, Aliases]
        public async Task Test([Remainder] string list = null)
        {
            await Context.Channel.SendConfirmAsync("Test Command").ConfigureAwait(false);
        }
    }
}
