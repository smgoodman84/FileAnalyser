using System;
using FileDedupe.Logging;
using FileDedupe.Sources.FileSystem;
using FileDedupe.Sources.S3;

namespace FileDedupe.Sources
{
    public class IndexerFactory
    {
        private readonly ILogger _logger;

        public IndexerFactory(ILogger logger)
        {
            _logger = logger;
        }

        public IIndexer GetIndexer(ISource source)
        {
            if (source is FileSystemSource)
            {
                return new FileSystemIndexer(_logger);
            }

            if (source is S3Source)
            {
                return new S3Indexer();
            }

            throw new Exception("Unknown source type");
        }
    }
}
