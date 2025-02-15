using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using LiveToMoveUI.Models;

namespace LiveToMoveUI.Core;

public static class ReportGenerator
{
    #region Constants

    private const string FILE_NAME_COLUMN = "File Name";
    private const string STATUS_COLUMN = "Status";
    
    private const string HEADER_TEXT = "FILE PROCESSING REPORT";

    private const int SPACING = 10;    
    
    #endregion
    
    public static void Generate(List<ProcessingResult> resultList, string path)
    {
        if (resultList == null || resultList.Count <= 0)
        {
            Console.WriteLine("No files to report.");
            
            return;
        }

        var generatedLine = $"Generated on: {DateTime.Now}";
        
        var maxFileNameLength = Math.Max(FILE_NAME_COLUMN.Length, resultList.Max(f => f.FileName.Length));
        var maxStatusLength = Math.Max(STATUS_COLUMN.Length, resultList.Max(f => f.ValueString.Length));

        var totalWidth = maxFileNameLength + maxStatusLength + SPACING;
        totalWidth = Math.Max(totalWidth, generatedLine.Length);
        
        var separatorLine = new string('=', totalWidth);
        
        var headerPadding = (totalWidth - HEADER_TEXT.Length) / 2;
        var centeredHeader = new string(' ', headerPadding) + HEADER_TEXT;
        
        var reportContent = new StringBuilder();
        reportContent.AppendLine(new string('=', totalWidth));
        reportContent.AppendLine(centeredHeader);
        reportContent.AppendLine(new string('=', totalWidth));
        reportContent.AppendLine(generatedLine);
        reportContent.AppendLine();
        reportContent.AppendLine("Processed Files:");
        reportContent.AppendLine(new string('-', totalWidth));
        reportContent.AppendLine($"{FILE_NAME_COLUMN.PadRight(maxFileNameLength + SPACING)}{STATUS_COLUMN.PadRight(maxStatusLength)}");
        reportContent.AppendLine(new string('-', totalWidth));

        foreach (var result in resultList)
        {
            reportContent.AppendLine($"{result.FileName.PadRight(maxFileNameLength + SPACING)}{result.ValueString.PadRight(maxStatusLength)}");
        }
        
        reportContent.AppendLine(new string('=', totalWidth));

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