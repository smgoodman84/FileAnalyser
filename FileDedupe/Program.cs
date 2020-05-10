using System.Threading.Tasks;
using FileDedupe.Analysis;
using FileDedupe.Configuration;
using FileDedupe.IndexFile;
using FileDedupe.Logging;
using FileDedupe.Sources;

namespace FileDedupe
{
    class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            var config = ConfigReader.LoadFromFile("config.json");

            var logger = new ConsoleLogger();
            var dedupeAnalyser = new DirectoryAnalyser();
            var indexWriter = new IndexWriter();

            var parser = new IndexedFileParser(logger);
            var indexerFactory = new IndexerFactory(logger);
            var indexMerger = new IndexMerger(parser, indexWriter, logger);

            var fileDedupe = new FileDedupe(
                config,
                parser,
                dedupeAnalyser,
                indexWriter,
                indexerFactory,
                indexMerger,
                logger);

            fileDedupe.AnalyseDirectories();
        }
    }
}
