using System.Collections.Generic;
using System.Linq;
using FileDedupe.IndexFile;

namespace FileDedupe.Analysis
{
    public class Duplicates
    {
        public static IEnumerable<DuplicatedFiles> FindDuplicateFiles(Index index, int? maxResults = null)
        {
            var files = index.IndexedFiles.Values;

            IEnumerable<DuplicatedFiles> result = files
                .GroupBy(f => f.FileId)
                .Select(MapGrouping)
                .Where(g => g.Count > 1)
                .OrderByDescending(g => g.PotentialSaving);

            if (maxResults.HasValue)
            {
                result = result.Take(maxResults.Value);
            }

            return result;
        }

        private static DuplicatedFiles MapGrouping(IGrouping<string, IndexedFile> grouping)
        {
            var result = new DuplicatedFiles
            {
                Count = grouping.Count(),
                Etag = grouping.First().Etag,
                Files = grouping.ToList(),
                TotalSize = grouping.Sum(f => f.Size),
                IndividualSize = grouping.First().Size
            };

            return result;
        }
    }
}
