using System;
using System.Linq;
using System.Threading.Tasks;

namespace Atomic.SerialToIp
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1 || string.IsNullOrEmpty(args[0]))
                Console.Error.WriteLine("Missing config file argument");

            var tasks = ConfigParser.GetPorts(args[0]).Select(port => Task.Run(async () =>
            {
                var sp = new SerialIpServer();
                await sp.RunServer(port);
            }));

            Task.WhenAll(tasks).Wait();
        }
    }
}
