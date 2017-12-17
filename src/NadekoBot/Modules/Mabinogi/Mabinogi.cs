using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Services;
using System.Threading.Tasks;
using NadekoBot.Extensions;
using NadekoBot.Modules.Mabinogi.Services;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Services.Database.Models;
using NLog;

namespace NadekoBot.Modules.Mabinogi
{
    public class Mabinogi : NadekoTopLevelModule
    {
        [Group]
        public class MabinogiDailies : NadekoSubmodule<MabinogiDailiesService>
        {
            private readonly DbService _db;

            public MabinogiDailies(DiscordSocketClient client, DbService db)
            {
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageMessages)]
            [Priority(0)]
            public async Task Dailies(ITextChannel channel = null)
            {
                if (channel == null)
                {
                    channel = Context.Channel as ITextChannel;
                    if (channel == null)
                    {
                        await Context.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                            .WithDescription("<#" + Context.Channel.Id + "> is not a text channel!"));
                    }
                }

                var perms = ((IGuildUser)Context.User).GetPermissions((ITextChannel)channel);
                if (!perms.SendMessages || !perms.ReadMessages)
                {
                    await ReplyErrorLocalized("cant_read_or_send").ConfigureAwait(false);
                    return;
                }
                else
                {
                    //var _ = RemindInternal(channel.Id, false, timeStr, message).ConfigureAwait(false);
                    try
                    {
                        if (!_service.ChannelList.TryGetValue(channel.Id, out IUserMessage oldMsg))
                        {
                            var chan = new DailyChannel
                            {
                                ChannelId = channel.Id,
                                ServerId = channel.Guild.Id
                            };
                            _service.ChannelList.AddOrUpdate(channel.Id, null as IUserMessage, (key, old) =>
                            {
                                return oldMsg;
                            });
                            await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                .WithDescription("Notifying about dailies on channel <#" + channel.Id + ">"));
                        }
                        else
                        {
                            if (_service.ChannelList.TryRemove(channel.Id, out IUserMessage _))
                            {
                                await Context.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                                    .WithDescription("Stopped notifying about dailies on channel <#" + channel.Id + ">"));
                            }
                            else
                            {
                                await Context.Channel.EmbedAsync(new EmbedBuilder().WithErrorColor()
                                    .WithDescription("There was an error removing channel <#" + channel.Id + ">"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn(ex);
                    }
                }
            }
        }
    }
}