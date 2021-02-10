using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace ReadingsBot.Modules
{
    [Name("Readings")]
    [Summary("Commands related to specific readings.")]
    public class ReadingsModule: ModuleBase<SocketCommandContext>
    {
        private readonly SchedulingService _scheduleService;
        private static readonly Color _color = new Color(135, 216, 112);

        public ReadingsModule(SchedulingService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [Group("lives"), Name("Lives of the Saints")]
        public class LivesModule : ModuleBase
        {
            private static OCALives _lives;
            private readonly SchedulingService _scheduleService;

            private readonly string _eventString = 
                SchedulingService.EventTypeToDescription(SchedulingService.EventType.OCALives);

            public LivesModule(SchedulingService scheduleService)
            {
                _scheduleService = scheduleService;
            }

            [Command("schedule")]
            [RequireUserPermission(GuildPermission.Administrator)]
            [Summary("Schedule the bot to post lives of the Saints at a given time of day in the channel this command is called from.")]
            public async Task Schedule([Remainder] [Summary("Formatted as hh:mm AM/PM - Time zone")] string time)
            {
                TimeSpan ts;
                string timeZone;
                try
                {
                    ts = ParsingUtilities.ParseTimeSpanAsUtc(time, out timeZone);
                }
                catch (ArgumentException e)
                {
                    await ReplyAsync(e.Message);
                    return;
                }
                

                bool rescheduled = await _scheduleService.ScheduleNewEvent(
                    Context.Guild.Id,
                    Context.Channel.Id,
                    ts,
                    timeZone,
                    SchedulingService.EventType.OCALives
                    );

                

                if (rescheduled)
                {
                    await ReplyAsync($"Rescheduled {_eventString} posting in this channel to {ParsingUtilities.FormatTimeLocallyAsString(ts,timeZone)} every day.");
                }
                else
                {
                    await ReplyAsync($"Scheduled {_eventString} posting in this channel for {ParsingUtilities.FormatTimeLocallyAsString(ts, timeZone)} every day.");
                }

            }

            [Command("cancel")]
            [RequireUserPermission(GuildPermission.Administrator)]
            [Summary("Cancel daily posting of the lives of the Saints in the channel this command is called from.")]
            public async Task Cancel()
            {
                bool deleted = await _scheduleService.CancelScheduledEvent(Context.Guild.Id, Context.Channel.Id, SchedulingService.EventType.OCALives);
                if (deleted)
                {
                    await ReplyAsync($"Canceled daily {_eventString} posting in this channel.");
                }
                else
                {
                    await ReplyAsync($"There was no {_eventString} posting to cancel");
                }
            }

            [Command("now")]
            [RequireUserPermission(ChannelPermission.ManageMessages)]
            [Summary("Post today's lives of the Saints right now.")]
            public async Task Now()
            {
                await PostLives(Context.Channel as Discord.WebSocket.ISocketMessageChannel);
            }

            public static async Task PostLives(Discord.WebSocket.ISocketMessageChannel channel)
            {
                //update lives
                _lives = await OCALivesCache.GetLives();

                foreach (OCALife life in _lives.commemorations)
                {
                    await PostLife(life, channel);
                    await Task.Delay(2000);
                }
            }

            private static async Task PostLife(OCALife life, Discord.WebSocket.ISocketMessageChannel channel)
            {
                await channel.SendMessageAsync("", false, life.ToEmbedBuilder(_color).Build());
            } 
        }        
    }
}
