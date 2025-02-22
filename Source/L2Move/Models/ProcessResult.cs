using System.Collections.Generic;

namespace L2Move.Models;

public class ProcessResult
{
    #region Enums
    
    public enum Value
    {
        Ok,
        GenericError,
        
        // Samples
        SamplesNotFound
    }

    #endregion
    
    private static readonly Dictionary<Value, string> _valueStringMap = new()
    {
        { Value.Ok, "Ok" },
        { Value.GenericError, "Generic error" },
        
        // Samples
        { Value.SamplesNotFound, "Samples not found" }
    };
   
    public ProcessResult(string sourceFilePath)
    {
        this.SourceFilePath = sourceFilePath;
    }
    
    public string SourceFilePath { get; }
    
    public Value AdgValue { get; private set; }

    public Value? PresetValue { get; protected set; }

    public string SourceFileName => System.IO.Path.GetFileName(this.SourceFilePath);
    
    public string AdgValueString => _valueStringMap.ContainsKey(this.AdgValue) ? _valueStringMap[this.AdgValue] : string.Empty;

    public string PresetValueString => (this.PresetValue.HasValue && _valueStringMap.ContainsKey(this.PresetValue.Value)) 
        ? _valueStringMap[this.PresetValue.Value] 
        : string.Empty;
    
    public ProcessResult Set(Value adgValue, Value? presetValue = null)
    {
        this.AdgValue = adgValue;
        this.PresetValue = presetValue;
        
        return this;
    }
}