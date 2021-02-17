using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReadingsBot.Extensions;
using ReadingsBot.Utilities;
using NodaTime;
using TimeZoneNames;

namespace ReadingsBot.Modules
{
    [Name("Admin")]
    [Summary("Administrative commands.")]
    public class AdminModule: ModuleBase<SocketCommandContext>
    {
        private static readonly Color _color = new Color(216, 112, 135);
        private readonly GuildService _guildService;

        public AdminModule(GuildService guildService)
        {
            _guildService = guildService;
        }

        [Command("setprefix")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [Summary("Set the prefix for ReadingsBot.")]
        public async Task SetPrefixAsync([Summary("The new prefix")] string newPrefix)
        {
            if (string.IsNullOrWhiteSpace(newPrefix))
            {
                await ReplyAsync("You have to enter a valid non-empty prefix.");
                return;
            }

            await _guildService.SetGuildPrefix(Context.Guild.Id, newPrefix);

            await ReplyAsync($"Prefix successfully set to {newPrefix}");
        }

        [Name("Time Zones")]
        [Group("timezone")]
        [Summary("Commands dealing with time zones.")]
        public class TimeZoneModule : ModuleBase<SocketCommandContext>
        {
            private readonly GuildService _guildService;

            public TimeZoneModule(GuildService guildService)
            {
                _guildService = guildService;
            }

            [Command("setdefault")]
            [Alias("set")]
            [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
            [Summary("Set the default time zone for ReadingsBot.")]
            public async Task SetTimezoneAsync([Summary("IANA time zone name")] string timeZone)
            {
                DateTimeZone tz;
                try
                {
                    tz = TextUtilities.ParseTimeZone(timeZone);
                }
                catch (ArgumentException e)
                {
                    await ReplyAsync(e.Message);
                    return;
                }

                await _guildService.SetGuildTimeZone(Context.Guild.Id, tz.Id);

                await ReplyAsync($"Default time zone set to {tz.Id}");
            }

            [Command("getdefault")]
            [Alias("get", "show")]
            [RequireUserPermission(ChannelPermission.ManageMessages, Group = "Permission")]
            [Summary("Display the default time zone for ReadingsBot.")]
            public async Task ShowTimeZoneAsync()
            {
                string timeZoneName = (await _guildService.GetGuildTimeZone(Context.Guild.Id)).Id;

                if (timeZoneName is null)
                {
                    await ReplyAsync("No default time zone has been set for this server.");
                    return;
                }
                else
                {
                    await ReplyAsync($"The default time zone for this server is {timeZoneName}");
                }
            }

            [Command("list")]
            [Alias("help")]
            [RequireUserPermission(ChannelPermission.ManageMessages, Group = "Permission")]
            [Summary("List valid time zone names for the bot.")]
            public async Task TimeZones(int page = 1)
            {
                page--;

                if (page < 0 || page > 20)
                    return;

                IDictionary<string,string> timezones = TZNames.GetDisplayNames("en-US", useIanaZoneIds: true);
                var timezonesPerPage = 20;
                await Context.SendPaginatedConfirmAsync(page,
                    (curPage) => new EmbedBuilder()
                    .WithColor(_color)
                    .WithTitle("Valid Time Zone Names")
                    .WithDescription(string.Join("\n", timezones
                        .Skip(curPage * timezonesPerPage)
                        .Take(timezonesPerPage)
                        .Select(x => $"{x.Key} : {x.Value}"))),
                    timezones.Count, timezonesPerPage).ConfigureAwait(false);
            }
        }

        [Name("Schedules")]
        [Group("schedule")]
        [Summary("Commands to manage scheduled tasks.")]
        public class ScheduleModule: ModuleBase<SocketCommandContext>
        {
            private readonly DiscordSocketClient _client;
            private readonly SchedulingService _scheduleService;
            
            public ScheduleModule(DiscordSocketClient client, SchedulingService scheduleService)
            {
                _client = client;
                _scheduleService = scheduleService;
            }

            [Command("show")]
            [RequireUserPermission(ChannelPermission.ManageMessages, Group = "Permission")]
            [Summary("Shows all scheduled tasks.")]
            public async Task Show()
            {
                List<Data.ScheduledEvent> events = await _scheduleService.GetGuildEvents(Context.Guild.Id);


                var builder = new EmbedBuilder()
                {
                    Color = _color,
                };

                if (events is null || events.Count == 0)
                {
                    builder.WithDescription($"There are no scheduled tasks for {Context.Guild.Name}");
                }
                else
                {
                    builder.WithDescription($"Here are the scheduled tasks for {Context.Guild.Name}");

                    foreach (Data.ScheduledEvent scheduledEvent in events)
                    {
                        builder.AddField(x =>
                        {
                            x.Name = scheduledEvent.EventInfo.Description; 
                            x.Value = EventToString(scheduledEvent);
                            x.IsInline = false;
                        }
                        );
                    }
                }

                await ReplyAsync("", false, builder.Build());
            }

            private string EventToString(Data.ScheduledEvent scheduledEvent)
            {
                var channel = _client.GetChannel(scheduledEvent.ChannelId) as IGuildChannel;
                string channelName = channel.Name;
                string time = TextUtilities.FormatLocalTimeAndTimeZone(
                    scheduledEvent.GetTimeOfDay(),
                    scheduledEvent.GetTimeZone()
                    );
                return $"In {channelName} at {time} daily";
            }
        }
    }
}
