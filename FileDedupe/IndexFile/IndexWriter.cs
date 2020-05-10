using System.Collections.Generic;
using System.IO;

namespace FileDedupe.IndexFile
{
    public class IndexWriter
    {
        public void WriteIndex(string outputFilename, IEnumerable<IndexedFile> index)
        {
            var outputFileWriter = File.AppendText(outputFilename);
            foreach (var indexedFile in index)
            {
                var output = indexedFile.ToString();
                outputFileWriter.WriteLine(output);
                outputFileWriter.Flush();
            }
        }
    }
}
