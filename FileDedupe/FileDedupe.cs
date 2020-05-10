using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileDedupe.Analysis;
using FileDedupe.Configuration;
using FileDedupe.IndexFile;
using FileDedupe.Logging;
using FileDedupe.Sources;

namespace FileDedupe
{
    public class FileDedupe
    {
        private IndexFile.Index _index;

        private readonly FileDedupeConfig _fileDedupeConfig;
        private readonly IndexedFileParser _indexedFileParser;
        private readonly DirectoryAnalyser _directoryAnalyser;
        private readonly IndexWriter _indexWriter;
        private readonly IndexerFactory _indexerFactory;
        private readonly IndexMerger _indexMerger;
        private readonly ILogger _logger;

        public FileDedupe(
            FileDedupeConfig fileDedupeConfig,
            IndexedFileParser indexedFileParser,
            DirectoryAnalyser dedupeAnalyser,
            IndexWriter indexWriter,
            IndexerFactory indexerFactory,
            IndexMerger indexMerger,
            ILogger logger)
        {
            _fileDedupeConfig = fileDedupeConfig;
            _indexedFileParser = indexedFileParser;
            _directoryAnalyser = dedupeAnalyser;
            _indexWriter = indexWriter;
            _indexerFactory = indexerFactory;
            _indexMerger = indexMerger;
            _logger = logger;

            Console.WriteLine($"Loading Index: {_fileDedupeConfig.IndexFile}");
            _index = indexedFileParser.Parse(_fileDedupeConfig.IndexFile);
            Console.WriteLine($"Loaded Index: {_fileDedupeConfig.IndexFile}");
        }

        public async Task Reindex()
        {
            var sources = _fileDedupeConfig
                .FileSystemSources
                .OfType<ISource>()
                .Concat(
                    _fileDedupeConfig
                    .S3Sources
                    .OfType<ISource>()
                    );

            foreach (var source in sources.Where(s => s.Reindex))
            {
                var indexer = _indexerFactory.GetIndexer(source);

                var additionalFiles = await indexer.IndexNewFilesAsync(source, _index);

                foreach (var file in additionalFiles)
                {
                    _index.IndexedFiles.Add(file.Key, file);
                }
            }

            if (File.Exists(_fileDedupeConfig.IndexFile))
            {
                if (_fileDedupeConfig.BackupIndex)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    File.Copy(_fileDedupeConfig.IndexFile, $"{_fileDedupeConfig.IndexFile}.{timestamp}.bak");
                }

                File.Delete(_fileDedupeConfig.IndexFile);
            }

            _indexWriter.WriteIndex(_fileDedupeConfig.IndexFile, _index.IndexedFiles.Values);
        }

        public void MergeIndexes(string newIndex, params string[] indexFiles)
        {
            _indexMerger.MergeIndexes(newIndex, indexFiles);
        }

        public void AnalyseDirectories()
        {
            var maxResults = 100;

            var directoryPairs = _directoryAnalyser.AnalyseDirectories(_index, maxResults);

            foreach (var directoryPair in directoryPairs)
            {
                Console.WriteLine();
                Console.WriteLine($"{directoryPair.Directory1}: {directoryPair.Directory1OnlyFilesSize.ToFileSize()}");
                Console.WriteLine($"{directoryPair.Directory2}: {directoryPair.Directory2OnlyFilesSize.ToFileSize()}");
                Console.WriteLine($"Potential Saving: {directoryPair.PotentialSaving.ToFileSize()}");

                var filesToShow = directoryPair.CommonFiles
                    .OrderByDescending(f => f.Value.First().Size)
                    .Take(10)
                    .ToList();

                foreach (var file in filesToShow)
                {
                    var fileNames = file.Value
                        .Select(fn => fn.Key.Substring(fn.Key.LastIndexOf('/') + 1))
                        .ToList();

                    var allFileNames = string.Join(", ", fileNames);
                    Console.WriteLine($"\t {file.Value.First().Size.ToFileSize()}: {allFileNames}");
                }
            }
        }

        public void AnalyseDuplicates()
        {
            var maxResults = 100;
            var duplicates = Duplicates.FindDuplicateFiles(_index, maxResults);

            foreach (var group in duplicates)
            {
                Console.WriteLine($"{group.Etag} - {group.Count} files, Total: {group.TotalSize.ToFileSize()}, Individual: {group.IndividualSize.ToFileSize()}, PotentialSaving: {group.PotentialSaving.ToFileSize()}");
                foreach (var file in group.Files.Take(3))
                {
                    Console.WriteLine($"\t{file.Key}:");
                }
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine($"Total Savings: {duplicates.Sum(g => g.PotentialSaving).ToFileSize()}");
        }

        public void AnalyseDuplicatesWithinDirectories()
        {
            var maxResults = 100;
            var duplicates = DuplicatesWithinDirectories.FindDuplicatesWithinDirectories(_index, maxResults);

            Console.WriteLine($"TOP {maxResults} Duplicate files within a directory");
            foreach (var group in duplicates)
            {
                Console.WriteLine($"{group.PotentialSaving.ToFileSize()} {group.Duplicates.First().Directory}");
                foreach (var file in group.Duplicates.Take(10))
                {
                    Console.WriteLine($"\t/{file.Name} - {file.Size.ToFileSize()}");
                }
            }
        }
    }
}
