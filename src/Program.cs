using System.Text;
using Sinya.PZModsTool.Objects;
using static System.String;

namespace Sinya.PZModsTool;

internal class Program
{
    private static ConsoleColor _defaultColor;

    static async Task Main(string[] args)
    {
        _defaultColor = Console.ForegroundColor;
        Console.WriteLine("This tool generates the 'Mods' and 'WorkshopItems' list for Project Zomboid dedicated server config.\r\nTwo files will be created; 'Mods.txt' and 'WorkshopItems.txt' in a folder named {workshopId}, copy and past their content to their respective config fields in your server.ini.");

        try
        {
            var path = GetAndValidatePath();
            var collection = await Parse(path);

            if (collection == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to parse workshop collection folder.");
                Console.ForegroundColor = _defaultColor;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Saving workshop collection (ModId: {collection.Mods.Count}, WorkshopItems: {collection.WorkshopItems.Count})...");
                Console.ForegroundColor = _defaultColor;

                await WriteCollection(collection);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occured: {ex.GetType().Name}");
            Console.ForegroundColor = _defaultColor;
        }

        Console.ReadLine();
    }

    private static async Task WriteCollection(WorkshopCollection collection)
    {
        var path = GetOutputPath(collection.Id);

        await WriteFile(path, "Mods", collection.Mods);
        await WriteFile(path, "WorkshopItems", collection.WorkshopItems);

        Console.WriteLine($"Done! Files can be found here: '{path}'");
    }

    private static async Task WriteFile(string path, string name, List<string> values)
    {
        var filePath = Path.Combine(path, $"{name}.txt");
        var content = values.Aggregate((c, n) => $"{c};{n}");

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
    }

    private static string GetOutputPath(string workshopId)
    {
        var path = Path.Combine(Environment.CurrentDirectory, workshopId);

        Directory.CreateDirectory(path);

        return path;
    }

    private static string GetAndValidatePath()
    {
        var done = false;
        string path = Empty;

        do
        {
            Console.WriteLine("\r\nEnter the path to the workshop collection (\\steamapps\\workshop\\content\\{collectionId}):");

            path = Console.ReadLine() ?? Empty;

            if (IsNullOrWhiteSpace(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid path!");
                Console.ForegroundColor = _defaultColor;
                continue;
            }

            if (!Directory.Exists(path))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not find path!");
                Console.ForegroundColor = _defaultColor;
                continue;
            }

            return path;
        } while (true);
    }

    private static async Task<WorkshopCollection?> Parse(string path)
    {
        var id = GetCurrentFolderName(path);

        if (!id.All(char.IsDigit))
            return null;

        var collection = new WorkshopCollection
        {
            Id = id
        };

        var workshopItemFolders = Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly);

        foreach (var workshopItemFolder in workshopItemFolders)
        {
            var workshopItem = GetCurrentFolderName(workshopItemFolder);
            var infos = await FindModInfos(workshopItemFolder);

            if (infos != null && infos.Any())
            {
                Console.WriteLine($"Adding WorkshopItem '{workshopItem}' with ({infos.Count}) mods:");
                foreach (var modInfo in infos)
                {
                    Console.WriteLine($"[{modInfo.Id}] {modInfo.Name}\r\n{modInfo.Description}");
                }
                Console.WriteLine();

                collection.WorkshopItems.Add(workshopItem);
                collection.Mods.AddRange(infos.Select(x => x.Id));
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"No mods found in WorkshopItem '{workshopItem}");
                Console.ForegroundColor = _defaultColor;
            }
        }

        return collection;
    }

    private static async Task<List<ModInfo>?> FindModInfos(string workshopItemFolder)
    {
        var modInfos = new List<ModInfo>();
        var subFolder = Path.Combine(workshopItemFolder, "mods");

        foreach (var modFolder in Directory.EnumerateDirectories(subFolder, "*", SearchOption.TopDirectoryOnly))
        {
            var modInfoFile = Path.Combine(modFolder, "mod.info");

            if (!File.Exists(modInfoFile))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Could not find mod.info for mod: {GetCurrentFolderName(modFolder)}");
                Console.ForegroundColor = _defaultColor;
                continue;
            }

            var modInfo = new ModInfo();
            var lines = await File.ReadAllLinesAsync(modInfoFile);

            foreach (var line in lines)
            {
                if (line.StartsWith("id="))
                {
                    modInfo.Id = line.Replace("id=", "");
                }
                else if (line.StartsWith("name="))
                {
                    modInfo.Name = line.Replace("name=", "");
                }
                else if (line.StartsWith("description="))
                {
                    modInfo.Description = line.Replace("description=", "");
                }
            }

            modInfos.Add(modInfo);
        }

        return modInfos;
    }

    private static string GetCurrentFolderName(string path) => Path.GetFileName(path);
}