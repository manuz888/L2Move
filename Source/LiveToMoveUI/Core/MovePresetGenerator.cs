using System.Collections.Generic;

using LiveToMoveUI.Models.Json;

namespace LiveToMoveUI.Core;

public class MovePresetGenerator
{
    public static MovePreset GenerateDrumRack(string presetName, List<string> drumSampleList)
    {
        var drumCells = new List<MovePreset.Chain>();

        var counter = 0;
        foreach (var drumSample in drumSampleList)
        {
            drumCells.Add(new MovePreset.Chain()
            {
                Devices = [MovePreset.Device.DrumCell(drumSample)],
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