# ArchiveDataR3
[Ru]ZipStorer класс для архивации ваших данных в .Net 4.0 и выше.

Оригинал ZipStorer класс был взят от сюда: https://github.com/jaime-olivares/zipstorer<br>
Моя статья по архивации с этими классами: https://teletype.in/@r3xq1/ArchiveDataR3<br>
Класс полностью прокомментирован на Русскоязычную аудиторию, чтобы было понятно кто не знаком с Английским языком.

**[Заметки]**<br>
Всегда используйте `using` ( при этом не вызывайте `Close();` )<br>
За место ковычек `""` пишите так: `string.Empty;`<br> 
Ни в коем случае не вызывайте `null` иначе будет крашить с ошибками.<br> 
Есть небольшие проблемы с [RU] кодировкой файлов при сборе, за мето Русского текста заменяется ____ подчёркиванием.

**Пример использования**<br>
````csharp
private static readonly string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // Путь к рабочему столу.
private static readonly string TargetFile = Path.Combine(Desktop, "11.jpg"); // Путь к файлу на рабочем столе.
private static readonly string ZipArchive = Path.Combine(Desktop, "MyArchive.zip"); // Путь куда будет сохраняться .Zip архив с данными.
````
*Для массового добавления файлов*

````csharp
// Создаём массив файлов для сбора
var filegrabber = new string[] 
{ 
   Path.Combine(Desktop, "Arch.png"), 
   Path.Combine(Desktop, "11.jpg") 
};
            
using var zip = ZipStorer.Create(ZipArchive, string.Empty); // Второй аргумент это комментарии к архиву.
zip.EncodeUTF8 = true; // Задаём кодировку. ( Не обязательно )
foreach (string files in filepath) // Перебираем все файлы в цикле.
{
  if (File.Exists(files)) // Проверяем каждый файл
  {
     zip?.AddFile(mode, files, Path.GetFileName(files), string.Empty); // Добавляем в архив каждый файл из цикла.
  }
}
````
Вызов: 
````csharp 
ZipR3.AddMassFile(ZipArchive, filegrabber, ZipStorer.Compression.Store);
````

*Для массового добавления папок*

````csharp
// Создаём массив папок для сбора
var dirgrabber = new string[] 
{ 
   Path.Combine(Desktop, "Dir One"), 
   Path.Combine(Desktop, "Dir Two") 
};

using var zipdir = ZipStorer.Create(ZipArchive, string.Empty);
foreach (string dir in datapath) // Проходимся по циклу
{
  if (Directory.Exists(dir)) // Проверяем каждую папку
  {
     zipdir?.AddDirectory(mode, dir, string.Empty); // Добавляем каждую папку(с файлами внутри) в архив
  }
}
````
Вызов:
````csharp
 ZipR3.AddMassDirectory(ZipArchive, dirgrabber, ZipStorer.Compression.Store);
 ````
 
