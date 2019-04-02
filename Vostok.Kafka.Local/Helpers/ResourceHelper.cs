using System;
using System.IO;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

// ReSharper disable InconsistentNaming

namespace Vostok.Kafka.Local.Helpers
{
    internal static class ResourceHelper
    {
        public static void ExtractResource<T>(string name, string destinationPath)
        {
            var assembly = typeof(T).Assembly;
            using (var stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    throw new Exception(
                        $"Resource {name} not found in {assembly.FullName}.  Valid resources are: {string.Join(", ", assembly.GetManifestResourceNames())}.");
                }

                ExtractTGZ(stream, destinationPath);
            }
        }

        private static void ExtractTGZ(Stream archiveStream, string destinationPath)
        {
            using (var gzipStream = new GZipInputStream(archiveStream))
            using (var tarArchive = TarArchive.CreateInputTarArchive(gzipStream))
            {
                tarArchive.ExtractContents(destinationPath);
            }
        }
    }
}