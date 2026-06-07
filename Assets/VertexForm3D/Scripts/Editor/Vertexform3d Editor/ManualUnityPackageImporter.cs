using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports a .unitypackage by manually extracting its tar.gz contents and copying
/// assets into the project, skipping paths that should not be imported.
/// Used when Unity's internal PackageUtility API is unavailable.
/// </summary>
internal static class ManualUnityPackageImporter
{
    private const int BufferSize = 4096;
    private const int TarHeaderSize = 512;

    private struct PackageEntry
    {
        public string GuidFolder;
        public string AssetPath;
    }

    public static bool TryImportPackage(
        string packagePath,
        string[] protectedFolders,
        string[] protectedFiles,
        out int excludedCount,
        out int importedCount,
        out string error)
    {
        excludedCount = 0;
        importedCount = 0;
        error = null;

        if (!File.Exists(packagePath))
        {
            error = $"Package file not found: {packagePath}";
            return false;
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string extractRoot = Path.Combine(projectRoot, "Library", "VertexForm3D_ManualPackageExtract");

        try
        {
            if (Directory.Exists(extractRoot))
                Directory.Delete(extractRoot, true);

            Directory.CreateDirectory(extractRoot);

            List<PackageEntry> entries = ReadPackageEntries(packagePath);
            if (entries.Count == 0)
            {
                Debug.Log("[ManualUnityPackageImporter] Package has no importable assets.");
                return true;
            }

            HashSet<string> allowedGuidFolders = new HashSet<string>();
            foreach (PackageEntry entry in entries)
            {
                if (SelectivePackageImporter.ShouldExcludeProtectedScene(entry.AssetPath, protectedFolders, protectedFiles))
                {
                    excludedCount++;
                    continue;
                }

                allowedGuidFolders.Add(entry.GuidFolder);
            }

            if (excludedCount == 0)
            {
                Debug.LogWarning("[ManualUnityPackageImporter] Toggle is on but no Database Scenes assets were found in the package to exclude.");
            }

            ExtractAllowedEntries(packagePath, extractRoot, allowedGuidFolders);
            importedCount = CopyExtractedAssetsToProject(extractRoot, projectRoot, allowedGuidFolders);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log($"[ManualUnityPackageImporter] Imported {importedCount} asset(s), skipped {excludedCount} Database Scenes asset(s).");
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            Debug.LogError($"[ManualUnityPackageImporter] Failed: {ex}");
            return false;
        }
        finally
        {
            if (Directory.Exists(extractRoot))
            {
                try { Directory.Delete(extractRoot, true); }
                catch (Exception ex) { Debug.LogWarning($"[ManualUnityPackageImporter] Failed to clean extract folder: {ex.Message}"); }
            }
        }
    }

    private static List<PackageEntry> ReadPackageEntries(string packagePath)
    {
        List<PackageEntry> entries = new List<PackageEntry>();
        Dictionary<string, string> guidToPath = new Dictionary<string, string>();

        using (FileStream fileStream = File.OpenRead(packagePath))
        using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
        {
            byte[] header = new byte[TarHeaderSize];
            byte[] buffer = new byte[BufferSize];

            while (ReadFully(gzipStream, header, 0, TarHeaderSize))
            {
                string entryName = Encoding.ASCII.GetString(header, 0, 100).Trim('\0', ' ').Trim();
                if (string.IsNullOrEmpty(entryName))
                    break;

                if (entryName.StartsWith("./", StringComparison.Ordinal))
                    entryName = entryName.Length > 2 ? entryName.Substring(2) : string.Empty;

                if (string.IsNullOrEmpty(entryName))
                {
                    SkipTarPadding(gzipStream, header, buffer, 0);
                    continue;
                }

                long size = Convert.ToInt64(Encoding.ASCII.GetString(header, 124, 12).Trim('\0', ' '), 8);

                if (size > 0 && string.Equals(Path.GetFileName(entryName), "pathname", StringComparison.OrdinalIgnoreCase))
                {
                    string guidFolder = Path.GetDirectoryName(entryName)?.Replace('\\', '/');
                    string assetPath = ReadTarStringContent(gzipStream, buffer, size).Trim();
                    int newLineIndex = assetPath.IndexOf('\n');
                    if (newLineIndex >= 0)
                        assetPath = assetPath.Substring(0, newLineIndex);
                    assetPath = assetPath.Trim().Replace('\\', '/');

                    if (!string.IsNullOrEmpty(guidFolder) && !string.IsNullOrEmpty(assetPath))
                        guidToPath[guidFolder] = assetPath;
                }
                else if (size > 0)
                {
                    SkipBytes(gzipStream, buffer, size);
                }

                SkipTarPadding(gzipStream, header, buffer, size);
            }
        }

        foreach (KeyValuePair<string, string> pair in guidToPath)
        {
            entries.Add(new PackageEntry
            {
                GuidFolder = pair.Key,
                AssetPath = pair.Value
            });
        }

        return entries;
    }

    private static void ExtractAllowedEntries(string packagePath, string extractRoot, HashSet<string> allowedGuidFolders)
    {
        using (FileStream fileStream = File.OpenRead(packagePath))
        using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
        {
            byte[] header = new byte[TarHeaderSize];
            byte[] buffer = new byte[BufferSize];

            while (ReadFully(gzipStream, header, 0, TarHeaderSize))
            {
                string entryName = Encoding.ASCII.GetString(header, 0, 100).Trim('\0', ' ').Trim();
                if (string.IsNullOrEmpty(entryName))
                    break;

                if (entryName.StartsWith("./", StringComparison.Ordinal))
                    entryName = entryName.Length > 2 ? entryName.Substring(2) : string.Empty;

                if (string.IsNullOrEmpty(entryName))
                {
                    SkipTarPadding(gzipStream, header, buffer, 0);
                    continue;
                }

                long size = Convert.ToInt64(Encoding.ASCII.GetString(header, 124, 12).Trim('\0', ' '), 8);
                string normalizedEntryName = entryName.Replace('\\', '/');
                bool shouldExtract = ShouldExtractTarEntry(normalizedEntryName, allowedGuidFolders);

                if (size > 0)
                {
                    if (shouldExtract && !string.Equals(normalizedEntryName, ".icon.png", StringComparison.OrdinalIgnoreCase))
                    {
                        string outputPath = Path.Combine(extractRoot, normalizedEntryName);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        WriteTarEntryContent(gzipStream, buffer, outputPath, size);
                    }
                    else
                    {
                        SkipBytes(gzipStream, buffer, size);
                    }
                }

                SkipTarPadding(gzipStream, header, buffer, size);
            }
        }
    }

    private static int CopyExtractedAssetsToProject(string extractRoot, string projectRoot, HashSet<string> allowedGuidFolders)
    {
        int importedCount = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guidFolder in allowedGuidFolders)
            {
                string sourceDirectory = Path.Combine(extractRoot, guidFolder);
                if (!Directory.Exists(sourceDirectory))
                    continue;

                string pathnameFile = Path.Combine(sourceDirectory, "pathname");
                if (!File.Exists(pathnameFile))
                    continue;

                string assetPath = File.ReadAllText(pathnameFile).Trim();
                int newLineIndex = assetPath.IndexOf('\n');
                if (newLineIndex >= 0)
                    assetPath = assetPath.Substring(0, newLineIndex);
                assetPath = assetPath.Trim().Replace('\\', '/');

                string destinationPath = Path.Combine(projectRoot, assetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                string sourceAsset = Path.Combine(sourceDirectory, "asset");
                if (File.Exists(sourceAsset))
                {
                    File.Copy(sourceAsset, destinationPath, true);
                    importedCount++;
                }
                else
                {
                    Directory.CreateDirectory(destinationPath);
                }

                string sourceMeta = Path.Combine(sourceDirectory, "asset.meta");
                if (File.Exists(sourceMeta))
                    File.Copy(sourceMeta, destinationPath + ".meta", true);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        return importedCount;
    }

    private static bool ShouldExtractTarEntry(string entryName, HashSet<string> allowedGuidFolders)
    {
        if (string.IsNullOrEmpty(entryName))
            return false;

        int slashIndex = entryName.IndexOf('/');
        if (slashIndex <= 0)
            return allowedGuidFolders.Contains(entryName);

        string guidFolder = entryName.Substring(0, slashIndex);
        return allowedGuidFolders.Contains(guidFolder);
    }

    private static string ReadTarStringContent(Stream stream, byte[] buffer, long size)
    {
        using (MemoryStream memoryStream = new MemoryStream((int)Math.Max(size, 0)))
        {
            CopyStreamBytes(stream, buffer, memoryStream, size);
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }

    private static void WriteTarEntryContent(Stream stream, byte[] buffer, string outputPath, long size)
    {
        using (FileStream outputStream = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            CopyStreamBytes(stream, buffer, outputStream, size);
        }
    }

    private static void CopyStreamBytes(Stream input, byte[] buffer, Stream output, long size)
    {
        long remaining = size;
        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int bytesRead = input.Read(buffer, 0, toRead);
            if (bytesRead <= 0)
                break;

            output.Write(buffer, 0, bytesRead);
            remaining -= bytesRead;
        }
    }

    private static void SkipBytes(Stream stream, byte[] buffer, long size)
    {
        long remaining = size;
        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            int bytesRead = stream.Read(buffer, 0, toRead);
            if (bytesRead <= 0)
                break;

            remaining -= bytesRead;
        }
    }

    private static void SkipTarPadding(Stream stream, byte[] header, byte[] buffer, long size)
    {
        int offset = TarHeaderSize - (int)(size % TarHeaderSize);
        if (offset > 0 && offset < TarHeaderSize)
            SkipBytes(stream, buffer, offset);
    }

    private static bool ReadFully(Stream stream, byte[] buffer, int offset, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = stream.Read(buffer, offset + totalRead, count - totalRead);
            if (read <= 0)
                return totalRead == count;

            totalRead += read;
        }

        return true;
    }
}
