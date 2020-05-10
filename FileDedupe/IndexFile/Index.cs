using System.Collections.Generic;

namespace FileDedupe.IndexFile
{
    public class Index
    {
        public Dictionary<string, IndexedFile> IndexedFiles { get; set; }
        public Dictionary<string, List<IndexedFile>> DirectoryFiles { get; set; }
    }
}
