using System.Linq;
using FileDedupe.Logging;

namespace FileDedupe.IndexFile
{
    public class IndexMerger
    {
        private readonly IndexedFileParser _indexedFileParser;
        private readonly IndexWriter _indexWriter;
        private readonly ILogger _logger;

        public IndexMerger(
            IndexedFileParser indexedFileParser,
            IndexWriter indexWriter,
            ILogger logger)
        {
            _indexedFileParser = indexedFileParser;
            _indexWriter = indexWriter;
            _logger = logger;
        }

        public void MergeIndexes(string newIndex, params string[] indexFiles)
        {
            var groupings = indexFiles
                .Select(_indexedFileParser.Parse)
                .SelectMany(index => index.IndexedFiles.Values)
                .GroupBy(f => f.ToString());

            var duplicates = groupings
                .Where(g => g.Count() > 1);

            foreach (var duplicate in duplicates)
            {
                _logger.Warn($"Ignoring duplicate: {duplicate.Key}");
            }

            var files = groupings.Select(g => g.First());

            _indexWriter.WriteIndex(newIndex, files);
        }
    }
}
