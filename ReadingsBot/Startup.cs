using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NodaTime;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace ReadingsBot
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup()
        {
            var root = System.IO.Directory.GetCurrentDirectory();
            var dotenv = string.Join("/", root, ".env");
            EnvironmentUtility.Load(dotenv);

            var builder = new ConfigurationBuilder();
            builder.AddEnvironmentVariables("READINGSBOT_");
            Configuration = builder.Build();
        }

        public static async Task RunAsync(string[] args)
        {
            var startup = new Startup();
            await startup.RunAsync();
        }

        public async Task RunAsync()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<LoggingService>();
            provider.GetRequiredService<CommandHandler>();
            provider.GetRequiredService<ScheduleRunnerService>();

            var productValue = new ProductInfoHeaderValue(
                Assembly.GetExecutingAssembly().GetName().Name,
                Assembly.GetExecutingAssembly().GetName().Version.ToString());
            LogUtilities.WriteLog(LogSeverity.Debug, $"Product Version: {productValue}");
            var commentValue = new ProductInfoHeaderValue(
                "(+https://github.com/Sparkliner/ReadingsBot/)");
            provider.GetRequiredService<HttpClient>()
                .DefaultRequestHeaders
                .UserAgent.Add(productValue);
            provider.GetRequiredService<HttpClient>()
                .DefaultRequestHeaders
                .UserAgent.Add(commentValue);

            await provider.GetRequiredService<StartupService>().StartAsync();
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
            .AddSingleton(new HttpClient())
            .AddSingleton<IMongoClient>(s => new MongoClient(Configuration["database_uri"]))
            .AddSingleton<CommandHandler>()
            .AddSingleton<StartupService>()
            .AddSingleton<LoggingService>()
            .AddSingleton<BlogCacheService>()
            .AddSingleton<IClock>(SystemClock.Instance)
            .AddSingleton<GuildService>()
            .AddSingleton<OcaLivesCacheService>()
            .AddSingleton<ReadingsPostingService>()
            .AddSingleton<SchedulingService>()
            .AddSingleton<ScheduleRunnerService>();
        }
    }
}
