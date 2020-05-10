using System.Collections.Generic;
using FileDedupe.IndexFile;

namespace FileDedupe.Analysis
{
    public class DuplicatedFiles
    {
        public string Etag { get; set; }
        public long Count { get; set; }
        public long TotalSize { get; set; }
        public long IndividualSize { get; set; }
        public long PotentialSaving => TotalSize - IndividualSize;
        public List<IndexedFile> Files { get; set; }
    }
}
