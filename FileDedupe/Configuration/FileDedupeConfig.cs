using System.Collections.Generic;
using FileDedupe.Sources.FileSystem;
using FileDedupe.Sources.S3;

namespace FileDedupe.Configuration
{
    public class FileDedupeConfig
    {
        public bool BackupIndex { get; set; }
        public string IndexFile { get; set; }
        public IEnumerable<FileSystemSource> FileSystemSources { get; set; }
        public IEnumerable<S3Source> S3Sources { get; set; }
    }
}
