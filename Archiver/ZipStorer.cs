namespace Archiver
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    /* ZipStorer, by Jaime Olivares
       https://github.com/jaime-olivares/zipstorer
       Website: zipstorer.codeplex.com 
       Translated by r3xq1
    */

    public delegate uint OnProgress(ZipStorer.ZipFileEntry fz, uint iter, uint done);

    /// <summary>
    /// Unique class for compression/decompression file. Represents a Zip file.
    /// <br>Уникальный класс для сжатия/распаковки файла. Представляет файл Zip.</br>
    /// </summary>
    public class ZipStorer : IDisposable
    {
        /// <summary>
        /// Compression method enumeration
        /// <br>Перечисление методом сжатия</br>
        /// </summary>
        public enum Compression : ushort
        {
            /// <summary>Uncompressed storage<br>Несжатое хранилище</br></summary>
            Store = 0,
            /// <summary>Deflate compression method<br>Метод сжатия Deflate</br></summary>
            Deflate = 8
        }

        /// <summary>
        /// Represents an entry in Zip file directory
        /// <br>Представляет запись в каталоге файлов Zip</br>
        /// </summary>
        public struct ZipFileEntry
        {
            /// <summary>Compression method<br>Метод сжатия</br></summary>
            public Compression Method;
            /// <summary>Full path and filename as stored in Zip<br>Полный путь и имя файла, сохраненные в Zip</br></summary>
            public string FilenameInZip;
            /// <summary>Original file size<br>Исходный размер файла</br></summary>
            public uint FileSize;
            /// <summary>Compressed file size<br>Размер сжатого файла</br></summary>
            public uint CompressedSize;
            /// <summary>Offset of header information inside Zip storage<br>Смещение информации заголовка внутри Zip-хранилища</br></summary>
            public uint HeaderOffset;
            /// <summary>Offset of file inside Zip storage<br>Смещение файла внутри Zip-хранилища</br></summary>
            public uint FileOffset;
            /// <summary>Size of header information<br>Размер информации заголовка</br></summary>
            public uint HeaderSize;
            /// <summary>32-bit checksum of entire file<br>32-битная контрольная сумма всего файла</br></summary>
            public uint Crc32;
            /// <summary>Last modification time of file<br>Время последнего изменения файла</br></summary>
            public DateTime ModifyTime;
            /// <summary>User comment for file<br>Пользовательский комментарий к файлу</br></summary>
            public string Comment;
            /// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)<br>True, если кодировка UTF8 для имени файла и комментариев, false, если по умолчанию (CP 437)</br></summary>
            public bool EncodeUTF8;

            /// <summary>Overriden method<br>Переопределенный метод</br></summary>
            /// <returns>Filename in Zip<br>Имя файла в Zip</br></returns>
            public override string ToString()
            {
                return FilenameInZip;
            }
        }

        #region Public fields
        /// <summary>True if UTF8 encoding for filename and comments, false if default (CP 437)<br>True, если кодировка UTF8 для имени файла и комментариев, false, если по умолчанию (CP 437)</br></summary>
        public bool EncodeUTF8 = false;
        /// <summary>Force deflate algotithm even if it inflates the stored file. Off by default.<br>Принудительно выкачать алгоритм, даже если он надувает сохраненный файл. Выкл по умолчанию.</br></summary>
        public bool ForceDeflating = false;
        #endregion

        #region Private fields

        // List of files to store
        // Список файлов для хранения
        private readonly List<ZipFileEntry> Files = new List<ZipFileEntry>();
        // Filename of storage file
        // Имя файла хранилища
        private string FileName;
        // Stream object of storage file
        // Потоковый объект хранилища файлов
        private Stream ZipFileStream;
        // General comment
        // Общее замечание
        private string Comment = "";
        // Central dir image
        // Центральное изображение
        private byte[] CentralDirImage = null;
        // Existing files in zip
        // Существующие файлы в zip
        private ushort ExistingFiles = 0;
        // File access for Open method
        // Доступ к файлу для метод Open
        private FileAccess Access;
        // Static CRC32 Table
        // Статическая таблица CRC32
        private static readonly uint[] CrcTable = null;
        // Default filename encoder
        // Кодировщик имени файла по умолчанию
        private static readonly Encoding DefaultEncoding = Encoding.GetEncoding(437);

        #endregion

        #region Public methods

        /// <summary>
        /// Static constructor. Just invoked once in order to create the CRC32 lookup table.
        /// <br>Статический конструктор. Просто вызывается один раз для создания таблицы поиска CRC32.</br>
        /// </summary>
        static ZipStorer()
        {
            // Generate CRC32 table
            // Создать таблицу CRC32
            CrcTable = new uint[256];
            for (int i = 0; i < CrcTable.Length; i++)
            {
                uint c = (uint)i;
                for (int j = 0; j < 8; j++)
                {
                    if ((c & 1) != 0)
                        c = 3988292384 ^ (c >> 1);
                    else
                        c >>= 1;
                }
                CrcTable[i] = c;
            }
        }

        /// <summary>
        /// Method to create a new storage file
        /// <br>Метод создания нового файла хранилища</br>
        /// </summary>
        /// <param name="_filename">Full path of Zip file to create<br>Полный путь к Zip-файлу для создания</br></param>
        /// <param name="_comment">General comment for Zip file<br>Общий комментарий к Zip-файлу</br></param>
        /// <returns>A valid ZipStorer object<br>Допустимый объект ZipStorer</br></returns>
        public static ZipStorer Create(string _filename, string _comment)
        {
            Stream stream = new FileStream(_filename, FileMode.Create, FileAccess.ReadWrite);
            ZipStorer zip = Create(stream, _comment);
            zip.EncodeUTF8 = true;
            zip.Comment = _comment;
            zip.FileName = _filename;
            return zip;
        }

        /// <summary>
        /// Method to create a new zip storage in a stream
        /// <br>Метод создания нового zip-хранилища в потоке</br>
        /// </summary>
        /// <param name="_stream">Stream for write<br>Поток для записи</br></param>
        /// <param name="_comment">Comments on a stream<br>Комментарии к потоку</br></param>
        /// <returns>A valid ZipStorer object<br>Допустимый объект ZipStorer</br></returns>
        public static ZipStorer Create(Stream _stream, string _comment)
        {
            ZipStorer zip = new ZipStorer
            {
                EncodeUTF8 = true,
                Comment = _comment,
                ZipFileStream = _stream,
                Access = FileAccess.Write
            };
            return zip;
        }

        /// <summary>
        /// Method to open an existing storage file
        /// <br>Метод для открытия существующего файла хранилища</br>
        /// </summary>
        /// <param name="_filename">Full path of Zip file to open<br>Полный путь к файлу Zip для открытия</br></param>
        /// <param name="_access">File access mode as used in FileStream constructor<br>Режим доступа к файлу, используемый в конструкторе FileStream</br></param>
        /// <returns>A valid ZipStorer object<br>Допустимый объект ZipStorer</br></returns>
        public static ZipStorer Open(string _filename, FileAccess _access)
        {
            Stream stream = new FileStream(_filename, FileMode.Open, _access == FileAccess.Read ? FileAccess.Read : FileAccess.ReadWrite);
            ZipStorer zip = Open(stream, _access);
            zip.EncodeUTF8 = true;
            zip.FileName = _filename;
            return zip;
        }

        /// <summary>
        /// Method to open an existing storage from stream
        /// <br>Способ открыть существующее хранилище из потока</br>
        /// </summary>
        /// <param name="_stream">Already opened stream with zip contents<br>Уже открытый поток с содержимым почтового индекса</br></param>
        /// <param name="_access">File access mode for stream operations<br>Режим доступа к файлу для потоковых операций</br></param>
        /// <returns>A valid ZipStorer object<br>Допустимый объект ZipStorer</br></returns>
        public static ZipStorer Open(Stream _stream, FileAccess _access)
        {
            if (!_stream.CanSeek && _access != FileAccess.Read)
                throw new InvalidOperationException("Stream cannot seek");
            ZipStorer zip = new ZipStorer
            {
                EncodeUTF8 = true,
                //zip.FileName = _filename;
                ZipFileStream = _stream,
                Access = _access
            };
            if (zip.ReadFileInfo())
                return zip;
            throw new InvalidDataException();
        }

        /// <summary>
        /// Add full contents of a file into the Zip storage
        /// <br>Добавить полное содержимое файла в хранилище Zip</br>
        /// </summary>
        /// <param name="_method">Compression method<br>Метод сжатия</br></param>
        /// <param name="_pathname">Full path of file to add to Zip storage<br>Полный путь к файлу для добавления в Zip-хранилище</br></param>
        /// <param name="_filenameInZip">Filename and path as desired in Zip directory<br>Имя файла и путь по желанию в каталоге Zip</br></param>
        /// <param name="_comment">Comment for stored file<br>Комментарий к сохраненному файлу</br></param>       
        public void AddFile(Compression _method, string _pathname, string _filenameInZip, string _comment)
        {
            if (Access == FileAccess.Read)
                throw new InvalidOperationException("Writing is not alowed");
            var stream = new FileStream(_pathname, FileMode.Open, FileAccess.Read);
            AddStream(_method, _filenameInZip, stream, File.GetLastWriteTime(_pathname), _comment);
            stream.Close();
        }

        /// <summary>
        /// Add full contents of a stream into the Zip storage
        /// <br>Добавить полное содержимое потока в хранилище Zip</br>
        /// </summary>
        /// <param name="_method">Compression method<br>Метод сжатия</br></param>
        /// <param name="_filenameInZip">Filename and path as desired in Zip directory<br>Имя файла и путь по желанию в каталоге Zip</br></param>
        /// <param name="_source">Stream object containing the data to store in Zip<br>Потоковый объект, содержащий данные для хранения в Zip</br></param>
        /// <param name="_modTime">Modification time of the data to store<br>Время модификации данных для хранения</br></param>
        /// <param name="_comment">Comment for stored file<br>Комментарий к сохраненному файлу</br></param>
        public void AddStream(Compression _method, string _filenameInZip, Stream _source, DateTime _modTime, string _comment)
        {
            if (Access == FileAccess.Read) throw new InvalidOperationException("Writing is not alowed");
            if ((uint)Files.Count > 0) _ = Files[Files.Count - 1];

            var _zfe = new ZipFileEntry()
            {
                Method = _method,
                EncodeUTF8 = EncodeUTF8,
                FilenameInZip = NormalizedFilename(_filenameInZip),
                Comment = _comment ?? "",
                // Even though we write the header now, it will have to be rewritten, since we don't know compressed size or crc.
                // Даже если мы сейчас напишем заголовок, его придется переписать, так как мы не знаем сжатый размер или crc.
                Crc32 = 0, // to be updated later // будет обновлено позже
                HeaderOffset = (uint)ZipFileStream.Position, // offset within file of the start of this local record // смещение в файле начала этой локальной записи
                ModifyTime = _modTime
            };
            // Write local header
            // Записать локальный заголовок
            WriteLocalHeader(ref _zfe);
            _zfe.FileOffset = (uint)ZipFileStream.Position;
            // Write file to zip (store)
            // Записать файл в zip (store)
            Store(ref _zfe, _source);
            _source.Close();
            UpdateCrcAndSizes(ref _zfe);
            Files.Add(_zfe);
        }

        /// <summary>
        /// Method for adding a directory to the archive
        /// <br>Метод для добавление директории в архив</br>
        /// </summary>
        /// <param name="_method">Compression method<br>Метод сжатия</br></param>
        /// <param name="_pathname">Full path of directory to add to Zip storage<br>Полный путь к директории для добавления в Zip-хранилище</br></param>
        /// <param name="_pathnameInZip">Full path to Zip<br>Полный путь к архиву</br></param>
        public void AddDirectory(Compression _method, string _pathname, string _pathnameInZip)
        {
            if (Access == FileAccess.Read) throw new InvalidOperationException("Writing is not allowed");

            int num = _pathname.LastIndexOf(Path.DirectorySeparatorChar);
            string text = Path.DirectorySeparatorChar.ToString(), text2;
            text2 = num >= 0 ? _pathname.Remove(0, num + 1) : _pathname;
            if (!string.IsNullOrEmpty(_pathnameInZip)) text2 = $"{_pathnameInZip}{text2}";
            if (!text2.EndsWith(text, StringComparison.CurrentCulture)) text2 += text;

            foreach (string text3 in Directory.EnumerateFiles(_pathname))
            {
                AddFile(_method, text3, $"{text2}{Path.GetFileName(text3)}", "");
            }
            foreach (string pathname in Directory.EnumerateDirectories(_pathname))
            {
                AddDirectory(_method, pathname, text2);
            }
        }

        /// <summary>
        /// Updates central directory (if pertinent) and close the Zip storage
        /// <br>Обновляет центральный каталог (если уместно) и закрывает Zip-хранилище</br>
        /// </summary>
        /// <remarks>This is a required step, unless automatic dispose is used<br>Это обязательный шаг, если не используется автоматическая утилизация</br></remarks>
        public void Close()
        {
            if (Access != FileAccess.Read)
            {
                uint centralOffset = (uint)ZipFileStream.Position, centralSize = 0;
                if (CentralDirImage != null) ZipFileStream.Write(CentralDirImage, 0, CentralDirImage.Length);
                for (int i = 0; i < Files.Count; i++)
                {
                    long pos = ZipFileStream.Position;
                    WriteCentralDirRecord(Files[i]);
                    centralSize += (uint)(ZipFileStream.Position - pos);
                }
               
                if (CentralDirImage != null)
                    WriteEndRecord(centralSize + (uint)CentralDirImage.Length, centralOffset);
                else
                    WriteEndRecord(centralSize, centralOffset);
            }
            if (ZipFileStream != null)
            {
                ZipFileStream.Flush();
                ZipFileStream.Dispose();
                ZipFileStream = null;
            }
        }

        /// <summary>
        /// Read all the file records in the central directory
        /// <br>Читает все записи файла в центральном каталоге</br>
        /// </summary>
        /// <returns>List of all entries in directory<br>Список всех записей в каталоге</br></returns>
        public List<ZipFileEntry> ReadCentralDir()
        {
            if (CentralDirImage == null) throw new InvalidOperationException("Central directory currently does not exist");

            var result = new List<ZipFileEntry>();
            for (int pointer = 0; pointer < CentralDirImage.Length;)
            {
                uint signature = BitConverter.ToUInt32(CentralDirImage, pointer);
                if (signature != 0x02014b50) break;
                bool encodeUTF8 = (BitConverter.ToUInt16(CentralDirImage, pointer + 8) & 0x0800) != 0;
                ushort method = BitConverter.ToUInt16(CentralDirImage, pointer + 10);
                uint modifyTime = BitConverter.ToUInt32(CentralDirImage, pointer + 12), 
                crc32 = BitConverter.ToUInt32(CentralDirImage, pointer + 16),
                comprSize = BitConverter.ToUInt32(CentralDirImage, pointer + 20),
                fileSize = BitConverter.ToUInt32(CentralDirImage, pointer + 24);
                ushort filenameSize = BitConverter.ToUInt16(CentralDirImage, pointer + 28),
                extraSize = BitConverter.ToUInt16(CentralDirImage, pointer + 30),
                commentSize = BitConverter.ToUInt16(CentralDirImage, pointer + 32);
                uint headerOffset = BitConverter.ToUInt32(CentralDirImage, pointer + 42),
                headerSize = (uint)(46 + filenameSize + extraSize + commentSize);
                Encoding encoder = encodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
                var zfe = new ZipFileEntry
                {
                    Method = (Compression)method,
                    FilenameInZip = encoder.GetString(CentralDirImage, pointer + 46, filenameSize),
                    FileOffset = GetFileOffset(headerOffset),
                    FileSize = fileSize,
                    CompressedSize = comprSize,
                    HeaderOffset = headerOffset,
                    HeaderSize = headerSize,
                    Crc32 = crc32,
                    ModifyTime = DosTimeToDateTime(modifyTime)
                };
                if (commentSize > 0)
                {
                    zfe.Comment = encoder.GetString(CentralDirImage, pointer + 46 + filenameSize + extraSize, commentSize);
                }

                result.Add(zfe);
                pointer += (46 + filenameSize + extraSize + commentSize);
            }
            return result;
        }

        /// <summary>
        /// Copy the contents of a stored file into a physical file
        /// <br>Копирует содержимое сохраненного файла в физический файл</br>
        /// </summary>
        /// <param name="_zfe">Entry information of file to extract<br>Информация о файле для извлечения</br></param>
        /// <param name="_filename">Name of file to store uncompressed data<br>Имя файла для хранения несжатых данных</br></param>
        /// <returns>True if success, false if not.<br>True если успех, false если нет.</br></returns>
        /// <remarks>Unique compression methods are Store and Deflate<br>Уникальными методами сжатия являются Store and Deflate</br></remarks>
        public bool ExtractFile(ZipFileEntry _zfe, string _filename)
        {
            // Make sure the parent directory exist
            // Проверяем что родительский каталог существует
            string path = Path.GetDirectoryName(_filename);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            // Check it is directory. If so, do nothing
            // Проверка каталога. Если так, ничего не делай
            if (Directory.Exists(_filename))
                return true;
            Stream output = new FileStream(_filename, FileMode.Create, FileAccess.Write);
            bool result = ExtractFile(_zfe, output);
            if (result) output.Close();

            File.SetCreationTime(_filename, _zfe.ModifyTime);
            File.SetLastWriteTime(_filename, _zfe.ModifyTime);

            return result;
        }

        /// <summary>
        /// Copy the contents of a stored file into an opened stream
        /// <br>Копирует содержимое сохраненного файла в открытый поток</br>
        /// </summary>
        /// <param name="_zfe">Entry information of file to extract<br> Информация о файле для извлечения</br></param>
        /// <param name="_stream">Stream to store the uncompressed data<br>Поток для хранения несжатых данных</br></param>
        /// <returns>True if success, false if not.<br>True если успех, false если нет.</br></returns>
        /// <remarks>Unique compression methods are Store and Deflate<br>Уникальными методами сжатия являются Store and Deflate</br></remarks>
        public bool ExtractFile(ZipFileEntry _zfe, Stream _stream)
        {
            if (!_stream.CanWrite) throw new InvalidOperationException("Stream cannot be written");

            // check signature
            // проверка подписи
            byte[] signature = new byte[4];
            ZipFileStream.Seek(_zfe.HeaderOffset, SeekOrigin.Begin);
            ZipFileStream.Read(signature, 0, 4);
            if (BitConverter.ToUInt32(signature, 0) != 0x04034b50) return false;

            // Select input stream for inflating or just reading
            // Выберите поток ввода для заполнения или просто чтения
            Stream inStream;
            switch (_zfe.Method)
            {
                case Compression.Store: inStream = ZipFileStream; break;
                case Compression.Deflate: inStream = new DeflateStream(ZipFileStream, CompressionMode.Decompress, true); break;
                default: return false;
            }
            // Buffered copy
            // Буферизованная копия
            byte[] buffer = new byte[16384];
            ZipFileStream.Seek(_zfe.FileOffset, SeekOrigin.Begin);
            uint bytesPending = _zfe.FileSize;
            while (bytesPending > 0)
            {
                int bytesRead = inStream.Read(buffer, 0, (int)Math.Min(bytesPending, buffer.Length));
                _stream.Write(buffer, 0, bytesRead);
                bytesPending -= (uint)bytesRead;
            }
            _stream.Flush();
            if (_zfe.Method == Compression.Deflate) inStream.Dispose();

            return true;
        }

        /// <summary>
        /// Removes one of many files in storage. It creates a new Zip file.
        /// <br>Удаляет один из многих файлов в хранилище. Создает новый Zip-файл.</br>
        /// </summary>
        /// <param name="_zip">Reference to the current Zip object<br>Ссылка на текущий объект Zip</br></param>
        /// <param name="_zfes">List of Entries to remove from storage<br>Список записей для удаления из хранилища</br></param>
        /// <returns>True if success, false if not<br>True если успех, false если не успешно</br></returns>
        /// <remarks>This method only works for storage of type FileStream<br>Этот метод работает только для хранения типа FileStream</br></remarks>
        public static bool RemoveEntries(ref ZipStorer _zip, List<ZipFileEntry> _zfes)
        {
            if (!(_zip.ZipFileStream is FileStream)) throw new InvalidOperationException("RemoveEntries is allowed just over streams of type FileStream");

            // Get full list of entries
            // Получить полный список записей
            List<ZipFileEntry> fullList = _zip.ReadCentralDir();
            // In order to delete we need to create a copy of the zip file excluding the selected items
            // Для удаления нам нужно создать копию zip-файла, исключая выбранные элементы.
            string tempZipName = Path.GetTempFileName(), tempEntryName = Path.GetTempFileName();
            try
            {
                ZipStorer tempZip = Create(tempZipName, string.Empty);
                tempZip.EncodeUTF8 = true;
                foreach (ZipFileEntry zfe in fullList)
                {
                    if (!_zfes.Contains(zfe) && _zip.ExtractFile(zfe, tempEntryName))
                    {
                        tempZip.AddFile(zfe.Method, tempEntryName, zfe.FilenameInZip, zfe.Comment);
                    }
                }
                _zip.Close(); tempZip.Close();

                File.Delete(_zip.FileName);
                File.Move(tempZipName, _zip.FileName);
                _zip = Open(_zip.FileName, _zip.Access);
            }
            catch { return false; }
            finally
            {
                if (File.Exists(tempZipName)) File.Delete(tempZipName);
                if (File.Exists(tempEntryName)) File.Delete(tempEntryName);
            }
            return true;
        }
        #endregion

        #region Private methods
        // Calculate the file offset by reading the corresponding local header
        // Вычислить смещение файла, прочитав соответствующий локальный заголовок
        private uint GetFileOffset(uint _headerOffset)
        {
            byte[] buffer = new byte[2];
            ZipFileStream.Seek(_headerOffset + 26, SeekOrigin.Begin);
            ZipFileStream.Read(buffer, 0, 2);
            ushort filenameSize = BitConverter.ToUInt16(buffer, 0);
            ZipFileStream.Read(buffer, 0, 2);
            ushort extraSize = BitConverter.ToUInt16(buffer, 0);
            return (uint)(30 + filenameSize + extraSize + _headerOffset);
        }

        /* Local file header:
            local file header signature     4 bytes  (0x04034b50)
            version needed to extract       2 bytes
            general purpose bit flag        2 bytes
            compression method              2 bytes
            last mod file time              2 bytes
            last mod file date              2 bytes
            crc-32                          4 bytes
            compressed size                 4 bytes
            uncompressed size               4 bytes
            filename length                 2 bytes
            extra field length              2 bytes
            filename (variable size)
            extra field (variable size)
        */

        private void WriteLocalHeader(ref ZipFileEntry _zfe)
        {
            long pos = ZipFileStream.Position;
            Encoding encoder = _zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes(_zfe.FilenameInZip);
            ZipFileStream.Write(new byte[] { 80, 75, 3, 4, 20, 0 }, 0, 6); // No extra header | Без дополнительного заголовка
            ZipFileStream.Write(BitConverter.GetBytes((ushort)(_zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2); // filename and comment encoding | кодировка имени файла и комментария
            ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method
            ZipFileStream.Write(BitConverter.GetBytes(DateTimeToDosTime(_zfe.ModifyTime)), 0, 4); // zipping date and time
            ZipFileStream.Write(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 12); // unused CRC, un/compressed size, updated later | неиспользованный CRC, не/сжатый размер, обновляется позже
            ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2); // размер файла
            ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // extra length | дополнительная длина
            ZipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            _zfe.HeaderSize = (uint)(ZipFileStream.Position - pos);
        }

        /* Central directory's File header:
            central file header signature   4 bytes  (0x02014b50)
            version made by                 2 bytes
            version needed to extract       2 bytes
            general purpose bit flag        2 bytes
            compression method              2 bytes
            last mod file time              2 bytes
            last mod file date              2 bytes
            crc-32                          4 bytes
            compressed size                 4 bytes
            uncompressed size               4 bytes
            filename length                 2 bytes
            extra field length              2 bytes
            file comment length             2 bytes
            disk number start               2 bytes
            internal file attributes        2 bytes
            external file attributes        4 bytes
            relative offset of local header 4 bytes
            filename (variable size)
            extra field (variable size)
            file comment (variable size)
        */

        /* Заголовок файла центрального каталога:
            подпись центрального файла заголовка 4 байта (0x02014b50)
            версия сделана 2 байтами
            версия, необходимая для извлечения 2 байтов
            битовый флаг общего назначения 2 байта
            метод сжатия 2 байтавремя последнего мода файла 2 байта
            дата последнего файла мода 2 байта
            crc-32 4 байта
            сжатый размер 4 байта
            несжатый размер 4 байта
            длина файла 2 байтадополнительная длина поля 2 байта
            длина комментария файла 2 байта
            номер диска, начало 2 байта
            внутренние атрибуты файла 2 байта
            атрибуты внешнего файла 4 байта
            Относительное смещение локального заголовка 4 байта
            имя файла (переменный размер)дополнительное поле (переменный размер)
            файл комментария (переменный размер)
         */

        private void WriteCentralDirRecord(ZipFileEntry _zfe)
        {
            Encoding encoder = _zfe.EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            byte[] encodedFilename = encoder.GetBytes(_zfe.FilenameInZip);
            byte[] encodedComment = encoder.GetBytes(_zfe.Comment);
            ZipFileStream.Write(new byte[] { 80, 75, 1, 2, 23, 0xB, 20, 0 }, 0, 8);
            ZipFileStream.Write(BitConverter.GetBytes((ushort)(_zfe.EncodeUTF8 ? 0x0800 : 0)), 0, 2); // filename and comment encoding
            ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method
            ZipFileStream.Write(BitConverter.GetBytes(DateTimeToDosTime(_zfe.ModifyTime)), 0, 4);  // zipping date and time
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4); // file CRC
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.CompressedSize), 0, 4); // compressed file size
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.FileSize), 0, 4); // uncompressed file size
            ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedFilename.Length), 0, 2); // Filename in zip
            ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // extra length
            ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);
            ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // disk=0
            ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // file type: binary
            ZipFileStream.Write(BitConverter.GetBytes((ushort)0), 0, 2); // Internal file attributes
            ZipFileStream.Write(BitConverter.GetBytes((ushort)0x8100), 0, 2); // External file attributes (normal/readable)
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.HeaderOffset), 0, 4);  // Offset of header
            ZipFileStream.Write(encodedFilename, 0, encodedFilename.Length);
            ZipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }

        /* End of central dir record:
            end of central dir signature    4 bytes  (0x06054b50)
            number of this disk             2 bytes
            number of the disk with the
            start of the central directory  2 bytes
            total number of entries in
            the central dir on this disk    2 bytes
            total number of entries in
            the central dir                 2 bytes
            size of the central directory   4 bytes
            offset of start of central
            directory with respect to
            the starting disk number        4 bytes
            zipfile comment length          2 bytes
            zipfile comment (variable size)
        */
        /* Конец центральной записи в директории:
            конец центральной директории подписи 4 байта (0x06054b50)
            номер этого диска 2 байта
            номер диска с
            начало центрального каталога 2 байта
            общее количество записей в
            центральный каталог на этом диске 2 байтаобщее количество записей в
            центральный каталог 2 байта
            размер центрального каталога 4 байта
            смещение начала центрального
            каталог по отношению к
            начальный диск № 4 байта
            zipfile длина комментария 2 байта Комментарий zipfile (переменный размер)
         */

        private void WriteEndRecord(uint _size, uint _offset)
        {
            var encoder = EncodeUTF8 ? Encoding.UTF8 : DefaultEncoding;
            ZipFileStream.Write(new byte[] { 80, 75, 5, 6, 0, 0, 0, 0 }, 0, 8);
            ZipFileStream.Write(BitConverter.GetBytes((ushort)Files.Count + ExistingFiles), 0, 2);
            ZipFileStream.Write(BitConverter.GetBytes((ushort)Files.Count + ExistingFiles), 0, 2);
            ZipFileStream.Write(BitConverter.GetBytes(_size), 0, 4);
            ZipFileStream.Write(BitConverter.GetBytes(_offset), 0, 4);
            byte[] encodedComment = encoder.GetBytes(Comment);
            ZipFileStream.Write(BitConverter.GetBytes((ushort)encodedComment.Length), 0, 2);
            ZipFileStream.Write(encodedComment, 0, encodedComment.Length);
        }

        /// <summary>
        /// Copies all source file into storage file
        /// <br>Копирует весь исходный файл в файл хранилища</br>
        /// </summary>
        /// <param name="_zfe"></param>
        /// <param name="_source"></param>
        private void Store(ref ZipFileEntry _zfe, Stream _source)
        {
            byte[] buffer = new byte[16384];
            int bytesRead; uint totalRead = 0;
            Stream outStream;
            long posStart = ZipFileStream.Position, sourceStart = _source.Position;
            outStream = _zfe.Method == Compression.Store ? ZipFileStream : new DeflateStream(ZipFileStream, CompressionMode.Compress, true);
            _zfe.Crc32 = 0 ^ 0xffffffff;

            do
            {
                bytesRead = _source.Read(buffer, 0, buffer.Length); totalRead += (uint)bytesRead;
                if (bytesRead > 0)
                {
                    outStream.Write(buffer, 0, bytesRead);
                    for (uint i = 0; i < bytesRead; i++)
                        _zfe.Crc32 = CrcTable[(_zfe.Crc32 ^ buffer[i]) & 0xFF] ^ (_zfe.Crc32 >> 8);
                }
            } 
            while (bytesRead == buffer.Length);
            outStream.Flush();
            if (_zfe.Method == Compression.Deflate) outStream.Dispose();

            _zfe.Crc32 ^= 0xffffffff;
            _zfe.FileSize = totalRead;
            _zfe.CompressedSize = (uint)(ZipFileStream.Position - posStart);

            // Verify for real compression
            // Проверка на реальное сжатие
            if (_zfe.Method == Compression.Deflate && !ForceDeflating && _source.CanSeek && _zfe.CompressedSize > _zfe.FileSize)
            {
                // Start operation again with Store algorithm
                // Начать операцию снова с алгоритмом Store
                _zfe.Method = Compression.Store;
                ZipFileStream.Position = posStart;
                ZipFileStream.SetLength(posStart);
                _source.Position = sourceStart;
                Store(ref _zfe, _source);
            }
        }
        /* DOS Date and time:
            MS-DOS date. The date is a packed value with the following format. Bits Description
                0-4 Day of the month (131)
                5-8 Month (1 = January, 2 = February, and so on)
                9-15 Year offset from 1980 (add 1980 to get actual year)
            MS-DOS time. The time is a packed value with the following format. Bits Description
                0-4 Second divided by 2
                5-10 Minute (059)
                11-15 Hour (023 on a 24-hour clock)
        */

        /* DOS Дата и время:
            Дата MS-DOS. Дата представляет собой упакованное значение в следующем формате. Описание битов
                0-4 день месяца (1В – 31)
                5-8 месяцев (1 = январь, 2 = февраль и т. Д.)
                Смещение на 9-15 лет по сравнению с 1980 годом (добавьте 1980, чтобы получить фактический год)
            Время MS-DOS.Время представляет собой упакованное значение в следующем формате. Описание битов
                0-4 секунды делится на 2
                5-10 минут (0–59)
                11-15 часов (0–23 на 24-часовой основе)
         */

        private uint DateTimeToDosTime(DateTime _dt) => 
        (uint)((_dt.Second / 2) | (_dt.Minute << 5) | (_dt.Hour << 11) | (_dt.Day << 16) | (_dt.Month << 21) | ((_dt.Year - 1980) << 25));

        private DateTime DosTimeToDateTime(uint _dt)
        {
            return 
            new DateTime((int)(_dt >> 25) + 1980, (int)(_dt >> 21) & 15, (int)(_dt >> 16) & 31,
            (int)(_dt >> 11) & 31, (int)(_dt >> 5) & 63, (int)(_dt & 31) * 2);
        }

        /* CRC32 algorithm
          The 'magic number' for the CRC is 0xdebb20e3. 
          The proper CRC pre and post conditioning
          is used, meaning that the CRC register is
          pre-conditioned with all ones (a starting value
          of 0xffffffff) and the value is post-conditioned by
          taking the one's complement of the CRC residual.
          If bit 3 of the general purpose flag is set, this
          field is set to zero in the local header and the correct
          value is put in the data descriptor and in the central
          directory.
        */
        /* CRC32 алгоритм
         «Магическое число» для CRC - 0xdebb20e3.
         Правильная CRC до и после кондиционирования
         используется, это означает, что регистр CRC
         предварительно обусловлено всеми (начальное значение
         0xffffffff), а значение пост-обусловлено
         взятие одного дополнения к остатку CRC.Если установлен бит 3 флага общего назначения, это
         поле установлено в ноль в локальном заголовке и правильное
         значение помещается в дескриптор данных и в центральном
         каталог.
       */

        private void UpdateCrcAndSizes(ref ZipFileEntry _zfe)
        {
            long lastPos = ZipFileStream.Position;  // remember position / запоминаем позицию
            ZipFileStream.Position = _zfe.HeaderOffset + 8;
            ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);  // zipping method
            ZipFileStream.Position = _zfe.HeaderOffset + 14;
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4);  // Update CRC
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.CompressedSize), 0, 4);  // Compressed size
            ZipFileStream.Write(BitConverter.GetBytes(_zfe.FileSize), 0, 4);  // Uncompressed size
            ZipFileStream.Position = lastPos;  // restore position / восстановить позицию
        }

        /// <summary>
        /// Replaces backslashes with slashes to store in zip header
        /// <br>Заменяет обратную косую черту косой чертой для сохранения в zip-заголовке</br>
        /// </summary>
        /// <param name="_filename">Имя файла</param>
        /// <returns></returns>
        private string NormalizedFilename(string _filename)
        {
            string filename = _filename.Replace('\\', '/');
            int pos = filename.IndexOf(':');
            if (pos >= 0)
                filename = filename.Remove(0, pos + 1);
            return filename.Trim('/');
        }

        /// <summary>
        /// Reads the end-of-central-directory record
        /// <br>Читает запись конца центрального каталога</br>
        /// </summary>
        /// <returns></returns>
        private bool ReadFileInfo()
        {
            if (ZipFileStream.Length < 22) return false;

            try
            {
                ZipFileStream.Seek(-17, SeekOrigin.End);
                var br = new BinaryReader(ZipFileStream);
                do
                {
                    ZipFileStream.Seek(-5, SeekOrigin.Current);
                    uint sig = br.ReadUInt32();
                    if (sig == 0x06054b50)
                    {
                        ZipFileStream.Seek(6, SeekOrigin.Current);

                        ushort entries = br.ReadUInt16();
                        int centralSize = br.ReadInt32();
                        uint centralDirOffset = br.ReadUInt32();
                        ushort commentSize = br.ReadUInt16();

                        // check if comment field is the very last data in file
                        if (ZipFileStream.Position + commentSize != ZipFileStream.Length) return false;

                        // Copy entire central directory to a memory buffer
                        ExistingFiles = entries;
                        CentralDirImage = new byte[centralSize];
                        ZipFileStream.Seek(centralDirOffset, SeekOrigin.Begin);
                        ZipFileStream.Read(CentralDirImage, 0, centralSize);

                        // Leave the pointer at the begining of central dir, to append new files
                        ZipFileStream.Seek(centralDirOffset, SeekOrigin.Begin);
                        return true;
                    }
                } while (ZipFileStream.Position > 0);
            }
            catch { }

            return false;
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Closes the Zip file stream
        /// <br>Закрывает поток файлов Zip</br>
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Method for dispose stream after compression
        /// <br>Метод для освобождения потока после выполнения сжатия</br>
        /// </summary>
        /// <param name="_zfe">Zip Catalog <br>Каталог Zip</br></param>
        /// <param name="s">Data stream<br>Поток данных</br></param>
        public void DisposeStream(ZipFileEntry _zfe, Stream s)
        {
            if (_zfe.Method == Compression.Deflate)
            {
                s?.Dispose();
            }
        }
        #endregion
    }
}