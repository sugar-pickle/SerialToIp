using System;
using System.Collections.Generic;
using System.IO;
// ReSharper disable StringLiteralTypo

namespace SerialToIp
{
    public static class ConfigParser
    {
        public static Config Config { get; } = new();
        public static IEnumerable<Port> GetPorts(string configFile)
        {
            Console.WriteLine($"Using config file {configFile}");
            var configLines = File.ReadAllLines(configFile);
            var output = new List<Port>();
            foreach (var line in configLines)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith('#')) continue;

                if (line.ToLower().StartsWith("baudrate") || line.ToLower().StartsWith("storewhendisconnected"))
                {
                    ParseConfig(line);
                    continue;
                }
                
                var split = line.Split('=');
                if (split.Length != 2)
                {
                    Console.Error.WriteLine($"Invalid config string {line}");
                }
                try
                {
                    output.Add(new Port
                    {
                        SerialPort = split[0],
                        IpPort = int.Parse(split[1])
                    });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Exception loading config string {ex.Message}");
                }
            }
            Console.WriteLine($"Loaded {output.Count} ports from config file");
            return output;
        }

        private static void ParseConfig(string configLine)
        {
            var split = configLine.Split('=');
            switch (split[0].ToLower()) {
                case "storewhendisconnected":
                    Config.StoreWhenDisconnected = bool.Parse(split[1]);
                    Console.WriteLine($"StoreWhenDisconnected set to {Config.StoreWhenDisconnected}");
                    break;
                case "baudrate":
                    Config.BaudRate = int.Parse(split[1]);
                    Console.WriteLine($"BaudRate set to {Config.BaudRate}");
                    break;
            }
        }
    }
}
