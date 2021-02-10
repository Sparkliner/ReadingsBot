using System.Threading.Tasks;

namespace ReadingsBot
{
    class Program
    {
        public static Task Main(string[] args)
            => Startup.RunAsync(args);
    }
}
