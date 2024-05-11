namespace HZDCoreTools;

using CommandLine;
using CommandLine.Text;
using Decima;
using HZDCoreTools.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Archive index utilities.
/// </summary>
public static class ArchiveIndex
{
/// <summary>
/// Represents a verb for exporting paths contained in a set of archive index files.
/// </summary>
[Verb("exportindexstrings", HelpText = "Extract all paths contained in a set of archive index files.")]
public class ExportIndexFilesCommand
{
    /// <summary>
    /// Gets or sets the input path for game data (.idx). Supports wildcards (*).
    /// </summary>
    [Option('i', "input", Required = true, HelpText = "OS input path for game data (.idx). Wildcards (*) supported.")]
    public string InputPath { get; set; }

    /// <summary>
    /// Gets or sets the output path for the generated text file (.txt, *.*).
    /// </summary>
    [Option('o', "output", Required = true, HelpText = "OS output path for the generated text file (.txt, *.*).")]
    public string OutputPath { get; set; }

    /// <summary>
    /// Gets the examples for this verb.
    /// </summary>
    [Usage(ApplicationAlias = nameof(HZDCoreTools))]
    public static IEnumerable<Example> Examples
    {
        get
        {
            // Provides an example of how to use this verb.
            yield return new Example("Extract single index", new ExportIndexFilesCommand
            {
                InputPath = @"E:\HZD\Packed_DX12\Initial.idx",
                OutputPath = @"E:\HZD\Packed_DX12\valid_file_lines.txt",
            });
        }
    }
}

    /// <summary>
    /// This class represents the command to rebuild index files from a set of archives.
    /// </summary>
[Verb("rebuildindexfiles", HelpText = "Rebuild index files from a set of archives.")]
public class RebuildIndexFilesCommand
    {
        /// <summary>
        /// Gets or sets the OS input path for game data (.bin). Wildcards (*) are supported.
        /// </summary>
        [Option('i', "input", Required = true, HelpText = "OS input path for game data (.bin). Wildcards (*) supported.")]
        public string InputPath { get; set; }

        /// <summary>
        /// Gets or sets the OS output directory for the generated index files (.idx).
        /// </summary>
        [Option('o', "output", Required = true, HelpText = "OS output directory for the generated index files (.idx).")]
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the OS input path for a text file containing possible core file paths (.txt, *.*).
        /// </summary>
        [Option('l', "lookupfile", Required = true, HelpText = "OS input path for a text file containing possible core file paths (.txt, *.*).")]
        public string LookupFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip creating entries if paths can't be mapped from the lookup file.
        /// </summary>
        [Option('s', "skipmissing", HelpText = "Skip creating entries if paths can't be mapped from the lookup file.")]
        public bool SkipMissing { get; set; }

        /// <summary>
        /// Gets the usage examples for this command.
        /// </summary>
        [Usage(ApplicationAlias = nameof(HZDCoreTools))]
        public static IEnumerable<Example> Examples
        {
            get
            {
                // Example 1: Update all
                yield return new Example("Update all", new RebuildIndexFilesCommand
                {
                    InputPath = @"E:\HZD\Packed_DX12\*.bin",
                    OutputPath = @"E:\HZD\Packed_DX12\",
                    LookupFile = "valid_file_lines.txt",
                });

                // Example 2: Update single bin
                yield return new Example("Update single bin", new RebuildIndexFilesCommand
                {
                    InputPath = @"E:\HZD\Packed_DX12\DLC1.bin",
                    OutputPath = @"E:\HZD\Packed_DX12\",
                    LookupFile = "DLC1_file_lines.txt",
                });
            }
        }
    }

    /// <summary>
    /// Export all paths contained in a set of archive index files.
    /// </summary>
    /// <param name="options">The command options.</param>
public static void ExportIndexFiles(ExportIndexFilesCommand options)
    {
        var sourceIndexes = Utils.GatherFiles(options.InputPath, new[] { ".idx" }, out _);
        var allValidPaths = new ConcurrentDictionary<string, bool>();

        foreach (var (indexPath, _) in sourceIndexes)
        {
            var packfileIndex = PackfileIndex.FromFile(indexPath);

            foreach (string corePath in packfileIndex.Entries.Select(x => x.FilePath))
            {
                string pathStr = Utils.RemoveMountPrefixes(corePath.ToLower());
                allValidPaths.TryAdd(pathStr, true);
            }
        }

        File.WriteAllLines(options.OutputPath, allValidPaths.Keys.OrderBy(x => x));
    }

    /// <summary>
    /// Rebuild index files from a set of archives.
    /// </summary>
    /// <param name="options">The command options.</param>
public static void RebuildIndexFiles(RebuildIndexFilesCommand options)
    {
        // Create table of lookup strings
        var fileLines = File.ReadAllLines(options.LookupFile);
        var lookupTable = new Dictionary<ulong, string>();

        foreach (string line in fileLines)
            lookupTable.TryAdd(Packfile.GetHashForPath(line), line);

        // Then apply them to the bins
        var sourceArchives = Utils.GatherFiles(options.InputPath, new[] { ".bin" }, out _);

        foreach ((string absolutePath, string relativePath) in sourceArchives)
        {
            Console.Write($"Processing {relativePath}...");

            using var archive = new PackfileReader(absolutePath);
            var index = PackfileIndex.RebuildFromArchive(archive, lookupTable, options.SkipMissing);

            Console.WriteLine($"Possible entries: {archive.FileEntries.Count} Mapped entries: {index.Entries.Count}");

            index.ToFile(Path.ChangeExtension(absolutePath, ".idx"), FileMode.Create);
        }
    }
}