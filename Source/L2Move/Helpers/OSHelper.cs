using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace L2Move.Helpers;

public static class OSHelper
{
    /// <summary>
    /// Open a folder in Finder on macOS (so far).
    /// </summary>
    public static void OpenFolderInFinder(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
            
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }
                
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                ArgumentList = { path },
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening folder: {ex.Message}");
        }
    }
}