using Discord;
using Discord.Commands;
using NodaTime;
using ReadingsBot.Utilities;
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

        private static readonly Color _color = new Color(135, 216, 112);

        public static Color GetColor()
        {
            return _color;
        }

        public abstract class DailyPostModule: ModuleBase<SocketCommandContext>
        {
            protected readonly SchedulingService _scheduleService;
            protected readonly GuildService _guildService;
            protected readonly ReadingsPostingService _readingsPoster;
            protected readonly IClock _clock;

            protected readonly IReadingInfo _readingInfo;

            public DailyPostModule(
                SchedulingService scheduleService,
                GuildService guildService,
                ReadingsPostingService readingsPoster,
                IClock clock,
                IReadingInfo readingInfo)
            {
                _scheduleService = scheduleService;
                _guildService = guildService;
                _readingsPoster = readingsPoster;
                _clock = clock;
                _readingInfo = readingInfo;
            }

            //[Command("schedule")]
            //[Summary("Schedule the bot to post {readings} at a given time of day in the channel this command is called from.")]
            public virtual async Task Schedule([Remainder] [Summary("Formatted as [time] (Optional)<-t [time zone]>")] string time)
            {
                LocalTime localEventTime;
                DateTimeZone timeZone;
                try
                {
                    localEventTime = Utilities.TextUtilities.ParseLocalTime(time, out timeZone);
                }
                catch (ArgumentException e)
                {
                    await ReplyAsync(e.Message);
                    return;
                }

                if (timeZone is null)
                {
                    //use default time zone
                    timeZone = await _guildService.GetGuildTimeZone(Context.Guild.Id);
                }

                ZonedClock zc = new ZonedClock(_clock, timeZone, CalendarSystem.Iso);
                LocalDateTime localEventDateTime = zc.GetCurrentDate() + localEventTime;
                //if the chosen time is already past today, set it for tomorrow instead
                if (localEventDateTime < zc.GetCurrentLocalDateTime())
                {
                    localEventDateTime = localEventDateTime.PlusDays(1);
                }
                ZonedDateTime eventZonedDateTime = localEventDateTime.InZoneLeniently(timeZone);

                bool rescheduled = await _scheduleService.ScheduleOrUpdateEvent(
                    Data.ScheduledEvent.CreateDailyEvent(
                        Context.Guild.Id,
                        Context.Channel.Id,
                        _readingInfo,
                        eventZonedDateTime
                        ));

                if (rescheduled)
                {
                    await ReplyAsync($"Rescheduled {_readingInfo.Description} posting in this channel to {TextUtilities.FormatLocalTimeAndTimeZone(localEventTime,timeZone)} every day.");
                }
                else
                {
                    await ReplyAsync($"Scheduled {_readingInfo.Description} posting in this channel for {TextUtilities.FormatLocalTimeAndTimeZone(localEventTime, timeZone)} every day.");
                }

            }

            //[Command("cancel")]
            //[Summary("Cancel daily posting of {readings} in the channel this command is called from.")]
            public virtual async Task Cancel()
            {
                bool deleted = await _scheduleService.DeleteScheduledEvent(Context.Guild.Id, Context.Channel.Id, _readingInfo);
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

            public LivesModule(
                SchedulingService scheduleService,
                GuildService guildService,
                ReadingsPostingService bulkPoster,
                IClock clock) : base(scheduleService, guildService, bulkPoster, clock, new SaintsLivesReadingInfo())
            { }

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
