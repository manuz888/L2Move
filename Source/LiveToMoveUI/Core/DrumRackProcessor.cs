using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO.Compression;
using System.Collections.Generic;

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
    
    #region Enums

    public enum ProcessingResult
    {
        Ok,
        GenericError,
        SamplesNotFound
    }
    
    #endregion
    
    #region Constants
    
    private const string DEFAULT_ADG_TARGET_DIR = "Processed";
    private static readonly string DEFAULT_TEMPLATE_FILE_NAME = Path.Combine("Resources", "template.xml");
    private static readonly byte[] GZIP_SIGNATURE = [0x1F, 0x8B];
    
    #endregion
    
    public static Dictionary<string, ProcessingResult> Process(List<string> sourceFiles)
    {
        var result = new Dictionary<string, ProcessingResult>();
        
        var xmlSourceTemplate = XDocument.Load(DEFAULT_TEMPLATE_FILE_NAME);
        
        foreach (var sourceFile in sourceFiles)
        {
            var xmlTemplateCopy = new XDocument(xmlSourceTemplate);
            
            result.Add(sourceFile, DrumRackProcessor.ProcessInternal(sourceFile, xmlTemplateCopy));
        }

        return result;
    }

    private static ProcessingResult ProcessInternal(string sourceFile, XDocument xmlTemplate)
    {
        List<DrumSample> drumSampleList;

        if (string.IsNullOrEmpty(sourceFile) || xmlTemplate == null)
        {
            return ProcessingResult.GenericError;
        }
        
        try
        {
            // Check if the ADG file is compressed (GZIP)
            if (DrumRackProcessor.IsGZipFile(sourceFile))
            {
                Console.WriteLine("The file is compressed. Extracting in memory...");

                // Open the ADG file as a FileStream and process it as a ZIP archive in memory
                using var fs = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
                using var gZipStream = new GZipStream(fs, CompressionMode.Decompress);

                drumSampleList = DrumRackProcessor.ExtractDrumSamplesFromAdgStream(gZipStream);
            }
            else
            {
                // If not compressed, assume the file is an XML file
                Console.WriteLine("The file is not compressed. Processing as XML...");

                using var xmlFileStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
                drumSampleList = DrumRackProcessor.ExtractDrumSamplesFromAdgStream(xmlFileStream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);

            return ProcessingResult.GenericError;
        }

        if ((drumSampleList?.Count ?? 0) <= 0)
        {
            Console.WriteLine($"No drum samples found on {Path.GetFileName(sourceFile)}");
            
            return ProcessingResult.SamplesNotFound;
        }

        var drumBranchPresetList = xmlTemplate.Descendants("DrumBranchPreset");
        foreach (var drumBranchPreset in drumBranchPresetList)
        {
            var presetId = drumBranchPreset.Attribute("Id")?.Value;
            if (presetId == null)
            {
                return ProcessingResult.GenericError;
            }

            var drumSample = drumSampleList.FirstOrDefault(p => p.Id == presetId);
            if (drumSample == null)
            {
                return ProcessingResult.GenericError;
            }

            var userSample = drumBranchPreset.Descendants("UserSample").FirstOrDefault();
            if (userSample == null)
            {
                return ProcessingResult.GenericError;
            }

            // Injecting the data of the sample into the userSample element
            userSample.Element("Value")?.Add(drumSample.Body);

            if (string.IsNullOrEmpty(drumSample.ReceivingNote))
            {
                return ProcessingResult.GenericError;
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
            var processedPath = Path.Combine(Path.GetDirectoryName(sourceFile), DEFAULT_ADG_TARGET_DIR);
            if (!Directory.Exists(processedPath))
            {
                Directory.CreateDirectory(processedPath);
            }

            processedPath = Path.Combine(processedPath, Path.GetFileName(sourceFile));

            using var fileStream = new FileStream(processedPath, FileMode.Create, FileAccess.Write);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
            using var writer = XmlWriter.Create(gzipStream, settings);
            xmlTemplate.Save(writer);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);

            return ProcessingResult.GenericError;
        }
        
        return ProcessingResult.Ok;
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