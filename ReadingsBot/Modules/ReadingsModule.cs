using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ReadingsBot.Modules
{
    [Name("Readings")]
    [Summary("Commands related to specific readings.")]
    [RequireContext(ContextType.Guild)]
    public class ReadingsModule: ModuleBase<SocketCommandContext>
    {
        private readonly SchedulingService _scheduleService;
        private static readonly Color _color = new Color(135, 216, 112);

        public ReadingsModule(SchedulingService scheduleService)
        {
            _scheduleService = scheduleService;
        }
        public static Color GetColor()
        {
            return _color;
        }

        [Group("lives"), Name("Lives of the Saints")]
        public class LivesModule : ModuleBase
        {
            private readonly SchedulingService _scheduleService;
            private readonly ReadingsPostingService _readingsPoster;

            private readonly string _eventString = 
                SchedulingService.EventTypeToDescription(SchedulingService.EventType.OCALives);

            public LivesModule(
                SchedulingService scheduleService,
                ReadingsPostingService bulkPoster)
            {
                _scheduleService = scheduleService;
                _readingsPoster = bulkPoster;
            }

            [Command("schedule")]
            [RequireUserPermission(GuildPermission.Administrator)]
            [Summary("Schedule the bot to post lives of the Saints at a given time of day in the channel this command is called from.")]
            public async Task Schedule([Remainder] [Summary("Formatted as hh:mm AM/PM ~ Time zone")] string time)
            {
                TimeSpan ts;
                string timeZone;
                try
                {
                    ts = Utilities.TextUtilities.ParseTimeSpanAsUtc(time, out timeZone);
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
                    await ReplyAsync($"Rescheduled {_eventString} posting in this channel to {Utilities.TextUtilities.FormatTimeLocallyAsString(ts,timeZone)} every day.");
                }
                else
                {
                    await ReplyAsync($"Scheduled {_eventString} posting in this channel for {Utilities.TextUtilities.FormatTimeLocallyAsString(ts, timeZone)} every day.");
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
                await _readingsPoster.PostReadings(ReadingsPostingService.ReadingType.OCALives, Context.Channel.Id);
            }
        }
    }
}
