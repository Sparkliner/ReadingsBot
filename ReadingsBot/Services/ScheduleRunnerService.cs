using Discord;
using Discord.WebSocket;
using ReadingsBot.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReadingsBot
{
    class ScheduleRunnerService
    {
        private readonly DiscordSocketClient _client;
        private readonly SchedulingService _schedulingService;
        private readonly ReadingsPostingService _readingsPoster;

        private Task ScheduleRunnerTask;
        private CancellationTokenSource cancellationToken;

        public ScheduleRunnerService(DiscordSocketClient client, SchedulingService schedulingService, ReadingsPostingService bulkPoster)
        {
            _client = client;
            _schedulingService = schedulingService;
            _readingsPoster = bulkPoster;

            _client.Connected += Initialize;
        }

        public Task Initialize()
        {
            if (ScheduleRunnerTask is null || ScheduleRunnerTask.IsCanceled || ScheduleRunnerTask.IsFaulted)
            {
                cancellationToken = new CancellationTokenSource();
                ScheduleRunnerTask = Task.Run(async () => await ScheduleRunnerAsync());
            }
            
            return Task.CompletedTask;
        }

        private async Task ScheduleRunnerAsync()
        {
            LogUtilities.WriteLog(LogSeverity.Verbose, "Schedule Thread Started");
            while (true) //since thread is background this should be ok now
            {
                if (_client.ConnectionState != ConnectionState.Connected)
                {
                    await Task.Delay(1000, cancellationToken.Token);
                    continue;
                }
                LogUtilities.WriteLog(LogSeverity.Verbose, "Polling event schedule");
                
                List<Data.ScheduledEvent> currentEvents = await _schedulingService.GetCurrentEventsAsync();

                if (!(currentEvents is null) && currentEvents.Any())
                {
                    LogUtilities.WriteLog(LogSeverity.Verbose, $"Found {currentEvents.Count} events");
                    List<Task> ts = new List<Task>();
                    foreach (Data.ScheduledEvent scheduledEvent in currentEvents)
                    {
                        Task nextTask;
                        switch (scheduledEvent.EventInfo)
                        {
                            case SaintsLivesReadingInfo:
                                nextTask = _readingsPoster.PostLivesAsync(scheduledEvent.ChannelId);
                                break;
                            case BlogsReadingInfo blogsReading:
                                var blogsTask = _readingsPoster.PostBlogsAsync(scheduledEvent.ChannelId, blogsReading);
                                //get new subs from result and update blog subscriptions
                                nextTask = blogsTask.ContinueWith(previousTask => 
                                    ((BlogsReadingInfo)scheduledEvent.EventInfo).Subscriptions = previousTask.Result);
                                break;
                            default:
                                nextTask = default;
                                break;
                        }
                        if (nextTask != default)
                        {
                            ts.Add(nextTask.ContinueWith(previousTask => _schedulingService.UpdateEventDataAsync(scheduledEvent)));
                        }
                    }
                    await Task.WhenAll(ts.ToArray());
                    LogUtilities.WriteLog(LogSeverity.Verbose, $"Executed all events");
                }
                else
                {
                    LogUtilities.WriteLog(LogSeverity.Verbose, $"No current events found");
                }
                await Task.Delay(1 * 60 * 1000, cancellationToken.Token); //sleep for a minute
            }
        }
    }
}
