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
            return processResult.Set(ProcessResult.Value.GenericError);
        }

        var drumSampleList = DrumRackAdgProcessor.ExtractDrumSamplesFromFile(sourcePath).ToList();
        if ((drumSampleList?.Count() ?? 0) <= 0)
        {
            Console.WriteLine($"No drum samples found on {Path.GetFileName(sourcePath)}");
            
            return processResult.Set(ProcessResult.Value.SamplesNotFound);
        }
        
        // Create a kit by distinct samples based on their note
        var sampleKitList = new List<IEnumerable<XmlSample>>();
        while (drumSampleList.Any())
        {
            var newKit = new HashSet<XmlSample>(drumSampleList.DistinctBy(_ => _.ReceivingNote));
            sampleKitList.Add(newKit);
            
            drumSampleList.RemoveAll(_ => newKit.Contains(_));
        }

        string fileName;
        
        // Single samples file
        if (sampleKitList.Count() <= 1)
        {
            fileName = Path.GetFileName(sourcePath);

            var result = DrumRackAdgProcessor.WriteAdg(xmlSourceTemplate, sampleKitList[0], targetPath, fileName);
            
            return result
                ? new SamplesProcessResult(processResult).Set(ProcessResult.Value.Ok, sampleKitList[0]) 
                : processResult.Set(ProcessResult.Value.GenericError);
        }
        
        // Multi samples file
        fileName = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = Path.GetExtension(sourcePath);
        
        var count = 1;
        var success = false;
        var multiSamplesList = new Dictionary<string, IEnumerable<Sample>>();       
        foreach (var sampleKit in sampleKitList)
        {
            // At least four samples for a valid kit
            if (sampleKit.Count() < 4)
            {
                continue;
            }
            
            var fileNameSampleKit = $"{fileName} - Kit {count}{extension}";
            
            success = DrumRackAdgProcessor.WriteAdg(xmlSourceTemplate, sampleKit, targetPath, fileNameSampleKit);
            if (!success)
            {
                continue;
            }
            
            multiSamplesList.Add(fileNameSampleKit, sampleKit);
            
            count++;
        }
        
        return success 
            ? new MultiSamplesProcessResult(processResult).Set(ProcessResult.Value.Ok, multiSamplesList)
            : processResult.Set(ProcessResult.Value.GenericError);
    }

    private static IEnumerable<XmlSample> ExtractDrumSamplesFromFile(string sourcePath)
    {
        FileStream fileStream = null;
        Stream xmlStream = null;

        try
        {
            // Check if the file is compressed (GZIP)
            if (FileHelper.IsGZipFile(sourcePath))
            {
                Console.WriteLine("The file is compressed. Extracting in memory...");

                // Open the ADG file as a FileStream and process it as a ZIP archive in memory
                fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
                xmlStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }
            else
            {
                // If not compressed, assume the file is an XML file
                Console.WriteLine("The file is not compressed. Processing as XML...");

                xmlStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            }

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

                    // Trying to search hex path on data
                    if (string.IsNullOrEmpty(path))
                    {
                        var data = sampleRefElement.Element("FileRef")
                            ?.Element("Data")
                            ?.Value;

                        if (string.IsNullOrEmpty(data) || !FileHelper.HexToPath(data, out var fallBackPath))
                        {
                            continue;
                        }

                        fallBackPath = FileHelper.CombineFromCommonPath(sourcePath, fallBackPath);
                        if (!File.Exists(fallBackPath))
                        {
                            continue;
                        }

                        path = fallBackPath;
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
        finally
        {
            fileStream?.Dispose();
            xmlStream?.Dispose();
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

            var fullTargetPath = Path.Combine(targetPath, fileName);
            if (File.Exists(fullTargetPath))
            {
                fileName = FileHelper.GenerateNewFileName(fileName);
                
                fullTargetPath = Path.Combine(targetPath, fileName);
            }

            using var fileStream = new FileStream(fullTargetPath, FileMode.Create, FileAccess.Write);
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