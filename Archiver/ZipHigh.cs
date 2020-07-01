namespace Archiver
{
    using System;
    using System.IO;
    using System.IO.Compression;

    public static class ZipHigh
    {
        /// <summary>
        /// Метод для добавления файла в .Zip архив
        /// </summary>
        /// <param name="zipfile">Полный путь к .Zip архиву</param>
        /// <param name="inputfile">Путь к файлу для добавления в .Zip архив</param>
        /// <param name="compressionlevel">Метод сжатия</param>
        public static void AddFileInZip(string zipfile, string inputfile, CompressionLevel compressionlevel)
        {
            try
            {
                using ZipArchive za = ZipFile.Open(zipfile, ZipArchiveMode.Update);
                za?.CreateEntryFromFile(inputfile, Path.GetFileName(inputfile), compressionlevel);
            }
            catch (Exception ex) { throw new Exception("Ошибка: ", ex); }
        }

    }
}