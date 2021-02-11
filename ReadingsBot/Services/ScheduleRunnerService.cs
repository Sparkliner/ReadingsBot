using System;
using System.Collections.Generic;
using System.Threading;
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
        }

        public void Initialize()
        {
            ScheduleRunnerThread = new Thread(ScheduleRunnerThread_Func);
            ScheduleRunnerThread.Start();
        }

        private void ScheduleRunnerThread_Func()
        {
            while (_client.ConnectionState != ConnectionState.Disconnecting)
            {
                if (_client.ConnectionState != ConnectionState.Connected)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                Thread.Sleep(1*60*1000); //sleep for a minute
                LogUtilities.WriteLog(LogSeverity.Verbose, "Polling event schedule");

                var eventresult = _schedulingService.GetCurrentEvents(DateTime.UtcNow.TimeOfDay);
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
                            switch (scheduledEvent.EventType)
                            {
                                case SchedulingService.EventType.OCALives:
                                    var result = _readingsPoster.PostReadings(
                                        ReadingsPostingService.ReadingType.OCALives,
                                        scheduledEvent.ChannelId
                                        );
                                    result.Wait();
                                    break;
                            }
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
            }
        }
    }
}
