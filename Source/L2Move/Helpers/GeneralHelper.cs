using System;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;

namespace L2Move.Helpers;

public static class GeneralHelper
{
    public static string GetLocalDateNow()
    {
        return DateTime.Now.ToString("yyyyMMddHHmmss");
    }
    
    public static string GetDateNow()
    {
        return DateTime.UtcNow.ToString("yyyyMMddHHmmss");
    }
    
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