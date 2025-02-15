using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

using Newtonsoft.Json;

using LiveToMoveUI.Core.Json;
using LiveToMoveUI.Models.Json;

namespace LiveToMoveUI.Core;

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
    
    public static bool GenerateDrumRack(string presetName, List<string> drumSampleList, string targetPath)
    {
        if (string.IsNullOrWhiteSpace(presetName) ||
            string.IsNullOrWhiteSpace(targetPath) ||
            !Directory.Exists(targetPath)         ||
            (drumSampleList?.Count ?? 0) <= 0)
        {
            return false;
        }
        
        var presetDirectory = Path.Combine(targetPath, presetName);
        
        // If directory exists, append timestamp to create unique one
        if (Directory.Exists(presetDirectory))
        {
            presetName += "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            presetDirectory = Path.Combine(targetPath, presetName);
        }
        
        var samplesDirectory = Path.Combine(presetDirectory, "Samples"); // Subdirectory for samples
        
        try
        {
            Directory.CreateDirectory(presetDirectory);

            var drumPreset = MovePresetManager.NewDrumRackPreset(presetName, drumSampleList);
            
            File.WriteAllText(Path.Combine(presetDirectory, PRESET_JSON_FILE_NAME),
                              JsonConvert.SerializeObject(drumPreset, JSON_SETTINGS));

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
                    // Something goes wrong
                    return false;
                }
            }

            var presetBundleName = presetName + PRESET_BUNDLE_EXTENSION;
            if (File.Exists(presetBundleName))
            {
                File.Delete(presetBundleName);
            }
            
            ZipFile.CreateFromDirectory(presetDirectory, Path.Combine(targetPath, presetBundleName));
            Directory.Delete(presetDirectory, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");

            return false;
        }
        
        return true;
    }

    private static MovePreset NewDrumRackPreset(string presetName, List<string> drumSampleList)
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
                    ChokeGroup = 1
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