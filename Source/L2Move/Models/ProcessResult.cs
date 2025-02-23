using System.Collections.Generic;

namespace L2Move.Models;

public class ProcessResult
{
    private const string PRESET_NULL_VALUE_TEXT = "Not processed";
    
    #region Enums
    
    public enum Value
    {
        Ok,
        GenericError,
        
        // Samples
        SamplesNotFound,
        ErrorOnCopySamples
    }

    #endregion
    
    private static readonly Dictionary<Value, string> _valueStringMap = new()
    {
        { Value.Ok, "Ok" },
        { Value.GenericError, "Generic error" },
        
        // Samples
        { Value.SamplesNotFound, "Samples not found" },
        { Value.ErrorOnCopySamples, "Error on copy samples" }
    };
   
    public ProcessResult(string sourceFilePath)
    {
        this.SourceFilePath = sourceFilePath;
    }
    
    public string SourceFilePath { get; }
    
    public Value AdgValue { get; private set; }

    public Value? PresetValue { get; set; }

    public string SourceFileName => System.IO.Path.GetFileName(this.SourceFilePath);
    
    public string AdgValueString => _valueStringMap.ContainsKey(this.AdgValue) ? _valueStringMap[this.AdgValue] : string.Empty;

    public string PresetValueString => (this.PresetValue.HasValue && _valueStringMap.ContainsKey(this.PresetValue.Value)) 
        ? _valueStringMap[this.PresetValue.Value] 
        : PRESET_NULL_VALUE_TEXT;
    
    public ProcessResult Set(Value adgValue)
    {
        this.AdgValue = adgValue;
        
        return this;
    }
}