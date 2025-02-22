using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Avalonia.Input;

namespace L2Move.Helpers;

public static class FileHelper
{
    private static readonly byte[] GZIP_SIGNATURE = [0x1F, 0x8B];
    
    public static bool GetFilesFromPathByExtension(string extension, string path, out List<string> fileList)
    {
        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }
        
        fileList = Directory.GetFiles(path, $"*{extension}")?.ToList() ?? new List<string>();
        
        return fileList.Count > 0;
    }

    public static bool GetPathFromDragEvent(DragEventArgs eventArgs, out string path)
    {
        path = default;
        
        if (!eventArgs.Data.Contains(DataFormats.Files))
        {
            return false;
        }

        var storageItem = eventArgs.Data.GetFiles()?.FirstOrDefault();
        if (storageItem == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(storageItem.Path?.LocalPath))
        {
            return false;
        }
        
        path = storageItem.Path.LocalPath;
        
        return true;
    }

    public static bool ContainsFilesWithExtension(string path, string extension)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }
        
        if (extension == Path.GetExtension(path))
        {
            return true;
        }
       
        return Directory.Exists(path) && FileHelper.GetFilesFromPathByExtension(extension, path, out _);
    }
    
    public static bool IsGZipFile(string filePath)
    {
        var fileHeader = new byte[2];

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        _ = fs.Read(fileHeader, 0, 2);

        return fileHeader[0] == GZIP_SIGNATURE[0] &&
               fileHeader[1] == GZIP_SIGNATURE[1];
    }
    
    public static string CombineFromCommonPath(string absolutePath, string relativePath)
    {
        var relativePathList = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var absolutePathList = absolutePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        // Find the first common folder
        var endAbsolute = -1;
        var startRelative = -1;

        for (var i = 0; i < absolutePathList.Length; i++)
        {
            for (var j = 0; j < relativePathList.Length; j++)
            {
                if (!absolutePathList[i].Equals(relativePathList[j], StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                endAbsolute = i;
                startRelative = j;
                
                break;
            }

            if (endAbsolute != -1)
            {
                break;
            }
        }

        // No common folder found
        if (endAbsolute == -1)
        {
            return string.Empty;
        }

        relativePath = string.Join(Path.DirectorySeparatorChar, relativePathList[(startRelative + 1)..]);
        absolutePath = Path.Combine(string.Join(Path.DirectorySeparatorChar, absolutePathList[..(endAbsolute + 1)]), relativePath);

        return $"{Path.DirectorySeparatorChar}{absolutePath}";
    }
    
    public static bool HexToPath(string hex, out string path)
    {
        path = default;
        
        // Clean the hex from tab, space, etc
        hex = Regex.Replace(hex, @"\s+", "");
        
        var sb = new StringBuilder();
        for (var i = 0; i < hex.Length - 1; i += 2)
        {
            var part = hex.Substring(i, 2);
            var num = Convert.ToInt32(part, 16);

            // Consider only readable characters
            if (num >= 32 && num <= 126)
            {
                sb.Append((char)num);
            }
        }
        
        // Search for possible file paths
        var matches = Regex.Matches(sb.ToString(), @"(/[\w/\-\. ]+\.\w{3,4})");
        if (matches.Count <= 0)
        {
            return false;
        }

        path = matches[0].Value;
        
        return true;
    }

    public static string GenerateNewFileName(string fileName)
    {
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        return $"{fileNameWithoutExtension}_{GeneralHelper.GetLocalDateNow()}{extension}";
    }
}