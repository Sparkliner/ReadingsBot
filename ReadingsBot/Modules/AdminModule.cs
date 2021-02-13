using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            [RequireUserPermission(ChannelPermission.ManageMessages)]
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
                string time = Utilities.TextUtilities.FormatTimeLocallyAsString(
                    scheduledEvent.GetEventTime(),
                    scheduledEvent.TimeZone
                    );
                return $"In {channelName} at {time} daily";
            }
        }
    }
}
