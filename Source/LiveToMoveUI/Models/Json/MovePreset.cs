using System.Collections.Generic;

using Newtonsoft.Json;

namespace LiveToMoveUI.Models.Json;

public sealed class MovePreset
{
    #region Nested classes
    
    public class Params
    {
        [JsonProperty("Enabled")]
        public bool? Enabled { get; set; }
        
        [JsonProperty("Macro0")]
        public double? Macro0 { get; set; }
        
        [JsonProperty("Macro1")]
        public double? Macro1 { get; set; }
        
        [JsonProperty("Macro2")]
        public double? Macro2 { get; set; }
        
        [JsonProperty("Macro3")]
        public double? Macro3 { get; set; }
        
        [JsonProperty("Macro4")]
        public double? Macro4 { get; set; }
        
        [JsonProperty("Macro5")]
        public double? Macro5 { get; set; }
        
        [JsonProperty("Macro6")]
        public double? Macro6 { get; set; }
        
        [JsonProperty("Macro7")]
        public double? Macro7 { get; set; }

        [JsonProperty("Voice_Envelope_Hold")]
        public double? VoiceEnvelopeHold { get; set; }
        
        public static Params Default() => new Params()
        {
            Enabled = true,

            Macro0 = 0.0d, Macro1 = 0.0d, 
            Macro2 = 0.0d, Macro3 = 0.0d,
            Macro4 = 0.0d, Macro5 = 0.0d,
            Macro6 = 0.0d, Macro7 = 0.0d
        };
    }

    public class Chain
    {
        public string Name { get; set; } = string.Empty;

        public int Color { get; set; } = 0;
        
        public List<Device> Devices { get; set; }
        
        public Mixer Mixer { get; set; }
        
        public DrumZoneSettings DrumZoneSettings { get; set; }
        
        public List<Chain> Chains { get; set; }
        
        public List<Chain> ReturnChains { get; set; }
        
        public static Chain ReverbReturnChain() => new Chain
        {
            Devices = [Device.Reverb()],
            Mixer = Mixer.Default(isEnabled: false)
        };
    }

    public class Device
    {
        public string PresetUri { get; set; }
        
        public string Kind { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public Params Parameters { get; set; }
        
        public List<Chain> Chains { get; set; }
        
        public List<Chain> ReturnChains { get; set; }

        public DeviceData? DeviceData { get; set; } = null;

        public static Device DrumRack(List<Chain> drumCells) => new Device()
        {
            Kind = "drumRack",
            Parameters = Params.Default(),
            Chains = drumCells,
            ReturnChains = [MovePreset.Chain.ReverbReturnChain()]
        };
        
        public static Device DrumCell(string samplePath) => new Device()
        {
            Kind = "drumCell",
            DeviceData = new MovePreset.DeviceData
            {
                SampleUri = samplePath
            },
            Parameters = new Params { VoiceEnvelopeHold = 60.0 }
        };
        
        public static Device Reverb() => new Device
        {
            Kind = "reverb",
            Name = "Reverb",
            Parameters = new Params(),
            DeviceData = new DeviceData()
        };
        
        public static Device Saturator() => new Device
        {
            Kind = "saturator",
            Name = "Saturator",
            Parameters = new Params(),
            DeviceData = new DeviceData()
        };
    }

    public class DeviceData
    {
        [JsonProperty("sampleUri")]
        public string SampleUri { get; set; }
    }

    public class Mixer
    {
        public double Pan { get; set; } = 0.0;
        
        [JsonProperty("solo-cue")]
        public bool SoloCue { get; set; } = false;
        
        public bool SpeakerOn { get; set; } = true;
        
        public double Volume { get; set; } = 0.0;
        
        public List<Send> Sends { get; set; } = new List<Send>();

        public static Mixer Default(bool isEnabled = true) => new Mixer
        {
            Sends = new List<Send> { new Send { IsEnabled = isEnabled, Amount = -70.0 } }
        };
    }

    public class Send
    {
        public bool IsEnabled { get; set; }
        public double Amount { get; set; }
    }

    public class DrumZoneSettings
    {
        public int ReceivingNote { get; set; }
        
        public int SendingNote { get; set; }
        
        public int ChokeGroup { get; set; }
    }
    
    #endregion

    [JsonProperty("$schema")]
    public string Schema { get; set; } = "http://tech.ableton.com/schema/song/1.4.4/devicePreset.json";

    public string Kind { get; set; } = "instrumentRack";
    
    public string Name { get; set; }

    public Params Parameters => Params.Default();
    
    public List<Chain> Chains { get; set; }
}