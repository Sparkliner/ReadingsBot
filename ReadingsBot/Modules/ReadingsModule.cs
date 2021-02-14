using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace ReadingsBot.Modules
{
    [Name("Readings")]
    [Summary("Commands related to specific readings.")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(ChannelPermission.ManageMessages)]
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

        public abstract class DailyPostModule: ModuleBase<SocketCommandContext>
        {
            protected readonly SchedulingService _scheduleService;
            protected readonly ReadingsPostingService _readingsPoster;

            protected readonly IReadingInfo _readingInfo;

            public DailyPostModule(
                SchedulingService scheduleService,
                ReadingsPostingService readingsPoster,
                IReadingInfo readingInfo)
            {
                _scheduleService = scheduleService;
                _readingsPoster = readingsPoster;
                _readingInfo = readingInfo;
            }

            //[Command("schedule")]
            //[Summary("Schedule the bot to post {readings} at a given time of day in the channel this command is called from.")]
            public virtual async Task Schedule([Remainder] [Summary("Formatted as [time] [time zone]")] string time)
            {
                TimeSpan ts;
                string timeZone;
                try
                {
                    ts = Utilities.TextUtilities.ParseTimeSpanAsLocal(time, out timeZone);
                }
                catch (ArgumentException e)
                {
                    await ReplyAsync(e.Message);
                    return;
                }
                

                bool rescheduled = await _scheduleService.ScheduleOrUpdateEvent(
                    Context.Guild.Id,
                    Context.Channel.Id,
                    ts,
                    timeZone,
                    _readingInfo
                    );

                if (rescheduled)
                {
                    await ReplyAsync($"Rescheduled {_readingInfo.Description} posting in this channel to {Utilities.TextUtilities.FormatTimeLocallyAsString(ts,timeZone)} every day.");
                }
                else
                {
                    await ReplyAsync($"Scheduled {_readingInfo.Description} posting in this channel for {Utilities.TextUtilities.FormatTimeLocallyAsString(ts, timeZone)} every day.");
                }

            }

            //[Command("cancel")]
            //[Summary("Cancel daily posting of {readings} in the channel this command is called from.")]
            public virtual async Task Cancel()
            {
                bool deleted = await _scheduleService.CancelScheduledEvent(Context.Guild.Id, Context.Channel.Id, _readingInfo);
                if (deleted)
                {
                    await ReplyAsync($"Canceled daily {_readingInfo.Description} posting in this channel.");
                }
                else
                {
                    await ReplyAsync($"There was no {_readingInfo.Description} posting to cancel");
                }
            }

            //[Command("now")]
            //[Summary("Post today's lives of the Saints right now.")]
            public abstract Task Now();
        }

        [Group("lives"), Name("Lives of the Saints")]
        public class LivesModule : DailyPostModule
        {
            private readonly OCALivesCacheService _ocaLives;

            public LivesModule(
                SchedulingService scheduleService,
                ReadingsPostingService bulkPoster,
                OCALivesCacheService ocaLives) : base(scheduleService, bulkPoster, new SaintsLivesReadingInfo())
            {
                _ocaLives = ocaLives;
            }

            [Command("schedule")]
            [Summary("Schedule the bot to post lives of the Saints at a given time of day in the channel this command is called from.")]
            public override async Task Schedule([Remainder] [Summary("Formatted as [time] [time zone]")] string time)
            {
                await base.Schedule(time);
            }

            [Command("cancel")]
            [Summary("Cancel daily posting of the lives of the Saints in the channel this command is called from.")]
            public override async Task Cancel()
            {
                await base.Cancel();
            }

            [Command("now")]
            [Summary("Post today's lives of the Saints right now.")]
            public override async Task Now()
            {
                await _readingsPoster.PostLives(Context.Channel.Id);
            }
        }
    }
}
