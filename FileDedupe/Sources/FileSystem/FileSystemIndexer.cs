using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FileDedupe.IndexFile;
using System.Threading.Tasks;
using FileDedupe.Logging;

namespace FileDedupe.Sources.FileSystem
{
    public class FileSystemIndexer : IIndexer
    {
        private readonly ILogger _logger;

        public FileSystemIndexer(ILogger logger)
        {
            _logger = logger;
        }
        /*
        public static void Calculate(string directoryPath, string outputFilename, Dictionary<string, IndexedFile> existingEtags)
        {
            var indexedFiles = IndexNewFiles(directoryPath, existingEtags);
            IndexWriter.WriteIndex(outputFilename, indexedFiles);
        }
        */
        public async Task<IEnumerable<IndexedFile>> IndexNewFilesAsync(ISource source, IndexFile.Index index)
        {
            if (!(source is FileSystemSource))
            {
                throw new Exception("source should be an S3Source");
            }

            var fileSystemSource = source as FileSystemSource;

            var directoryPath = fileSystemSource.Path;
            var existingEtags = index.IndexedFiles;
            var files = GetRecursiveFiles(directoryPath);

            var result = new List<IndexedFile>();
            var verify = false;
            foreach (var file in files)
            {
                try
                {
                    var filename = file.FullName;//.Substring(directoryPath.Length + 1);
                    if (existingEtags.TryGetValue(filename, out var s3File))
                    {
                        if (verify)
                        {
                            var md5 = await CalculateETag(file);
                            var matches = md5.Equals(s3File.Etag);
                            var matchText = matches ?
                                $"OK - {md5}" :
                                $"FAIL - S3 {s3File.Etag} - Local {md5}";
                            Console.WriteLine($"Verifying {filename} - {matchText}");
                        }
                    }
                    else
                    {
                        var invalidCharacters = new char[]
                        {
                            '\r',
                            '\n'
                        };

                        if (invalidCharacters.Any(c => filename.Contains(c)))
                        {
                            _logger.Warn($"Skipping file, Filename contains an invalid character: [{filename}]");
                        }
                        else
                        {
                            var etag = await CalculateETag(file);
                            result.Add(new IndexedFile(filename, etag, file.Length));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception indexing \"{file.FullName}\" {ex.Message}");
                }
            }

            return result;
        }


        private static string CalculateMD5(FileInfo file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(file.FullName))
                {
                    var md5Bytes = md5.ComputeHash(stream);
                    return ByteArrayToString(md5Bytes);
                }
            }
        }

        private static async Task<string> CalculateETag(FileInfo file)
        {
            int chunkSize = 8 * 1024 * 1024;

            if (file.Length <= chunkSize)
            {
                return CalculateMD5(file);
            }

            var fileReader = File.OpenRead(file.FullName);
            //var fileBytes = File.ReadAllBytes(file.FullName);
            var fileBytes = new byte[chunkSize];
            long hashedBytes = 0;
            var md5s = new List<byte[]>();

            using (var md5 = MD5.Create())
            {
                while (hashedBytes < file.Length)
                {
                    var thisChunk = chunkSize;
                    if (hashedBytes + chunkSize > file.Length)
                    {
                        thisChunk = (int)(file.Length - hashedBytes);
                    }

                    await fileReader.ReadAsync(fileBytes, 0, thisChunk);

                    //var md5Bytes = md5.ComputeHash(fileBytes, (int)hashedBytes, thisChunk);
                    var md5Bytes = md5.ComputeHash(fileBytes, 0, thisChunk);
                    md5s.Add(md5Bytes);

                    hashedBytes += thisChunk;
                }

                var concatenatedMd5 = string.Join("", md5s.Select(ByteArrayToString));

                var concatenatedMd5Bytes = concatenatedMd5
                    .ToArray()
                    .Select(c => (byte)c)
                    .ToArray();

                concatenatedMd5Bytes = md5s.SelectMany(b => b).ToArray();

                var etagMd5 = md5.ComputeHash(concatenatedMd5Bytes);
                var finaleTag = $"{ByteArrayToString(etagMd5)}-{md5s.Count}";
                return finaleTag;
            }
        }

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private static IEnumerable<FileInfo> GetRecursiveFiles(string directoryPath)
        {
            return Directory.EnumerateFiles(directoryPath, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = true
            }).Select(f => new FileInfo(f));
        }
    }
}
