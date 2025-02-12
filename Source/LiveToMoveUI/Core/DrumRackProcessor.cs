using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO.Compression;
using System.Collections.Generic;

using LiveToMoveUI.Models;

namespace LiveToMoveUI.Core;

public abstract class DrumRackProcessor
{
    // TODO: insert all strings as a constants
    
    #region Nested classes
    
    /// <summary>
    /// Class representing a mapping between a DrumBranchPreset and a SampleRef
    /// </summary>
    private class DrumSample
    {
        public DrumSample(string id, XElement body, string receivingNote)
        {
            this.Id = id;
            this.Body = body;
            this.ReceivingNote = receivingNote;
        }

        public string Id { get; }
        
        public XElement Body { get; }
        
        public string ReceivingNote { get; }
    }
    
    #endregion
    
    #region Constants
    
    private static readonly string DEFAULT_TEMPLATE_FILE_NAME = Path.Combine("Resources", "template.xml");
    private static readonly byte[] GZIP_SIGNATURE = [0x1F, 0x8B];
    
    #endregion
    
    public static List<ProcessingResult> Process(List<string> sourcePathList, string targetPath)
    {
        var result = new List<ProcessingResult>();
        
        var xmlSourceTemplate = XDocument.Load(DEFAULT_TEMPLATE_FILE_NAME);
        
        foreach (var sourcePath in sourcePathList)
        {
            var xmlTemplateCopy = new XDocument(xmlSourceTemplate);
            
            result.Add(DrumRackProcessor.ProcessInternal(sourcePath, targetPath, xmlTemplateCopy));
        }

        return result;
    }

    private static ProcessingResult ProcessInternal(string sourcePath, string targetPath, XDocument xmlTemplate)
    {
        List<DrumSample> drumSampleList;

        var processingResult = new ProcessingResult() { Path = sourcePath };
        if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath) || xmlTemplate == null)
        {
            return processingResult.SetValue(ProcessingResult.ValueEnum.GenericError);
        }
        
        try
        {
            // Check if the ADG file is compressed (GZIP)
            if (DrumRackProcessor.IsGZipFile(sourcePath))
            {
                Console.WriteLine("The file is compressed. Extracting in memory...");

                // Open the ADG file as a FileStream and process it as a ZIP archive in memory
                using var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
                using var gZipStream = new GZipStream(fs, CompressionMode.Decompress);

                drumSampleList = DrumRackProcessor.ExtractDrumSamplesFromAdgStream(gZipStream);
            }
            else
            {
                // If not compressed, assume the file is an XML file
                Console.WriteLine("The file is not compressed. Processing as XML...");

                using var xmlFileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
                drumSampleList = DrumRackProcessor.ExtractDrumSamplesFromAdgStream(xmlFileStream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);

            return processingResult.SetValue(ProcessingResult.ValueEnum.GenericError);
        }

        if ((drumSampleList?.Count ?? 0) <= 0)
        {
            Console.WriteLine($"No drum samples found on {Path.GetFileName(sourcePath)}");
            
            return processingResult.SetValue(ProcessingResult.ValueEnum.SamplesNotFound);
        }

        var drumBranchPresetList = xmlTemplate.Descendants("DrumBranchPreset");
        foreach (var drumBranchPreset in drumBranchPresetList)
        {
            var presetId = drumBranchPreset.Attribute("Id")?.Value;
            if (presetId == null)
            {
                return processingResult.SetValue(ProcessingResult.ValueEnum.GenericError);
            }

            var drumSample = drumSampleList.FirstOrDefault(p => p.Id == presetId);
            if (drumSample == null)
            {
                return processingResult.SetValue(ProcessingResult.ValueEnum.GenericError);
            }

            var userSample = drumBranchPreset.Descendants("UserSample").FirstOrDefault();
            if (userSample == null)
            {
                return processingResult.SetValue(ProcessingResult.ValueEnum.GenericError);
            }

            // Injecting the data of the sample into the userSample element
            userSample.Element("Value")?.Add(drumSample.Body);

            if (string.IsNullOrEmpty(drumSample.ReceivingNote))
            {
                return processingResult.SetValue(ProcessingResult.ValueEnum.GenericError);
            }

            var zoneSettings = drumBranchPreset.Element("ZoneSettings");
            var receivingNote = zoneSettings?.Element("ReceivingNote");

            receivingNote?.SetAttributeValue("Value", drumSample.ReceivingNote);
        }

        var settings = new XmlWriterSettings
        {
            Indent = true, // Enable indent
            IndentChars = "\t", // Use TAB as an indent char
            Encoding = new UTF8Encoding(false), // Avoiding BOM (Byte Order Mark)
            OmitXmlDeclaration = false // To use the XML declaration
        };

        try
        {
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            targetPath = Path.Combine(targetPath, Path.GetFileName(sourcePath));

            using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
            using var writer = XmlWriter.Create(gzipStream, settings);
            xmlTemplate.Save(writer);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);

            return processingResult.SetValue(ProcessingResult.ValueEnum.GenericError);
        }
        
        return processingResult.SetValue(ProcessingResult.ValueEnum.Ok);
    }

    /// <summary>
    /// Checks if the file, based on file path, is gzip or not.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private static bool IsGZipFile(string filePath)
    {
        var fileHeader = new byte[2];

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        _ = fs.Read(fileHeader, 0, 2);

        return fileHeader[0] == GZIP_SIGNATURE[0] &&
               fileHeader[1] == GZIP_SIGNATURE[1];
    }
    
    /// <summary>
    /// Extract drum samples from an XML Stream, based on an ADG file.
    /// </summary>
    /// <param name="xmlStream"></param>
    /// <returns></returns>
    private static List<DrumSample> ExtractDrumSamplesFromAdgStream(Stream xmlStream)
    {
        try
        {
            var doc = XDocument.Load(xmlStream);
            
            // Find all DrumBranchPreset elements in the stream
            var drumBranchPresetList = doc.Descendants("DrumBranchPreset");
            var drumSampleList = new List<DrumSample>();

            foreach (var drumBranchPreset in drumBranchPresetList)
            {
                // Get the current branch ID, otherwise a string empty
                var drumBranchId = drumBranchPreset.Attribute("Id")?.Value ?? string.Empty;

                // Search for a SampleRef element anywhere within the current branch
                var sampleRefElement = drumBranchPreset.Descendants("SampleRef").FirstOrDefault();
        
                // Look for FileRef under the SampleRef element
                if (sampleRefElement?.Element("FileRef") == null)
                {
                    continue;
                }
                
                var zoneSettings = drumBranchPreset.Element("ZoneSettings");
                var receivingNote = zoneSettings?.Element("ReceivingNote");
                var receivingNoteValue = receivingNote?.Attribute("Value")?.Value;

                // Add the default id for safe
                sampleRefElement.SetAttributeValue("Id", 0);
        
                // To remove and add a SourceContext empty
                sampleRefElement.Descendants("SourceContext").Remove();
                sampleRefElement.Add(new XElement("SourceContext"));

                drumSampleList.Add(new DrumSample(drumBranchId, sampleRefElement, receivingNoteValue));
            }

            return drumSampleList;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error processing ADG XML: " + ex.Message);

            return new List<DrumSample>();
        }
    }
}