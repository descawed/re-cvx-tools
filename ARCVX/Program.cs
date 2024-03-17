using ARCVX.Formats;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ARCVX
{
    internal class Program
    {
        private const string EXTRACT = ".extract";

        private static HashSet<string> Convert { get; } = [EXTRACT, ".tex", ".mes", ".evt"];

        private static async Task<int> Main(string[] args)
        {
            if (args.Length > 0 && Path.Exists(args[0]))
            {
                string ext = Path.GetExtension(args[0]);

                if (Convert.Contains(ext))
                    args = ["convert", "--path", ..args];
                else
                    args = ["extract", "--path", ..args];
            }

            Option<string> arcOption = new(
                aliases: ["-p", "--path"],
                description: "Path to .arc file or folder containing .arc files",
                isDefault: true,
                parseArgument: result =>
                {
                    try
                    {
                        string path = result.Tokens.Single().Value;

                        if (!Path.Exists(path))
                            throw new Exception();

                        return path;
                    }
                    catch
                    {
                        result.ErrorMessage = "Path does not exist";
                        return null;
                    }
                }
            );

            Option<string> pathOption = new(
                aliases: ["-p", "--path"],
                description: "Path to content file or folder",
                isDefault: true,
                parseArgument: result =>
                {
                    try
                    {
                        string path = result.Tokens.Single().Value;

                        if (!Path.Exists(path))
                            throw new Exception();

                        return path;
                    }
                    catch
                    {
                        result.ErrorMessage = "Path does not exist";
                        return null;
                    }
                }
            );

            Option<DirectoryInfo> extractOption = new(
                aliases: ["-e", "--extract"],
                description: $"Optional path to extract .arc contents (<name>{EXTRACT})"
            );

            Option<DirectoryInfo> rebuildOption = new(
                aliases: ["-r", "--rebuild"], 
                description: $"Optional path to folder with content to rebuild .arc container (<name>{EXTRACT})"
            );

            RootCommand rootCommand = new("Extract and rebuild Resident Evil: Code: Veronica X HD .arc files");

            Command extractCommand = new("extract", "Extract .arc container") { arcOption, extractOption };
            extractCommand.SetHandler((path, extract) => { ExtractCommand(path!, extract!); }, arcOption, extractOption);
            rootCommand.AddCommand(extractCommand);

            Command rebuildCommand = new("rebuild", "Rebuild .arc container") { arcOption, rebuildOption, };
            rebuildCommand.SetHandler((path, rebuild) => { RebuildCommand(path!, rebuild!); }, arcOption, rebuildOption);
            rootCommand.AddCommand(rebuildCommand);

            Command convertCommand = new("convert", "Convert files to readable formats") { pathOption };
            convertCommand.SetHandler((path) => { ConvertCommand(path!); }, pathOption);
            rootCommand.AddCommand(convertCommand);

            return await rootCommand.InvokeAsync(args);
        }

        public static void ExtractCommand(string path, DirectoryInfo extract = null)
        {
            DirectoryInfo folder = new(path);
            List<FileInfo> files = [];

            if (folder.Exists)
                files = [.. new DirectoryInfo(path).GetFiles("*.arc", SearchOption.AllDirectories)];
            else
                files.Add(new(path));

            if (files.Count < 1)
            {
                Console.WriteLine($"No .arc files found in directory.");
                Console.ReadLine();
                return;
            }

            foreach (FileInfo file in files)
            {
                DirectoryInfo output =
                    extract != null && extract.Exists ?
                    extract :

                    folder.Exists ?
                    new($"{folder.FullName}{EXTRACT}") :
                    new(Path.Combine(file.Directory.FullName,
                        Path.ChangeExtension(file.Name, EXTRACT)));

                ExtractARC(file, output);
            }

            Console.WriteLine();
            Console.WriteLine($"ARC extraction complete.");
            Console.ReadLine();
        }

        public static void RebuildCommand(string path, DirectoryInfo rebuild = null)
        {
            DirectoryInfo folder = new(path);
            List<FileInfo> files = [];

            if (folder.Exists)
                files = [.. new DirectoryInfo(path).GetFiles("*.arc", SearchOption.AllDirectories)];
            else
                files.Add(new(path));

            if (files.Count < 1)
            {
                Console.WriteLine($"No .arc files found in directory.");
                Console.ReadLine();
                return;
            }

            foreach (FileInfo file in files)
            {
                DirectoryInfo input =
                    rebuild != null && rebuild.Exists ?
                    rebuild :

                    folder.Exists ?
                    new($"{folder.FullName}{EXTRACT}") :
                    new(Path.Combine(file.Directory.FullName,
                        Path.ChangeExtension(file.Name, EXTRACT)));

                if (input.Exists)
                    RebuildARC(file, input);
            }

            Console.WriteLine();
            Console.WriteLine($"ARC rebuild complete.");
            Console.ReadLine();
        }

        public static void ConvertCommand(string path)
        {
            DirectoryInfo folder = new(path);
            List<FileInfo> files = [];

            if (folder.Exists)
                files = [.. new DirectoryInfo(path).GetFiles(".tex;*.mes;*.evt", SearchOption.AllDirectories)];
            else
                files.Add(new(path));

            if (files.Count < 1)
            {
                Console.WriteLine($"No convertable files found in directory.");
                Console.ReadLine();
                return;
            }

            foreach (FileInfo file in files)
            {
                if (file.Extension == ".tex")
                    ConvertTexture(file);

                /*if (file.Extension == ".mes")
                    ConvertMessage(file);

                if (file.Extension == ".evt")
                    ConvertScript(file);*/
            }

            Console.WriteLine();
            Console.WriteLine($"File conversion complete.");
            Console.ReadLine();
        }

        public static void ExtractARC(FileInfo file, DirectoryInfo folder)
        {
            HFS hfs = new(file);
            using ARC arc = hfs.IsValid ? new(file, hfs.GetDataStream()) : new(file);
            hfs.Dispose();

            if (!arc.IsValid)
            {
                Console.WriteLine($"{file.FullName} is not a supported ARC file.");
                return;
            }

            Console.WriteLine($"Extracting {file.FullName}");
            Console.WriteLine();

            foreach (ARCExport export in arc.ExportAllEntries(folder))
            {
                if (export == null)
                {
                    Console.Error.WriteLine($"Failed {export.File}");
                    continue;
                }

                Console.WriteLine($"Extracted {export.File}");

                if (file.Extension == ".tex")
                    ConvertTexture(file);

                /*if (file.Extension == ".mes")
                    ConvertMessage(file);

                if (file.Extension == ".evt")
                    ConvertScript(file);*/
            }

            Console.WriteLine("---------------------------------");
        }

        public static void RebuildARC(FileInfo file, DirectoryInfo folder)
        {
            using HFS hfs = new(file);
            using ARC arc = hfs.IsValid ? new(file, hfs.GetDataStream()) : new(file);

            if (!arc.IsValid)
            {
                Console.WriteLine($"{file.FullName} is not a supported ARC file.");
                return;
            }

            if (hfs.IsValid)
            {
                //using MemoryStream stream = arc.CreateNewStream(folder);
                //_ = hfs.Save(stream);
                _ = arc.Save(folder, new(Path.ChangeExtension(arc.File.FullName, ".tmp")));
            }
            else
            {
                //_ = arc.Save(folder);
                _ = arc.Save(folder, new(Path.ChangeExtension(arc.File.FullName, ".tmp")));
            }
        }

        public static void ConvertTexture(FileInfo file)
        {
            HFS hfs = new(file);
            using Tex tex = hfs.IsValid ? new(file, hfs.GetDataStream()) : new(file);
            hfs.Dispose();

            FileInfo output;
            if ((output = tex.Export()) != null)
                Console.WriteLine("Converted " + output.FullName);
        }

        // TODO: Convert message files.
        public static void ConvertMessage(FileInfo file)
        {
            using Mes mes = new(file);

            FileInfo output;
            if ((output = mes.Export()) != null)
                Console.WriteLine("Converted " + output.FullName);
        }

        // TODO: Convert script files.
        public static void ConvertScript(FileInfo file)
        {
            using Evt evt = new(file);

            FileInfo output;
            if ((output = evt.Export()) != null)
                Console.WriteLine("Converted " + output.FullName);
        }
    }
}