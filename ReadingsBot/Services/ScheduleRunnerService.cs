using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Discord;
using Discord.WebSocket;
using System.Linq;

namespace ReadingsBot
{
    class ScheduleRunnerService
    {
        private readonly DiscordSocketClient _client;
        private readonly SchedulingService _schedulingService;
        private readonly ReadingsPostingService _readingsPoster;

        private Thread ScheduleRunnerThread;

        public ScheduleRunnerService(DiscordSocketClient client, SchedulingService schedulingService, ReadingsPostingService bulkPoster)
        {
            _client = client;
            _schedulingService = schedulingService;
            _readingsPoster = bulkPoster;

            _client.Connected += Initialize;
        }

        public Task Initialize()
        {
            return Task.Run(() =>
            {
                if (ScheduleRunnerThread is null || !ScheduleRunnerThread.IsAlive)
                {
                    ScheduleRunnerThread = new Thread(ScheduleRunnerThread_Func);
                    ScheduleRunnerThread.IsBackground = true;
                    ScheduleRunnerThread.Start();
                }
            });
        }

        private void ScheduleRunnerThread_Func()
        {
            LogUtilities.WriteLog(LogSeverity.Verbose, "Schedule Thread Started");
            while (true) //since thread is background this should be ok now
            {
                if (_client.ConnectionState != ConnectionState.Connected)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                LogUtilities.WriteLog(LogSeverity.Verbose, "Polling event schedule");

                var eventresult = _schedulingService.GetCurrentEvents();
                eventresult.Wait();
                List<Data.ScheduledEvent> currentEvents = eventresult.Result;
                if (!(currentEvents is null) && currentEvents.Any())
                {
                    LogUtilities.WriteLog(LogSeverity.Verbose, $"Found {currentEvents.Count} events");
                    List<Thread> ts = new List<Thread>();
                    foreach (Data.ScheduledEvent scheduledEvent in currentEvents)
                    {
                        ts.Add(new Thread(() =>
                        {
                            switch (scheduledEvent.EventInfo)
                            {
                                case SaintsLivesReadingInfo:
                                    var result = _readingsPoster.PostLives(scheduledEvent.ChannelId);
                                    result.Wait();
                                    break;
                            }
                            _schedulingService.HandleEventRecurrence(scheduledEvent).Wait();
                        }));
                    }  
                    foreach (Thread t in ts)
                    {
                        t.Start();
                    }
                    LogUtilities.WriteLog(LogSeverity.Verbose, $"Executed all events");
                }
                else
                {
                    LogUtilities.WriteLog(LogSeverity.Verbose, $"No current events found");
                }
                Thread.Sleep(1*60*1000); //sleep for a minute
            }
            //LogUtilities.WriteLog(LogSeverity.Warning, $"ScheduleRunnerThread exiting");
        }
    }
}
