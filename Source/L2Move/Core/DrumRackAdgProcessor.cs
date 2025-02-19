using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO.Compression;
using System.Collections.Generic;

using L2Move.Models;
using L2Move.Helpers;

namespace L2Move.Core;

public static class DrumRackAdgProcessor
{
    // TODO: insert all strings as a constants
    
    private static readonly string DEFAULT_TEMPLATE_FILE_NAME = Path.Combine(AppContext.BaseDirectory, "Resources", "template.xml");
    
    public static IEnumerable<ProcessResult> Process(List<string> sourcePathList, string targetPath)
    {
        var result = new List<ProcessResult>();
        
        var xmlSourceTemplate = XDocument.Load(DEFAULT_TEMPLATE_FILE_NAME);
        foreach (var sourcePath in sourcePathList)
        {
            result.Add(DrumRackAdgProcessor.ProcessInternal(sourcePath, targetPath, xmlSourceTemplate));
        }

        return result;
    }

    private static ProcessResult ProcessInternal(string sourcePath, string targetPath, XDocument xmlSourceTemplate)
    {
        var processResult = new ProcessResult(sourcePath);
        
        if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(targetPath) || xmlSourceTemplate == null)
        {
            return processResult.Set(ProcessResult.ValueEnum.GenericError);
        }
        
        List<XmlSample> drumSampleList;

        try
        {
            // Check if the ADG file is compressed (GZIP)
            if (FileHelper.IsGZipFile(sourcePath))
            {
                Console.WriteLine("The file is compressed. Extracting in memory...");

                // Open the ADG file as a FileStream and process it as a ZIP archive in memory
                using var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
                using var gZipStream = new GZipStream(fs, CompressionMode.Decompress);

                drumSampleList = DrumRackAdgProcessor.ExtractDrumSamplesFromXml(gZipStream).ToList();
            }
            else
            {
                // If not compressed, assume the file is an XML file
                Console.WriteLine("The file is not compressed. Processing as XML...");

                using var xmlFileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
                drumSampleList = DrumRackAdgProcessor.ExtractDrumSamplesFromXml(xmlFileStream).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);

            return processResult.Set(ProcessResult.ValueEnum.GenericError);
        }

        if ((drumSampleList?.Count() ?? 0) <= 0)
        {
            Console.WriteLine($"No drum samples found on {Path.GetFileName(sourcePath)}");
            
            return processResult.Set(ProcessResult.ValueEnum.SamplesNotFound);
        }
        
        // Distinct samples by their note
        var distinctSampleList = new List<IEnumerable<XmlSample>>();
        while (drumSampleList.Any())
        {
            var newKit = new HashSet<XmlSample>(drumSampleList.DistinctBy(_ => _.ReceivingNote));
            distinctSampleList.Add(newKit);
            
            drumSampleList.RemoveAll(_ => newKit.Contains(_));
        }

        string fileName;
        
        // Single samples file
        if (distinctSampleList.Count() <= 1)
        {
            fileName = Path.GetFileName(sourcePath);

            var result = DrumRackAdgProcessor.WriteAdg(xmlSourceTemplate, distinctSampleList[0], targetPath, fileName);
            
            return result
                ? new SamplesProcessResult(processResult).Set(ProcessResult.ValueEnum.Ok, drumSampleList) 
                : processResult.Set(ProcessResult.ValueEnum.GenericError);
        }
        
        // Multi samples file
        fileName = Path.GetFileNameWithoutExtension(sourcePath);
        
        var count = 1;
        var success = false;
        var multiSampleList = new Dictionary<string, IEnumerable<Sample>>();       
        foreach (var kit in distinctSampleList)
        {
            // At least four samples to create a new kit
            if (kit.Count() < 4)
            {
                continue;
            }
            
            var fileNameKit = $"{fileName} - Kit {count}";
            
            success = DrumRackAdgProcessor.WriteAdg(xmlSourceTemplate, kit, targetPath, $"{fileNameKit}.adg");
            if (!success)
            {
                continue;
            }
            
            multiSampleList.Add(fileNameKit, kit);
            
            count++;
        }
        
        return success 
            ? new MultiSampleProcessResult(processResult).Set(ProcessResult.ValueEnum.Ok, multiSampleList)
            : processResult.Set(ProcessResult.ValueEnum.GenericError);
    }

    private static IEnumerable<XmlSample> ExtractDrumSamplesFromXml(Stream xmlStream)
    {
        try
        {
            // Find all DrumBranchPreset elements in the stream
            var drumBranchPresetList = XDocument.Load(xmlStream).Descendants("DrumBranchPreset");
            
            var drumSampleList = new List<XmlSample>();
            foreach (var drumBranchPreset in drumBranchPresetList)
            {
                // Get the current branch ID, otherwise a string empty
                var drumBranchId = drumBranchPreset.Attribute("Id")?.Value ?? string.Empty;
                
                // Get receiving note for the current branch
                var receivingNote = drumBranchPreset.Element("ZoneSettings")
                    ?.Element("ReceivingNote")
                    ?.Attribute("Value")?.Value;

                if (string.IsNullOrEmpty(receivingNote))
                {
                    continue;
                }
                
                // Search for a SampleRef elements anywhere within the current branch
                var sampleRefElementList = drumBranchPreset.Descendants("SampleRef");
                foreach (var sampleRefElement in sampleRefElementList)
                {
                    // Look for FileRef under the SampleRef element
                    if (sampleRefElement?.Element("FileRef") == null)
                    {
                        continue;
                    }
                    
                    // Get path for the current SampleRef element 
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
            }
            
            return drumSampleList;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error processing ADG XML: " + ex.Message);

            return new List<XmlSample>();
        }
    }

    private static bool WriteAdg(XDocument xmlSourceTemplate,
                                 IEnumerable<XmlSample> drumSampleList,
                                 string targetPath,
                                 string fileName)
    {
        // Copy the template to preserve the source one
        var xmlTemplate = new XDocument(xmlSourceTemplate);
        
        var drumBranchPresetList = xmlTemplate.Descendants("DrumBranchPreset");
        foreach (var drumBranchPreset in drumBranchPresetList)
        {
            var presetId = drumBranchPreset.Attribute("Id")?.Value;
            if (presetId == null)
            {
                return false;
            }

            var drumSample = drumSampleList.FirstOrDefault(p => p.Id == presetId);
            if (drumSample == null)
            {
                // A drum branch may not have a sample set
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

            targetPath = Path.Combine(targetPath, fileName);

            using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
            using var writer = XmlWriter.Create(gzipStream, settings);
            xmlTemplate.Save(writer);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);

            return false;
        }

        return true;
    }
}