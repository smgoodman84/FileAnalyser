using System.Collections.Generic;
using System.Threading.Tasks;
using FileDedupe.IndexFile;

namespace FileDedupe.Sources
{
    public interface IIndexer
    {
        Task<IEnumerable<IndexedFile>> IndexNewFilesAsync(ISource source, IndexFile.Index index);
    }
}
