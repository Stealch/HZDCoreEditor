namespace HZDCoreTools.Util;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Decima;

/// <summary>
/// Utility methods.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Gathers files from the given path.
    /// </summary>
    /// <param name="inputPath">The path from which files are gathered.</param>
    /// <param name="acceptedExtensions">The accepted file extensions. If null, any file extension is accepted.</param>
    /// <param name="extension">The extension of the gathered files.</param>
    /// <returns>An enumerable of tuples containing the absolute and relative paths of the gathered files.</returns>
    /// <exception cref="ArgumentException">Thrown when an invalid path is supplied.</exception>
    public static IEnumerable<(string Absolute, string Relative)> GatherFiles(string inputPath, string[] acceptedExtensions, out string extension)
    {
        // If no directory is supplied, use the current working dir
        string basePath = Path.GetDirectoryName(inputPath);
        string filePart = Path.GetFileName(inputPath);

        if (string.IsNullOrEmpty(basePath))
            basePath = @".\";

        if (acceptedExtensions != null)
        {
            extension = acceptedExtensions.SingleOrDefault(x => filePart.EndsWith(x));

            if (extension == null)
                throw new ArgumentException($"Invalid path supplied. Supported file extension(s): {string.Join(',', acceptedExtensions)}", nameof(inputPath));
        }
        else
        {
            // Accept anything
            extension = Path.GetExtension(filePart);
        }

        return Directory.EnumerateFiles(basePath, filePart, SearchOption.AllDirectories)
            .Select(x => (x, x.Substring(basePath.Length + 1)));
    }

    /// <summary>
    /// Removes mount prefixes from the given path.
    /// </summary>
    /// <param name="path">The path from which mount prefixes are to be removed.</param>
    /// <returns>The path with the mount prefixes removed.</returns>
    public static string RemoveMountPrefixes(string path)
    {
        foreach (string p in PackfileDevice.ValidMountPrefixes.Where(x => path.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
            return path.Substring(p.Length);

        return path;
    }

    /// <summary>
    /// Extracts the core binary from the given device.
    /// </summary>
    /// <param name="device">The device from which to extract the core binary.</param>
    /// <param name="corePath">The path of the core binary to extract.</param>
    /// <returns>The core binary extracted from the device.</returns>
    public static CoreBinary ExtractCoreBinaryInMemory(PackfileDevice device, string corePath)
    {
        using var ms = new MemoryStream();
        device.ExtractFile(corePath, ms);

        ms.Position = 0;
        return CoreBinary.FromData(new BinaryReader(ms));
    }

    /// <summary>
    /// Extracts the core binary from the given device using the specified path ID.
    /// </summary>
    /// <param name="device">The device from which to extract the core binary.</param>
    /// <param name="pathId">The path ID of the core binary to extract.</param>
    /// <returns>The extracted core binary.</returns>
    public static CoreBinary ExtractCoreBinaryInMemory(PackfileDevice device, ulong pathId)
    {
        using var ms = new MemoryStream();
        device.ExtractFile(pathId, ms);

        ms.Position = 0;
        return CoreBinary.FromData(new BinaryReader(ms));
    }
}
