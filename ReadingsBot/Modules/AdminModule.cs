using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NodaTime;
using ReadingsBot.Data;
using ReadingsBot.Extensions;
using ReadingsBot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeZoneNames;

namespace ReadingsBot.Modules
{
    [Name("Admin")]
    [Summary("Administrative commands.")]
    public class AdminModule : ColorModule
    {
        private readonly GuildService _guildService;

        public AdminModule(GuildService guildService)
            : base(new Color(216, 112, 135))
        {
            _guildService = guildService;
        }

        //not a fan of this solution
        static Color GetEmbedColor => new Color(216, 112, 135);

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
        public class TimeZoneModule : InfoEmbedModule
        {
            private readonly GuildService _guildService;

            public TimeZoneModule(GuildService guildService, ReadingsBotVersionInfo versionInfo)
                : base(versionInfo, AdminModule.GetEmbedColor)
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

                IDictionary<string, string> timezones = TZNames.GetDisplayNames("en-US", useIanaZoneIds: true);
                var timezonesPerPage = 20;
                await Context.SendPaginatedConfirmAsync(page,
                    (curPage) => BasicEmbedBuilder
                    .WithTitle("Valid Time Zone Names")
                    .WithDescription(string.Join("\n", timezones
                        .Skip(curPage * timezonesPerPage)
                        .Take(timezonesPerPage)
                        .Select(x => $"{x.Key} : {x.Value}"))),
                    timezones.Count, timezonesPerPage).ConfigureAwait(false);
            }
        }

        [Name("Readings")]
        [Group("readings")]
        [Summary("Commands to manage scheduled readings.")]
        public class ScheduleModule : InfoEmbedModule
        {
            private readonly DiscordSocketClient _client;
            private readonly SchedulingService _scheduleService;

            public ScheduleModule(DiscordSocketClient client, SchedulingService scheduleService, ReadingsBotVersionInfo versionInfo)
                : base(versionInfo, AdminModule.GetEmbedColor)
            {
                _client = client;
                _scheduleService = scheduleService;
            }

            [Command("show")]
            [RequireUserPermission(ChannelPermission.ManageMessages, Group = "Permission")]
            [Summary("Shows all scheduled readings.")]
            public async Task Show()
            {
                var eventGroups = (await _scheduleService.GetGuildEventsAsync(Context.Guild.Id))
                    .GroupBy(evt => evt.EventInfo.Description)
                    .OrderBy(g => g.Key);


                var builder = BasicEmbedBuilder;

                if (eventGroups is null || !eventGroups.Any())
                {
                    builder.WithDescription($"There are no scheduled readings for {Context.Guild.Name}");
                }
                else
                {
                    builder.WithDescription($"Here are the scheduled readings for {Context.Guild.Name}");

                    foreach (var eventType in eventGroups)
                    {
                        builder.AddField(x =>
                        {
                            x.Name = eventType.Key;
                            x.Value = String.Join("\n", eventType.OrderBy(evt => _client.GetChannel(evt.ChannelId).ToString()).Select(evt => $"{EventToString(evt)}"));
                            x.IsInline = false;
                        });
                    }
                }

                await ReplyAsync("", false, builder.Build());
            }

            private string EventToString(Data.ScheduledEvent scheduledEvent)
            {
                var channel = _client.GetChannel(scheduledEvent.ChannelId) as IGuildChannel;
                string channelName = channel.Name;
                if (scheduledEvent.EventInfo is BlogsReadingInfo)
                {
                    return $"In {channelName}, updated hourly" +
                        (scheduledEvent.EventInfo as BlogsReadingInfo).Blogs;
                }
                else
                {
                    string time = TextUtilities.FormatLocalTimeAndTimeZone(
                        scheduledEvent.GetTimeOfDay(),
                        scheduledEvent.GetTimeZone()
                        );
                    return $"In {channelName} at {time} daily";
                }
            }
        }
    }
}
