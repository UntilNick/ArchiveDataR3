namespace Archiver
{
    using Microsoft.Win32;

    public static class RegZipPath
    {
        /* Created by r3xq1 */

        /// <summary>
        /// Метод для поиска установленного WinRAR архиватора в системе через реестр
        /// </summary>
        /// <returns>Путь к <b>.exe</b> архиватору</returns>
        public static string FindWinRar()
        {
            string result = string.Empty;
            try
            {
                const string REGPATH = @"WinRAR\Shell\Open\Command";
                using RegistryKey Root = Registry.ClassesRoot.OpenSubKey(REGPATH);
                string winrarPath = (Root?.GetValue(""))?.ToString();
                winrarPath = winrarPath.Substring(1, winrarPath.Length - 7);
                result = winrarPath;
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Метод для поиска установленного 7-Zip архиватора в системе через реестр
        /// </summary>
        /// <returns>Путь к <b>.exe</b> архиватору</returns>
        public static string FindWinZip()
        {
            string result = string.Empty;
            try
            {
                const string REGPATH = @"Software\7-Zip";
                using RegistryKey zip = Registry.CurrentUser.OpenSubKey(REGPATH);
                result = string.Concat((zip?.GetValue("Path"))?.ToString(), "7z.exe");
            }
            catch { }
            return result;
        }
    }
}