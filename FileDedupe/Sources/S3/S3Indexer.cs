using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FileDedupe.IndexFile;

namespace FileDedupe.Sources.S3
{
    class S3Indexer : IIndexer
    {
        public static async Task DownloadAsync(string[] args)
        {
            // Create a client
            AmazonS3Client client = new AmazonS3Client(
                new BasicAWSCredentials("", ""));

            var now = DateTime.Now.ToString("YYYYmmDDhhMMss");
            var filename = $"s3tags-{now}.csv";

            using (var file = new StreamWriter(filename))
            {
                long requestLimit = 3;
                long requestNumber = 0;
                long itemCount = 0;
                string continuationToken = null;
                do
                {
                    requestNumber += 1;
                    Console.Write($"Request {requestNumber}, {itemCount}: ");

                    var listresponse = await client.ListObjectsV2Async(
                        new ListObjectsV2Request()
                        {
                            BucketName = "",
                            ContinuationToken = continuationToken
                        });

                    var first = true;
                    itemCount += listresponse.KeyCount;
                    foreach (var s3object in listresponse.S3Objects)
                    {
                        var escapedKey = s3object.Key.Replace("\"", "\"\"");
                        var line = $"\"{escapedKey}\", {s3object.ETag}, {s3object.Size}";

                        file.WriteLine(line);
                        if (first)
                        {
                            Console.WriteLine(line);
                            first = false;
                        }
                    }

                    continuationToken = listresponse.NextContinuationToken;
                    //requestLimit -= 1l;
                } while (continuationToken != null && requestLimit >= 0L);
            }
        }

        public async Task<IEnumerable<IndexedFile>> IndexNewFilesAsync(ISource source, IndexFile.Index index)
        {
            if (!(source is S3Source))
            {
                throw new Exception("source should be an S3Source");
            }

            var s3Source = source as S3Source;

            // Create a client
            AmazonS3Client client = new AmazonS3Client(
                new BasicAWSCredentials(s3Source.AccessKey, s3Source.SecretKey));

            var results = new List<IndexedFile>();

            string continuationToken = null;
            do
            {
                var listresponse = await client.ListObjectsV2Async(
                    new ListObjectsV2Request()
                    {
                        BucketName = s3Source.BucketName,
                        ContinuationToken = continuationToken
                    });

                foreach (var s3object in listresponse.S3Objects)
                {
                    if (!index.IndexedFiles.ContainsKey(s3object.Key))
                    {
                        results.Add(new IndexedFile(s3object.Key, s3object.ETag, s3object.Size));
                    }
                }

                continuationToken = listresponse.NextContinuationToken;
            } while (continuationToken != null);

            return results;
        }
    }
}
