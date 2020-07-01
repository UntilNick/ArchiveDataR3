namespace Archiver
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class ZipR3
    {
        /* Created r3xq1 */

        #region Methods for adding folders to the archive | Методы для добавления папок в архив

        /// <summary>
        /// Method for adding folders to .Zip archive
        /// <br>Метод для добавления папок в .Zip архив</br>
        /// </summary>
        /// <param name="zippath">The path to the .zip archive folder<br>Путь к папке .zip архива</br></param>
        /// <param name="datapath">Full path to folders to add to .zip archive<br>Полный путь к папкам для добавления в .zip архив</br></param>
        /// <param name="mode">Compression method<br>Метод сжатия</br></param>
        /// <param name="commentzip">Comments on the .zip archive<br>Комментарии к .zip архиву</br></param>
        public static void AddMassDirectory(string zippath, string[] datapath, ZipStorer.Compression mode, string commentzip = "") 
        {
            using var zipdir = ZipStorer.Create(zippath, commentzip);
            foreach (string dir in datapath.Where(dir => Directory.Exists(dir)))
            {
                zipdir?.AddDirectory(mode, dir, string.Empty);
            }
        }
        public static void AddMassDirectory(string zippath, List<string> datapath, ZipStorer.Compression mode, string commentzip = "")
        {
            using var zipdir = ZipStorer.Create(zippath, commentzip);
            foreach (string dir in datapath.Where(dir => Directory.Exists(dir)))
            {
                zipdir?.AddDirectory(mode, dir, string.Empty);
            }
        }

        /// <summary>
        /// <br>Метод для добавления папки в .Zip архив</br>
        /// </summary>
        /// <param name="zippath">The path to the .zip archive folder<br>Путь к папке .zip архива</br></param>
        /// <param name="datapath">Full path to the folder to add to the .zip archive<br>Полный путь к папке для добавления в .zip архив</br></param>
        /// <param name="mode">Compression method<br>Метод сжатия</br></param>
        /// <param name="commentzip">Comments on the .zip archive<br>Комментарии к .zip архиву</br></param>
        public static void AddDirectory(string zippath, string datapath, ZipStorer.Compression mode, string commentzip = "") 
        {
            using var zipdir = ZipStorer.Create(zippath, commentzip);
            if (Directory.Exists(datapath))
            {
                zipdir?.AddDirectory(mode, datapath, string.Empty);
            }
        }
        #endregion

        #region Methods for adding files to the archive | Методы для добавления файлов в архив

        /// <summary>
        /// Method for adding a files to a .zip archive
        /// <br>Метод для добавления файлов в .zip архив</br>
        /// </summary>
        /// <param name="zippath">The path to the .zip archive folder<br>Путь к папке .zip архива</br></param>
        /// <param name="filepath">Full path to files to add to .zip archive<br>Полный путь к файлам для добавления в .zip архив</br></param>
        /// <param name="mode">Compression method<br>Метод сжатия</br></param>
        /// <param name="commentzip">Comments on the .zip archive<br>Комментарии к .zip архиву</br></param>
        public static void AddMassFile(string zippath, string[] filepath, ZipStorer.Compression mode, string commentzip = "")
        {
            using var zip = ZipStorer.Create(zippath, commentzip);
            zip.EncodeUTF8 = true;
            foreach (string files in filepath.Where(files => File.Exists(files)))
            {
                zip?.AddFile(mode, files, Path.GetFileName(files), string.Empty);
            }
        }
        public static void AddMassFile(string zippath, List<string> filepath, ZipStorer.Compression mode, string commentzip = "")
        {
            using var zip = ZipStorer.Create(zippath, commentzip);
            zip.EncodeUTF8 = true;
            foreach (string files in filepath.Where(files => File.Exists(files)))
            {
                zip?.AddFile(mode, files, Path.GetFileName(files), string.Empty);
            }
        }

        /// <summary>
        /// Method for adding a file to a .zip archive
        /// <br>Метод для добавления файла в .zip архив</br>
        /// </summary>
        /// <param name="zippath">The path to the .zip archive folder<br>Путь к папке .zip архива</br></param>
        /// <param name="filepath">Full path to the file to add to the .Zip archive<br>Полный путь к файлу для добавления в .Zip архив</br></param>
        /// <param name="mode">Compression method<br>Метод сжатия</br></param>
        /// <param name="commentzip">Comments on the .zip archive<br>Комментарии к .zip архиву</br></param>
        public static void AddFile(string zippath, string filepath, ZipStorer.Compression mode, string commentzip = "")
        {
            using var zip = ZipStorer.Create(zippath, commentzip);
            zip.EncodeUTF8 = true;
            zip?.AddFile(mode, filepath, Path.GetFileName(filepath), string.Empty);
        }

        #endregion

        #region Methods for unpacking files from the archive | Методы для распаковки файлов из архива

        /// <summary>
        /// Method for unpacking a files from a .Zip archive
        /// <br>Метод для распаковки файлов из .Zip архива</br>
        /// </summary>
        /// <param name="zippath">The path to the .zip archive folder<br>Путь к папке .zip архива</br></param>
        /// <param name="filenames">Name of files to unzip<br>Имя файлов которые нужно распаковать</br></param>
        /// <param name="savepath">Path to save files<br>Путь для сохранения файлов</br></param>
        public static void UnpackMassFile(string zippath, string[] filenames, string savepath)
        {
            using var zip = ZipStorer.Open(zippath, FileAccess.Read);
            foreach (ZipStorer.ZipFileEntry entry in zip.ReadCentralDir())
            {
                foreach (string file in filenames.Where(file => Path.GetFileName(entry.FilenameInZip).Contains(file)))
                {
                    zip?.ExtractFile(entry, Path.Combine(savepath, file));
                }
            }
        }

        /// <summary>
        /// Method for unpacking a file from a .Zip archive
        /// <br>Метод для распаковки файла из .Zip архива</br>
        /// </summary>
        /// <param name="zippath">The path to the .zip archive folder<br>Путь к папке .zip архива</br></param>
        /// <param name="filename">The name of the file you want to unzip<br>Имя файла которого нужно распаковать</br></param>
        /// <param name="savepath">Path to save file<br>Путь для сохранения файла</br></param>
        public static void UnpackFile(string zippath, string filename, string savepath)
        {
            using var zip = ZipStorer.Open(zippath, FileAccess.Read);
            foreach (ZipStorer.ZipFileEntry entry in zip.ReadCentralDir().Where(entry => Path.GetFileName(entry.FilenameInZip).Contains(filename)))
            {
                zip?.ExtractFile(entry, Path.Combine(savepath, filename));
                break;
            }
        }
        #endregion
    }
}