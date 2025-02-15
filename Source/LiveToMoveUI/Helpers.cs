using System.IO;
using System.Linq;
using System.Collections.Generic;

using Avalonia.Input;

namespace LiveToMoveUI;

public static class Helpers
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
       
        return Directory.Exists(path) && Helpers.GetFilesFromPathByExtension(extension, path, out _);
    }
    
    public static bool IsGZipFile(string filePath)
    {
        var fileHeader = new byte[2];

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        _ = fs.Read(fileHeader, 0, 2);

        return fileHeader[0] == GZIP_SIGNATURE[0] &&
               fileHeader[1] == GZIP_SIGNATURE[1];
    }
}