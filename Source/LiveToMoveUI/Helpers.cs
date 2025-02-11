using System.IO;
using System.Linq;
using System.Collections.Generic;

using Avalonia.Input;

namespace LiveToMoveUI;

public static class Helpers
{
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
}