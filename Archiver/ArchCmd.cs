namespace Archiver
{
    using System.Diagnostics;
    using System.IO;

    public static class ArchCmd
    {
        /* Created by r3xq1 */

        #region Методы для реализации архивации и разархивации при помощи 7-Zip архиватора Windows

        /* -Примеры использования-
           string CurrDir = Environment.CurrentDirectory; // Текущая директория
           string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // Рабочий стол
           ZipDot - Имя папки которую архивируем. 
           r3xq1 - Имя архива ( без .zip )
           Для архивации: PackZip(RegZipPath.FindWinZip(), Path.Combine(CurrDir, "ZipDot"), Path.Combine(Desktop, "r3xq1"));
           Для разархивации: UnpackZip(RegZipPath.FindWinZip(), Path.Combine(Desktop, "r3xq1"), Path.Combine(Desktop, "r3xq1"));
        */

        /// <summary>
        /// Метод для архивации папки с ультра жатием.
        /// </summary>
        /// <param name="zipshell">Путь к архиватору</param>
        /// <param name="datapath">Путь к папке которую нужно архивировать</param>
        /// <param name="outputzip">Выходной путь, куда сохранять с новым именем архива</param>
        public static void PackZip(string zipshell, string datapath, string outputzip)
        {
            if (File.Exists(zipshell) && (!string.IsNullOrWhiteSpace(datapath) || !string.IsNullOrWhiteSpace(outputzip)))
            {
                var pro = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = zipshell,
                    Arguments = $@"a -tzip -r {outputzip}.zip {datapath} -mx=9"
                };
                using var x = Process.Start(pro);
                x.Refresh();
                x.WaitForExit();
            }
        }

        /// <summary>
        /// Метод для распаковки .zip архива
        /// </summary>
        /// <param name="zipshell">Путь к архиватору</param>
        /// <param name="inputzip">Путь к папке которую нужно разархивировать</param>
        /// <param name="outputdir">Выходной путь, куда сохранять папку</param>
        public static void UnpackZip(string zipshell, string inputzip, string outputdir)
        {
            if (File.Exists(zipshell) && (!string.IsNullOrWhiteSpace(inputzip) || !string.IsNullOrWhiteSpace(outputdir)))
            {
                var pro = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = zipshell,
                    Arguments = $"x {inputzip}.zip -o{outputdir}"
                };
                using var x = Process.Start(pro);
                x.WaitForExit();
            }
        }
        #endregion

        #region Методы для реализации архивации и разархивации при помощи WinRaR архиватора Windows 

        /// <summary>
        /// Метод для архивации папки с ультра жатием.
        /// </summary>
        /// <param name="rarshell">Путь к архиватору</param>
        /// <param name="datapath">Путь к папке которую нужно архивировать</param>
        /// <param name="outputzip">Выходной путь, куда сохранять с новым именем архива</param>
        public static void PackRar(string rarshell, string datapath, string outputzip)
        {
            if (File.Exists(rarshell) && (!string.IsNullOrWhiteSpace(datapath) || !string.IsNullOrWhiteSpace(outputzip)))
            {
                var pro = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = rarshell,
                    Arguments = $@"a -ep1 -m5 -r -y {outputzip}.rar {datapath}"
                };
                using var x = Process.Start(pro);
                x.Refresh();
                x.WaitForExit();
            }
        }

        // Метод нужно переделать.
        public static void UnpackRar(string rarshell, string inputrar, string outputdir)
        {
            if (File.Exists(rarshell) && (!string.IsNullOrWhiteSpace(inputrar) || !string.IsNullOrWhiteSpace(outputdir)))
            {
                var pro = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = rarshell,
                    Arguments = $@"unrar x  {inputrar}.rar -o{outputdir}"
                };
                using var x = Process.Start(pro);
                x.Refresh();
                x.WaitForExit();
            }
        }

        #endregion
    }
}