using System;
using System.Collections.Generic;
using System.Linq;
using FileDedupe.IndexFile;

namespace FileDedupe.Analysis
{
    public class DuplicatesWithinDirectory
    {
        public string Directory { get; set; }
        public string Etag { get; set; }
        public long Size;
        public long PotentialSaving;
        public List<IndexedFile> Duplicates {get;set;}
    }

    public class DuplicatesWithinDirectories
    {
        public static IEnumerable<DuplicatesWithinDirectory> FindDuplicatesWithinDirectories(IndexFile.Index index, int maxResults)
        {
            var files = index.IndexedFiles.Values;

            var groupedByEtagAndDirectory = files
                .GroupBy(f => $"{f.FileId}-{f.Directory}")
                .Where(g => g.Count() > 1)
                .Select(MapGrouping)
                .OrderByDescending(g => g.PotentialSaving)
                .Take(maxResults);

            return groupedByEtagAndDirectory;
        }

        public static DuplicatesWithinDirectory MapGrouping(IGrouping<string, IndexedFile> grouping)
        {
            var first = grouping.First();

            var result = new DuplicatesWithinDirectory
            {
                Directory = first.Directory,
                Etag = first.Etag,
                Duplicates = grouping.ToList(),
                Size = first.Size,
                PotentialSaving = grouping.Sum(f => f.Size) - first.Size
            };

            return result;
        }
    }
}
