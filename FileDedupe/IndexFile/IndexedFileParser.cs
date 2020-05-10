using System;
using System.IO;
using System.Linq;
using FileDedupe.Logging;

namespace FileDedupe.IndexFile
{
    public class IndexedFileParser
    {
        private readonly ILogger _logger;

        public IndexedFileParser(ILogger logger)
        {
            _logger = logger;
        }

        public Index Parse(string file)
        {
            var indexedFiles = File.ReadAllLines(file)
                .Select((line, lineNumber) => ParseLine(file, line, lineNumber));

            var fileDictionary = indexedFiles
                .ToDictionary(f => f.Key);

            var directoryDictionary = indexedFiles
                .GroupBy(f => f.Directory)
                .ToDictionary(g => g.Key, g => g.ToList());

            var index = new Index
            {
                IndexedFiles = fileDictionary,
                DirectoryFiles = directoryDictionary
            };

            return index;
        }

        private IndexedFile ParseLine(string file, string line, int lineNumber)
        {
            try
            {
                var lastComma = line.LastIndexOf(",");
                var size = line.Substring(lastComma + 1).Trim();
                var keyAndETag = line.Substring(0, lastComma);

                lastComma = keyAndETag.LastIndexOf(",");
                var eTag = keyAndETag.Substring(lastComma + 1).Trim();
                var key = keyAndETag.Substring(0, lastComma).Trim();

                if (key.StartsWith("\"") && key.EndsWith("\""))
                {
                    key = key.Substring(1, key.Length - 2);
                }

                if (eTag.StartsWith("\"") && eTag.EndsWith("\""))
                {
                    eTag = eTag.Substring(1, eTag.Length - 2);
                }

                return new IndexedFile(key, eTag, long.Parse(size));
            }
            catch (Exception ex)
            {
                _logger.Error($"failed to parse '{file}' line {lineNumber}: [{line}] {ex.Message}");
                throw;
            }
        }
    }
}
