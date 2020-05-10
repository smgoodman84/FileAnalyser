using System;

namespace FileDedupe.Logging
{
    public class ConsoleLogger : ILogger
    {
        public void Error(string message)
        {
            Console.WriteLine($"Error: {message}");
        }

        public void Warn(string message)
        {
            Console.WriteLine($"Warning: {message}");
        }
    }
}
