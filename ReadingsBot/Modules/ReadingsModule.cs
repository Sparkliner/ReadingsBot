using Discord;
using Discord.Commands;
using NodaTime;
using ReadingsBot.Data;
using ReadingsBot.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ReadingsBot.Modules
{
    [Name("Readings")]
    [Summary("Commands related to specific readings.")]
    [RequireContext(ContextType.Guild, Group = "Context")]
    [RequireUserPermission(ChannelPermission.ManageMessages, Group = "Permission")]
    public class ReadingsModule : ModuleBase<SocketCommandContext>
    {

        private static readonly Color _color = new Color(135, 216, 112);

        public static Color GetColor()
        {
            return _color;
        }

        //class for content that updates exactly once per day
        public abstract class DailyPostModule : ModuleBase<SocketCommandContext>
        {
            protected readonly SchedulingService _scheduleService;
            protected readonly GuildService _guildService;
            protected readonly ReadingsPostingService _readingsPoster;
            protected readonly IClock _clock;

            protected readonly IReadingInfo _readingInfo;

            protected DailyPostModule(
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
            public virtual async Task ScheduleAsync([Remainder][Summary("Formatted as [time] (Optional)<-t [time zone]>")] string time)
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

                bool rescheduled = await _scheduleService.ScheduleOrUpdateEventAsync(
                    Data.ScheduledEvent.CreateDailyEvent(
                        Context.Guild.Id,
                        Context.Channel.Id,
                        _readingInfo,
                        eventZonedDateTime
                        ));

                if (rescheduled)
                {
                    await ReplyAsync($"Rescheduled {_readingInfo.Description} posting in this channel to {TextUtilities.FormatLocalTimeAndTimeZone(localEventTime, timeZone)} every day.");
                }
                else
                {
                    await ReplyAsync($"Scheduled {_readingInfo.Description} posting in this channel for {TextUtilities.FormatLocalTimeAndTimeZone(localEventTime, timeZone)} every day.");
                }
            }

            //[Command("cancel")]
            //[Summary("Cancel daily posting of {readings} in the channel this command is called from.")]
            public virtual async Task CancelAsync()
            {
                bool deleted = await _scheduleService.DeleteScheduledEventAsync(Context.Guild.Id, Context.Channel.Id, _readingInfo);
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
            public abstract Task PostNowAsync();
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
            [Summary("Schedule the bot to post the lives of the Saints at a given time of day in the channel this command is called from.")]
            public virtual async Task Schedule([Remainder][Summary("Formatted as [time] (Optional)<-t [time zone]>")] string time)
            {
                await base.ScheduleAsync(time);
            }

            [Command("cancel")]
            [Summary("Cancel daily posting of the lives of the Saints in the channel this command is called from.")]
            public override async Task CancelAsync()
            {
                await base.CancelAsync();
            }

            [Command("now")]
            [Summary("Post today's lives of the Saints right now.")]
            public override async Task PostNowAsync()
            {
                await _readingsPoster.PostLivesAsync(Context.Channel.Id);
            }
        }

        //class for content that updates exactly once per day
        [Group("blogs"), Name("Blog Posting")]
        public class BlogPostModule : ModuleBase<SocketCommandContext>
        {
            protected readonly SchedulingService _scheduleService;
            protected readonly GuildService _guildService;
            protected readonly ReadingsPostingService _readingsPoster;
            protected readonly BlogCacheService _blogService;
            protected readonly IClock _clock;

            public BlogPostModule(
                SchedulingService scheduleService,
                GuildService guildService,
                ReadingsPostingService readingsPoster,
                BlogCacheService blogService,
                IClock clock)
            {
                _scheduleService = scheduleService;
                _guildService = guildService;
                _readingsPoster = readingsPoster;
                _blogService = blogService;
                _clock = clock;
            }

            [Command("list")]
            [Summary("List blogs that are available to post from the bot.")]
            public async Task ListBlogsAsync()
            {
                var blogList = _blogService.GetBlogList();

                var builder = new EmbedBuilder()
                {
                    Color = _color,
                };

                if (blogList is null || blogList.Count == 0)
                {
                    builder.WithDescription($"There are no available blogs.");
                }
                else
                {
                    builder.WithDescription($"Here are the available blogs");

                    foreach (BlogDescription blogInfo in blogList.OrderBy(x => x.BlogName))
                    {
                        builder.AddField(x =>
                        {
                            x.Name = blogInfo.BlogName;
                            x.Value = $"By {blogInfo.Author}\n" +
                            $"Aliases:\n\u2003{string.Join("\n\u2003", blogInfo.Aliases)}";
                            x.IsInline = false;
                        }
                        );
                    }
                }

                await ReplyAsync("", false, builder.Build());
            }

            [Command("subscribe")]
            [Summary("Add the given blog to the bot's subscriptions for this channel. Subscriptions are updated once an hour (using server time zone).")]
            public virtual async Task SubscribeAsync([Remainder][Summary("Name of the blog, or an alias")] string blogName)
            {
                //search for the input
                BlogId matchingBlog = _blogService.GetBlogList()
                    .FirstOrDefault(blogInfo =>
                        blogInfo.BlogName.Equals(blogName, StringComparison.OrdinalIgnoreCase)
                        || blogInfo.Aliases.Contains(blogName, StringComparer.OrdinalIgnoreCase))
                    ?.BId ?? default;

                if (matchingBlog == default)
                {
                    await ReplyAsync("Blog name or alias not found in available blogs");
                }
                else
                {
                    //check if this blog is already subscribed to on this channel
                    Data.ScheduledEvent? channelSubEvent = (await _scheduleService.GetGuildEventsAsync(Context.Guild.Id))
                        .FirstOrDefault(evt =>
                            evt.ChannelId == Context.Channel.Id
                            && evt.EventInfo is BlogsReadingInfo);

                    if (channelSubEvent is null)
                    {
                        DateTimeZone timeZone = await _guildService.GetGuildTimeZone(Context.Guild.Id);

                        ZonedClock zc = new ZonedClock(_clock, timeZone, CalendarSystem.Iso);
                        LocalDateTime localEventDateTime = zc.GetCurrentLocalDateTime();
                        localEventDateTime = localEventDateTime.Date + LocalTime.FromHourMinuteSecondTick(localEventDateTime.Hour, 0, 0, 0);

                        ZonedDateTime eventZonedDateTime = localEventDateTime.InZoneLeniently(timeZone);
                        channelSubEvent = new Data.ScheduledEvent(
                            Context.Guild.Id,
                            Context.Channel.Id,
                            new BlogsReadingInfo(),
                            eventZonedDateTime,
                            Period.FromHours(1),
                            isRecurring: true
                            );
                    }
                    else if (((BlogsReadingInfo)channelSubEvent.EventInfo).Subscriptions.Select(sub => sub.BId).Contains(matchingBlog))
                    {
                        await ReplyAsync("This blog is already subscribed to on this channel");
                        return;
                    }

                    ((BlogsReadingInfo)channelSubEvent.EventInfo).Subscriptions.Add((matchingBlog, default));

                    await _scheduleService.ScheduleOrUpdateEventAsync(channelSubEvent);

                    await ReplyAsync($"Added {matchingBlog.BlogName} to the subscriptions for this channel.");
                }
            }

            [Command("cancel")]
            [Summary("Remove the given blog from the bot's subscriptions in this channel.")]
            public virtual async Task Cancel([Remainder][Summary("Name of the blog, or an alias")] string blogName)
            {
                //search for the input
                BlogId matchingBlog = _blogService.GetBlogList()
                    .FirstOrDefault(
                        blogInfo => blogInfo.BlogName.Equals(blogName, StringComparison.OrdinalIgnoreCase) 
                        || blogInfo.Aliases.Contains(blogName, StringComparer.OrdinalIgnoreCase))
                    ?.BId ?? default;

                if (matchingBlog == default)
                {
                    await ReplyAsync("Blog name or alias not found in available blogs");
                }
                else
                {
                    //check if this blog is already subscribed to on this channel
                    Data.ScheduledEvent? channelSubEvent = (await _scheduleService.GetGuildEventsAsync(Context.Guild.Id)).FirstOrDefault(evt => evt.ChannelId == Context.Channel.Id && evt.EventInfo is BlogsReadingInfo);

                    if (channelSubEvent is null)
                    {
                        await ReplyAsync("There are no blog subscriptions on this channel");
                    }
                    else
                    {
                        bool existed = ((BlogsReadingInfo)channelSubEvent.EventInfo).Subscriptions.RemoveAll(sub => sub.BId == matchingBlog) > 0;

                        if (existed)
                        {
                            if (!((BlogsReadingInfo)channelSubEvent.EventInfo).Subscriptions.Any())
                            {
                                await _scheduleService.DeleteScheduledEventAsync(Context.Guild.Id, Context.Channel.Id, channelSubEvent.EventInfo as BlogsReadingInfo);
                            }
                            else
                            {
                                await _scheduleService.ScheduleOrUpdateEventAsync(channelSubEvent);
                            }

                            await ReplyAsync($"Removed {matchingBlog.BlogName} from the subscriptions for this channel.");
                        }
                        else
                        {
                            await ReplyAsync("This blog is not subscribed to on this channel");
                        }
                    }
                }
            }

            [Command("now")]
            [RequireOwner]
            [Summary("Testing only: Post all cached blogs without checking for updates")]
            public async Task TestAsync()
            {
                await _readingsPoster.PostBlogsAsync(Context.Channel.Id);
            }
        }
    }
}
