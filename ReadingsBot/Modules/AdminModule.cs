using Discord;
using Discord.WebSocket;
using Discord.Commands;
using ReadingsBot.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ReadingsBot.Modules
{
    [Name("Admin")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [RequireOwner] 
    [Summary("Administrative commands.")]
    public class AdminModule: ModuleBase<SocketCommandContext>
    {
        private static readonly Color _color = new Color(216, 112, 135);
        private readonly GuildService _guildService;

        public AdminModule(GuildService guildService)
        {
            _guildService = guildService;
        }
        

        [Command("timezones")]
        [Summary("List valid time zone formats for the bot.")]
        public async Task TimeZones(int page = 1)
        {
            page--;

            if (page < 0 || page > 20)
                return;

            var timezones = TimeZoneInfo.GetSystemTimeZones()
                .OrderBy(x => x.BaseUtcOffset)
                .ToArray();
            var timezonesPerPage = 20;

            await Context.SendPaginatedConfirmAsync(page,
                (curPage) => new EmbedBuilder()
                .WithColor(_color)
                .WithTitle("Valid Time Zone Names")
                .WithDescription(string.Join("\n", timezones
                    .Skip(curPage * timezonesPerPage)
                    .Take(timezonesPerPage)
                    .Select(x => $"{x.Id}: {x.DisplayName}"))),
                timezones.Length, timezonesPerPage).ConfigureAwait(false);
        }

        [Command("setprefix")]
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
            [Summary("Shows all scheduled tasks.")]
            public async Task Show()
            {
                List<Data.ScheduledEvent> events = await _scheduleService.GetGuildEvents(Context.Guild.Id);

                var builder = new EmbedBuilder()
                {
                    Color = _color,
                    Description = $"Here are the scheduled tasks for {Context.Guild.Name}"
                };

                foreach (Data.ScheduledEvent scheduledEvent in events)
                {
                    builder.AddField(x =>
                    {
                        x.Name = SchedulingService.EventTypeToDescription(scheduledEvent.EventType);
                        x.Value = EventToString(scheduledEvent);
                        x.IsInline = false;
                    }
                    );
                }

                await ReplyAsync("", false, builder.Build());
            }

            private string EventToString(Data.ScheduledEvent scheduledEvent)
            {
                var channel = _client.GetChannel(scheduledEvent.ChannelId) as IGuildChannel;
                string channelName = channel.Name;
                string time = Utilities.TextUtilities.FormatTimeLocallyAsString(
                    scheduledEvent.GetEventTime(),
                    scheduledEvent.TimeZone
                    );
                return $"In {channelName} at {time} daily";
            }
        }
    }
}
