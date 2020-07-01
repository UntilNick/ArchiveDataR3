namespace Archiver
{
    using System;
    using System.IO;
    using System.IO.Compression;

    internal static class Program
    {
        private static readonly string CurrDir = Environment.CurrentDirectory;
        private static readonly string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private static readonly string TargetFile = Path.Combine(Desktop, "11.jpg");
        private static readonly string TargetDir = Path.Combine(Desktop, "ZipStorer New");

        public static void Main()
        {
            //ArchCmd.PackZip(RegZipPath.FindWinZip(), Path.Combine(CurrDir, "ZipDot"), Path.Combine(Desktop, "r3xq1"));
            //ArchCmd.UnpackZip(RegZipPath.FindWinZip(), Path.Combine(Desktop, "r3xq1"), Path.Combine(Desktop, "r3xq1"));

            // ArchCmd.PackRar(RegZipPath.FindWinRar(), Path.Combine(CurrDir, "ZipDot"), Path.Combine(Desktop, "r3xq1"));

            // var DirGrabber = new string[] { Path.Combine(Desktop, "Echeln"), Path.Combine(Desktop, "dbdriverinstaller") };
            //ZipR3.AddMassDirectory("r3xq1.zip", DirGrabber, ZipStorer.Compression.Store);
            // ZipR3.AddDirectory("r3xq1.zip", Path.Combine(Desktop, "dbdriverinstaller"), ZipStorer.Compression.Store);
            //ZipR3.AddFile("r3xq1.zip", Path.Combine(Desktop, "Build.txt"), ZipStorer.Compression.Store);
            // ZipR3.UnpackFile("r3xq1.zip", "Build.txt", CurrDir);
            //ZipHigh.AddFileInZip("r3xq1.zip", TargetFile, CompressionLevel.Fastest);
            Console.Read();
        }
    }
}