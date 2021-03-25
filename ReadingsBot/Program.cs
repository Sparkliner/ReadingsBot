using System.Threading.Tasks;

namespace ReadingsBot
{
    static class Program
    {
        public static Task Main(string[] args)
            => Startup.RunAsync(args);
    }
}
