using System;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;

namespace L2Move.Helpers;

public static class GeneralHelper
{
    /// <summary>
    /// Get the current local date and time in the format yyyyMMddHHmmss.
    /// </summary>
    public static string GetLocalDateNow()
    {
        return DateTime.Now.ToString("yyyyMMddHHmmss");
    }
    
    /// <summary>
    /// Get the current UTC date and time in the format yyyyMMddHHmmss.
    /// </summary>
    public static string GetDateNow()
    {
        return DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    }
    
    /// <summary>
    /// Animate the button text with a loading effect.
    /// </summary>
    public static async Task AnimateButtonText(Button button, string text, CancellationToken token)
    {
        string[] animatedStates = [".", "..", "..."];
        
        var index = 0;
        while (!token.IsCancellationRequested)
        {
            button.Content = text + animatedStates[index];
            index = (index + 1) % animatedStates.Length;
            
            await Task.Delay(250, token); 
        }
    }
}