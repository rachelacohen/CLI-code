using System.CommandLine;
using System.IO.Compression;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;

static string[] SortFileNamesByType(string[] fileNames)
{
    var sortedFileNames = fileNames.OrderBy(fileName => GetFileType(fileName))
                                   .ThenBy(fileName => fileName);

    return sortedFileNames.ToArray();
}
static string GetFileType(string fileName)
{
    int lastDotIndex = fileName.LastIndexOf(".");
    if (lastDotIndex >= 0 && lastDotIndex < fileName.Length - 1)
    {
        return fileName.Substring(lastDotIndex + 1).ToLower();
    }

    return string.Empty;
}

string[] languages = { "cs", "c", "py", "cpp", "html", "css", "js", "ts" };

var bundleOption = new Option<FileInfo>("-o", "file path and name");
var languageOption = new Option<string>("-l", "set the name of the language (py, cs, java...)");
var noteOption = new Option<string>("-n", "write the source as note");
var authorOption = new Option<string>("-a", "write the name of the author as note");
var typeOption = new Option<bool>("-s", "sort by type of code");
var emptyOption = new Option<bool>("-e", "remove the empty lines");



var bundlecommand = new Command("bundle", "groups code files to a single file");
var rspcommand = new Command("create-rsp", "create rsp file");


bundlecommand.AddOption(bundleOption);
bundlecommand.AddOption(languageOption);
bundlecommand.AddOption(noteOption);
bundlecommand.AddOption(typeOption);
bundlecommand.AddOption(emptyOption);
bundlecommand.AddOption(authorOption);



bundlecommand.SetHandler<FileInfo, string, string, bool, bool, string?>((output, language, note, sort, empty, author) =>
{
    try
    {
        using (var outputFile = File.CreateText(output.FullName))
        {
            string projectFolderPath = Directory.GetCurrentDirectory();
            string[] codeFiles;
            if (language != "all")
            {
                codeFiles = Directory.GetFiles(projectFolderPath, $"*.{language}", SearchOption.AllDirectories);
            }
            else
            {
                codeFiles = Directory.GetFiles(projectFolderPath, "*", SearchOption.AllDirectories);
                codeFiles = codeFiles.Where(file => languages.Any(suffix => file.EndsWith(suffix))).ToArray();
            }
            if (!sort)
                Array.Sort(codeFiles);
            else
                codeFiles = SortFileNamesByType(codeFiles);

            if (author != null)
                outputFile.WriteLine($"//{author}");
            if (!empty)
                foreach (var codeFile in codeFiles)
                {
                    if (note != null)
                        outputFile.WriteLine($"// the file name: {codeFile}");

                    string code = File.ReadAllText(codeFile);
                    outputFile.WriteLine(code);
                    outputFile.WriteLine();
                }
            else
                foreach (var codeFile in codeFiles)
                {
                    if (note != null)
                        outputFile.WriteLine($"// the file name: {codeFile}");

                    string code = File.ReadAllText(codeFile);
                    string[] lines = code.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            outputFile.WriteLine(line);
                        }
                    }
                }
        }

        Console.WriteLine("Code bundle created successfully!");
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Error: The path is invalid!");
    }

}, bundleOption, languageOption, noteOption, typeOption, emptyOption, authorOption);


//string responseFilePath = Directory.GetCurrentDirectory();
rspcommand.SetHandler(() =>
    {
        var rspfile = File.CreateText("rspfile.rsp");
        try
        {
            rspfile.WriteLine("bundle");
            Console.WriteLine("Enter the name of the file:");
            string nameOf_file = Console.ReadLine().Trim();
            rspfile.WriteLine($"-o {nameOf_file}.txt");
            Console.WriteLine("Enter the language:");
            string languageName = Console.ReadLine().Trim();
            rspfile.WriteLine($"-l {languageName}");
            Console.WriteLine("you want a note? (y/n):");
            string noteCheck = Console.ReadLine()?.Trim();
            if (noteCheck == "y")
                rspfile.WriteLine($"-n");
            Console.WriteLine("sort by type code? (default- by ABC order) (y/n):");
            string byTpe = Console.ReadLine()?.Trim();
            if (byTpe == "y")
                rspfile.WriteLine($"-s");
            Console.WriteLine("remove empty lines? (y/n):");
            string empty = Console.ReadLine()?.Trim();
            if (empty == "y")
                rspfile.WriteLine("-e");
            Console.WriteLine("your name in the head of the page as commant:");
            string yourName = Console.ReadLine()?.Trim();
            rspfile.WriteLine($"-a {yourName}");         
        }

        catch (Exception ex)
        {
            Console.WriteLine("Error creating rsp file: " + ex.Message);
        }
        finally
        {
            rspfile.Close();
        }
    });


var rootbundle = new RootCommand("files to one");

rootbundle.AddCommand(bundlecommand);
rootbundle.AddCommand(rspcommand);
rootbundle.InvokeAsync(args);