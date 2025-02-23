using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using L2Move.Models;

namespace L2Move.Core;

public static class ReportGenerator
{
    #region Constants
    
    private const string HEADER_TEXT = "L2MOVE PROCESSING REPORT";

    private const string FILE_NAME_COLUMN = "File Name";
    private const string ADG_STATUS_COLUMN = "Adg Status";
    private const string PRESET_STATUS_COLUMN = "Preset Status";

    private const int SPACING = 10;
    
    #endregion
    
    public static void Generate(IEnumerable<ProcessResult> resultList, string path)
    {
        if (resultList == null || !resultList.Any())
        {
            Console.WriteLine("No files to report.");
            
            return;
        }

        var generatedLine = $"Generated on: {DateTime.Now}";
        
        var maxFileNameLength = Math.Max(FILE_NAME_COLUMN.Length, resultList.Max(f => f.SourceFileName.Length));
        var maxAdgStatusLength = Math.Max(ADG_STATUS_COLUMN.Length, resultList.Max(f => f.AdgValueString.Length));
        var maxPresetStatusLength = Math.Max(ADG_STATUS_COLUMN.Length, resultList.Max(f => f.PresetValueString.Length));
        
        var totalWidth = maxFileNameLength + SPACING + maxAdgStatusLength + SPACING + maxPresetStatusLength;
        totalWidth = Math.Max(totalWidth, generatedLine.Length);
        
        var thickSeparatorLine = new string('=', totalWidth);
        var thinSeparatorLine = new string('-', totalWidth);
        
        var headerPadding = (totalWidth - HEADER_TEXT.Length) / 2;
        var centeredHeader = new string(' ', headerPadding) + HEADER_TEXT;
        
        var reportContent = new StringBuilder();
        
        reportContent.AppendLine(thickSeparatorLine);
        reportContent.AppendLine(centeredHeader);
        reportContent.AppendLine(thickSeparatorLine);
        reportContent.AppendLine(generatedLine);
        reportContent.AppendLine();
        reportContent.AppendLine("Processed Files:");
        reportContent.AppendLine(thinSeparatorLine);
        reportContent.AppendLine($"{FILE_NAME_COLUMN.PadRight(maxFileNameLength + SPACING)}" +
                                 $"{ADG_STATUS_COLUMN.PadRight(maxAdgStatusLength + SPACING)}" +
                                 $"{PRESET_STATUS_COLUMN}");
        reportContent.AppendLine(thinSeparatorLine);

        foreach (var result in resultList)
        {
            reportContent.AppendLine($"{result.SourceFileName.PadRight(maxFileNameLength + SPACING)}" +
                                     $"{result.AdgValueString.PadRight(maxAdgStatusLength +SPACING)}" +
                                     $"{result.PresetValueString}");
        }
        
        reportContent.AppendLine(thickSeparatorLine);

        try
        {
            File.WriteAllText(path, reportContent.ToString(), Encoding.UTF8);
            
            Console.WriteLine($"Report successfully saved to: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing the report: {ex.Message}");
        }
    }
}