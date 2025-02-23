using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Collections.Generic;

using Newtonsoft.Json;

using L2Move.Models;
using L2Move.Helpers;
using L2Move.Core.Json;
using L2Move.Models.Json;

namespace L2Move.Core;

public static class MovePresetManager
{
    private const string PRESET_BUNDLE_EXTENSION = ".ablpresetbundle";
    private const string SAMPLES_DIRECTORY = "Samples";
    private const string PRESET_JSON_FILE_NAME = "Preset.ablpreset";
    
    private static readonly JsonSerializerSettings JSON_SETTINGS = new JsonSerializerSettings
    {
        ContractResolver = new CustomContractResolver(),
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };
    
    // ! The order on drum sample list will define the related pad 
    public static void GenerateDrumKit(string presetName,
                                       IEnumerable<string> drumSampleList,
                                       string targetPath,
                                       ProcessResult processResult)
    {
        if (string.IsNullOrWhiteSpace(presetName) ||
            string.IsNullOrWhiteSpace(targetPath) ||
            (drumSampleList?.Count() ?? 0) <= 0)
        {
            processResult.PresetValue = ProcessResult.Value.GenericError;
            
            return;
        }

        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }
        
        var presetDirectoryPath = Path.Combine(targetPath, presetName);
        var presetBundlePath =  Path.Combine(targetPath, presetName + PRESET_BUNDLE_EXTENSION);
        
        // If directory exists, append timestamp to create unique one
        if (File.Exists(presetBundlePath))
        {
            presetName = FileHelper.GenerateNewFileName(presetName);
            
            presetDirectoryPath = Path.Combine(targetPath, presetName);
            presetBundlePath =  Path.Combine(targetPath, presetName + PRESET_BUNDLE_EXTENSION);
        }
        
        try
        {
            Directory.CreateDirectory(presetDirectoryPath);

            var drumPreset = MovePresetManager.NewDrumRackPreset(presetName, drumSampleList);
            
            File.WriteAllText(Path.Combine(presetDirectoryPath, PRESET_JSON_FILE_NAME),
                              JsonConvert.SerializeObject(drumPreset, JSON_SETTINGS));

            var samplesDirectory = Path.Combine(presetDirectoryPath, "Samples");
            Directory.CreateDirectory(samplesDirectory);
            
            foreach (var drumSample in drumSampleList)
            {
                var destinationFile = Path.Combine(samplesDirectory, Path.GetFileName(drumSample));

                if (File.Exists(drumSample))
                {
                    File.Copy(drumSample, destinationFile, overwrite: true);
                }
                else
                {
                    var copied = false;
                    
                    // Try the way to combine path
                    if (!string.IsNullOrEmpty(processResult.SourceFilePath))
                    {
                        var fallbackDrumSample = FileHelper.CombineFromCommonPath(processResult.SourceFilePath, drumSample);
                        if (File.Exists(fallbackDrumSample))
                        {
                            File.Copy(fallbackDrumSample, destinationFile, overwrite: true);
                            
                            copied = true;
                        }
                    }

                    if (!copied)
                    {
                        throw new FileNotFoundException($"The file {drumSample} was not found.");
                    }
                }
            }

            ZipFile.CreateFromDirectory(presetDirectoryPath, presetBundlePath);
            Directory.Delete(presetDirectoryPath, recursive: true);
        }
        catch (Exception ex)
        {
            if (Directory.Exists(presetDirectoryPath))
            {
                Directory.Delete(presetDirectoryPath, recursive: true);
            }
            
            Console.WriteLine($"Error: {ex.Message}");

            processResult.PresetValue = ex is FileNotFoundException 
                ? ProcessResult.Value.ErrorOnCopySamples 
                : ProcessResult.Value.GenericError;

            return;
        }

        processResult.PresetValue = ProcessResult.Value.Ok;
    }

    private static MovePreset NewDrumRackPreset(string presetName, IEnumerable<string> drumSampleList)
    {
        var drumCells = new List<MovePreset.Chain>();

        var counter = 0;
        foreach (var drumSample in drumSampleList)
        {
            var encodedDrumSample = Uri.EscapeDataString(Path.GetFileName(drumSample));
            drumCells.Add(new MovePreset.Chain()
            {
                Devices = [MovePreset.Device.DrumCell(Path.Combine(SAMPLES_DIRECTORY, encodedDrumSample))],
                Mixer = MovePreset.Mixer.Default(),
                DrumZoneSettings = new MovePreset.DrumZoneSettings
                {
                    ReceivingNote = 36 + counter,
                    SendingNote = 60,
                    ChokeGroup = null  // TODO: to manage
                }
            });

            counter++;
        }

        var movePreset = new MovePreset()
        {
            Name = presetName,
            Chains =
            [
                new MovePreset.Chain
                {
                    Devices =
                    [
                        MovePreset.Device.DrumRack(drumCells),
                        MovePreset.Device.Saturator()
                    ],
                    Mixer = new MovePreset.Mixer()
                }
            ]
        };

        return movePreset;
    }
}