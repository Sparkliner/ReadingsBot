using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ReadingsBot
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(string[] args)
        {
            var root = System.IO.Directory.GetCurrentDirectory();
            var dotenv = string.Join("/", root, ".env");
            EnvironmentUtility.Load(dotenv);

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory);
            builder.AddEnvironmentVariables("READINGSBOT_");
            Configuration = builder.Build();
        }

        public static async Task RunAsync(string[] args)
        {
            var startup = new Startup(args);
            await startup.RunAsync();
        }

        public async Task RunAsync()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<LoggingService>();
            provider.GetRequiredService<CommandHandler>();

            await provider.GetRequiredService<StartupService>().StartAsync();
            provider.GetRequiredService<ScheduleRunnerService>().Initialize();
            await Task.Delay(-1);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000
            }))
            .AddSingleton(new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async
            }))
            .AddSingleton(Configuration)
            .AddSingleton<IMongoClient>(s => new MongoClient(Configuration["database_uri"]))
            .AddSingleton<CommandHandler>()
            .AddSingleton<StartupService>()
            .AddSingleton<LoggingService>()
            .AddSingleton<GuildService>()
            .AddSingleton<OCALivesCacheService>()
            .AddSingleton<ReadingsPostingService>()
            .AddSingleton<SchedulingService>()
            .AddSingleton<ScheduleRunnerService>();
        }
    }
}
