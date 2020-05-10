using System;
using System.Collections.Generic;
using System.Linq;
using FileDedupe.IndexFile;

namespace FileDedupe.Analysis
{
    public class AnalysedDirectoryPair : DirectoryPair
    {
        public List<IndexedFile> Directory1OnlyFiles { get; set; }
        public List<IndexedFile> Directory2OnlyFiles { get; set; }
        public Dictionary<string, List<IndexedFile>> CommonFiles { get; set; }

        public long CommonFilesSize { get; set; }
        public long Directory1OnlyFilesSize { get; set; }
        public long Directory2OnlyFilesSize { get; set; }

        public long PotentialSaving { get; set; }
    }

    public class DirectoryPair
    {
        public string Directory1 { get; set; }
        public string Directory2 { get; set; }

        public string UniqueKey => $"[{Directory1}]-[{Directory2}]";
    }

    public class DirectoryAnalyser
    {
        public List<AnalysedDirectoryPair> AnalyseDirectories(IndexFile.Index index, int maxResults)
        {
            var groupedFiles = index.IndexedFiles
                .Select(ix => ix.Value)
                .GroupBy(f => f.FileId)
                .ToList();

            Console.WriteLine($"Found {groupedFiles.Count} Files with duplicates");

            var directoryPairs = groupedFiles
                .Where(g => g.Count() < 100)
                .SelectMany(g => GetDirectoryPairs(g))
                .ToList();

            Console.WriteLine($"Found {groupedFiles.Count} Files with duplicates");

            var dedupedDirectoryPairs = directoryPairs
                .GroupBy(dp => dp.UniqueKey)
                .Select(g => g.First())
                .ToList();

            var directoriesToAnalyse = dedupedDirectoryPairs
                .Select(dp => AnalyseDirectoryPair(index, dp))
                .OrderByDescending(dp => dp.PotentialSaving)
                .Take(maxResults)
                .ToList();

            return directoriesToAnalyse;
        }

        private AnalysedDirectoryPair AnalyseDirectoryPair(IndexFile.Index index, DirectoryPair directoryPair)
        {
            var d1Files = index.DirectoryFiles[directoryPair.Directory1];
            var d2Files = index.DirectoryFiles[directoryPair.Directory2];


            var d1FileIds = d1Files.Select(f => f.FileId).ToList();
            var d2FileIds = d2Files.Select(f => f.FileId).ToList();

            var d1OnlyFiles = d1Files.Where(f => !d2FileIds.Contains(f.FileId)).ToList();
            var d2OnlyFiles = d2Files.Where(f => !d1FileIds.Contains(f.FileId)).ToList();

            var commonFiles = d1Files.Union(d2Files)
                .GroupBy(f => f.FileId)
                .Where(g => g.Count() > 1)
                .ToDictionary(g => g.Key, g => g.ToList());

            var totalCommonFileSize = commonFiles.Sum(g => g.Value.Sum(f => f.Size));
            var uniqueCommonFileSize = commonFiles.Sum(g => g.Value.First().Size);

            return new AnalysedDirectoryPair
            {
                Directory1 = directoryPair.Directory1,
                Directory2 = directoryPair.Directory2,

                Directory1OnlyFiles = d1OnlyFiles,
                Directory2OnlyFiles = d2OnlyFiles,
                CommonFiles = commonFiles,

                Directory1OnlyFilesSize = d1OnlyFiles.Sum(f => f.Size),
                Directory2OnlyFilesSize = d2OnlyFiles.Sum(f => f.Size),
                CommonFilesSize = totalCommonFileSize,

                PotentialSaving = totalCommonFileSize - uniqueCommonFileSize
            };
        }

        private class DirectoryPair
        {
            public string Directory1 { get; set; }
            public string Directory2 { get; set; }

            public string UniqueKey => $"[{Directory1}]-[{Directory2}]";
        }

        private IEnumerable<DirectoryPair> GetDirectoryPairs(IGrouping<string, IndexedFile> grouping)
        {
            var directories = grouping.Select(g => g.Directory).ToList();
            // Console.WriteLine($"Directory Count: {directories.Count}");

            var result = new List<DirectoryPair>();

            foreach(var d1 in directories)
            {
                foreach (var d2 in directories)
                {
                    if (LessThan(d1, d2))
                    {
                        result.Add(new DirectoryPair
                        {
                            Directory1 = d1,
                            Directory2 = d2
                        });
                    }
                }
            }

            return result;
        }

        private bool LessThan(string s1, string s2)
        {
            var array1 = s1.ToCharArray();
            var array2 = s2.ToCharArray();

            var i = 0;
            while (i < array1.Length && i < array2.Length)
            {
                var c1 = array1[i];
                var c2 = array2[i];

                if (c1 < c2)
                {
                    return true;
                }

                if (c2 > c1)
                {
                    return false;
                }

                i += 1;
            }

            return array1.Length < array2.Length;
        }

        private void AnalyseDirectories2(IEnumerable<DuplicatedFiles> duplicates)
        {
            var directories = duplicates
                .SelectMany(g =>
                    g.Files
                    .Select(f => new AnalysedDirectory
                    {
                        Directory = f.Directory,
                        Size = f.Size,
                        Key = f.Key,
                        Duplicates = g.Files.Where(gf => gf.Key != f.Key).ToList()
                    })
                )
                .GroupBy(f => f.Directory)
                .ToList();

            Analyse(directories);
        }

        private void AnalyseDirectories(IEnumerable<DuplicatedFiles> groupedByEtag)
        {
            var directories = groupedByEtag
                .SelectMany(g =>
                    g.Files
                    .Select(f => new AnalysedDirectory
                    {
                        Directory = f.Directory,
                        Size = f.Size,
                        Key = f.Key,
                        Duplicates = g.Files.Where(gf => gf.Key != f.Key).ToList()
                    })
                )
                .GroupBy(f => f.Directory)
                .ToList();

            Analyse(directories);
        }

        private class AnalysedDirectory
        {
            public string Directory { get; set; }
            public long Size { get; set; }
            public string Key { get; set; }
            public string Name { get; set; }
            public List<IndexedFile> Duplicates { get; set; }
        }

        private void Analyse(List<IGrouping<string, AnalysedDirectory>> directories)
        {
            foreach(var directory in directories)
            {
                var otherDirectories = directory
                    .Select(x => x.Duplicates)
                    .SelectMany(d => d.Select(dup => new AnalysedDirectory
                    {
                        Directory = dup.Directory,
                        Size = dup.Size,
                        Key = dup.Key,
                        Name = dup.Name
                    }))
                    .GroupBy(directory => directory.Directory)
                    .Where(g => g.Sum(f => f.Size) > 1.GB())
                    .OrderByDescending(g => g.Sum(f => f.Size))
                    .Take(2);

                foreach(var otherDirectory in otherDirectories)
                {
                    Console.WriteLine($"{otherDirectory.Sum(f => f.Size).ToFileSize()} {otherDirectory.Count()} files: {directory.Key} <-> {otherDirectory.Key}");
                    foreach(var file in otherDirectory.OrderByDescending(f => f.Size).Take(10))
                    {
                        Console.WriteLine($"\t/{file.Name}: {file.Size}");
                    }
                }
            }
        }
    }
}
