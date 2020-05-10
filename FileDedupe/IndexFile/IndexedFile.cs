using System.Collections.Generic;
using System.Linq;

namespace FileDedupe.IndexFile
{
    public class IndexedFile
    {
        public IndexedFile(string key, string etag, long size)
        {
            Key = key;
            Etag = etag;
            Size = size;

            var paths = Key.Split('/').ToList();
            paths.RemoveAt(paths.Count - 1);

            Directory = string.Join('/', paths);
            Name = Key.Substring(Key.LastIndexOf('/') + 1);
            FileId = $"{Size}-{Etag}";
        }

        public string Key { get; private set; }
        public string Etag { get; private set; }
        public long Size { get; private set; }

        public List<string> Parents { get; private set; }

        public string Directory { get; private set; }
        public string Name { get; private set; }
        public string FileId { get; private set; }

        public override string ToString()
        {
            return $@"""{Key}"", ""{Etag}"", {Size}";
        }
    }
}
