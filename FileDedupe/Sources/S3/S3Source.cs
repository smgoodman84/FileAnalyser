namespace FileDedupe.Sources.S3
{
    public class S3Source : ISource
    {
        public bool Reindex { get; set; } = true;
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string BucketName { get; set; }
    }
}
