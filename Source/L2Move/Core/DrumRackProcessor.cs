using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO.Compression;
using System.Collections.Generic;

using L2Move.Models;

namespace L2Move.Core;

public static class DrumRackProcessor
{
    // TODO: insert all strings as a constants
    
    private static readonly string DEFAULT_TEMPLATE_FILE_NAME = Path.Combine(AppContext.BaseDirectory, "Resources", "template.xml");
    
    public static IEnumerable<SamplesProcessResult> Process(List<string> sourcePathList, string targetPath)
    {
        var result = new List<SamplesProcessResult>();
        
        var xmlSourceTemplate = XDocument.Load(DEFAULT_TEMPLATE_FILE_NAME);
        
        foreach (var sourcePath in sourcePathList)
        {
            var xmlTemplateCopy = new XDocument(xmlSourceTemplate);
            
            result.Add(DrumRackProcessor.ProcessInternal(sourcePath, targetPath, xmlTemplateCopy));
        }

        return result;
    }

    private static SamplesProcessResult ProcessInternal(string sourcePath, string targetPath, XDocument xmlTemplate)
    {
        IEnumerable<XmlSample> drumSampleList;

        var processingResult = new SamplesProcessResult(sourcePath);
        if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath) || xmlTemplate == null)
        {
            return processingResult.Set(SamplesProcessResult.ValueEnum.GenericError);
        }
        
        try
        {
            // Check if the ADG file is compressed (GZIP)
            if (Helpers.IsGZipFile(sourcePath))
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

            return processingResult.Set(SamplesProcessResult.ValueEnum.GenericError);
        }

        if ((drumSampleList?.Count() ?? 0) <= 0)
        {
            Console.WriteLine($"No drum samples found on {Path.GetFileName(sourcePath)}");
            
            return processingResult.Set(SamplesProcessResult.ValueEnum.SamplesNotFound);
        }

        var drumBranchPresetList = xmlTemplate.Descendants("DrumBranchPreset");
        foreach (var drumBranchPreset in drumBranchPresetList)
        {
            var presetId = drumBranchPreset.Attribute("Id")?.Value;
            if (presetId == null)
            {
                return processingResult.Set(SamplesProcessResult.ValueEnum.GenericError);
            }

            var drumSample = drumSampleList.FirstOrDefault(p => p.Id == presetId);
            if (drumSample == null)
            {
                // A drum branch might not have a drum sample set
                continue;
            }

            var userSample = drumBranchPreset.Descendants("UserSample").FirstOrDefault();
            if (string.IsNullOrEmpty(drumSample.ReceivingNote) || userSample == null)
            {
                // Trying to go ahead and hope for better luck with the next one
                continue;
            }

            // Injecting the data of the sample into the userSample element
            userSample.Element("Value")?.Add(drumSample.Body);

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

            return processingResult.Set(SamplesProcessResult.ValueEnum.GenericError);
        }

        return processingResult.Set(SamplesProcessResult.ValueEnum.Ok, drumSampleList.Select(_ => _.Path).ToList());
    }

    private static IEnumerable<XmlSample> ExtractDrumSamplesFromAdgStream(Stream xmlStream)
    {
        try
        {
            var doc = XDocument.Load(xmlStream);
            
            // Find all DrumBranchPreset elements in the stream
            var drumBranchPresetList = doc.Descendants("DrumBranchPreset");
            var drumSampleList = new List<XmlSample>();

            foreach (var drumBranchPreset in drumBranchPresetList)
            {
                // Get the current branch ID, otherwise a string empty
                var drumBranchId = drumBranchPreset.Attribute("Id")?.Value ?? string.Empty;

                // Search for a SampleRef element anywhere within the current branch
                var sampleRefElement = drumBranchPreset.Descendants("SampleRef").FirstOrDefault();

                // !!! Testing !!!
                // var t = drumBranchPreset.Descendants("SampleRef");
                
                // Look for FileRef under the SampleRef element
                if (sampleRefElement?.Element("FileRef") == null)
                {
                    continue;
                }
                
                var receivingNote = drumBranchPreset.Element("ZoneSettings")
                    ?.Element("ReceivingNote")
                    ?.Attribute("Value")?.Value;
                
                var path = sampleRefElement.Element("FileRef")
                    ?.Element("Path")
                    ?.Attribute("Value")?.Value;

                if (string.IsNullOrEmpty(receivingNote) || string.IsNullOrEmpty(path))
                {
                    continue;
                }

                // Add the default id for safe
                sampleRefElement.SetAttributeValue("Id", 0);
        
                // To remove and add a SourceContext empty
                sampleRefElement.Descendants("SourceContext").Remove();
                sampleRefElement.Add(new XElement("SourceContext"));

                drumSampleList.Add(new XmlSample(drumBranchId, sampleRefElement, receivingNote, path));
            }
            
            // Ordering based to notes
            return drumSampleList.OrderByDescending(_ => _.ReceivingNote).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error processing ADG XML: " + ex.Message);

            return new List<XmlSample>();
        }
    }
}