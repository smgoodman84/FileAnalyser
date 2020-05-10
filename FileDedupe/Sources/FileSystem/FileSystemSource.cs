namespace FileDedupe.Sources.FileSystem
{
    public class FileSystemSource : ISource
    {
        public bool Reindex { get; set; } = true;
        public string Path { get; set; }
    }
}
