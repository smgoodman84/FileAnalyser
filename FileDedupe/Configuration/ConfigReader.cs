using System.IO;
using System.Text.Json;

namespace FileDedupe.Configuration
{
    public class ConfigReader
    {
        public static FileDedupeConfig LoadFromFile(string filename)
        {
            var fileContents = File.ReadAllText(filename);
            var config = JsonSerializer.Deserialize<FileDedupeConfig>(fileContents);
            return config;
        }
    }
}
