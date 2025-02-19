using System;

namespace L2Move.Models;

public class ProcessResult
{
    #region Enums
    
    public enum ValueEnum
    {
        Ok,
        GenericError,
        
        // Samples
        SamplesNotFound
    }

    #endregion
   
    public ProcessResult(string sourceFilePath)
    {
        this.SourceFilePath = sourceFilePath;
    }
    
    public string SourceFilePath { get; }
    
    public ValueEnum Value { get; protected set; }

    public string SourceFileName => System.IO.Path.GetFileName(this.SourceFilePath);
    
    public string ValueString => this.Value switch
    {
        ValueEnum.Ok => "Ok",
        ValueEnum.GenericError => "Generic error",
        
        // Samples
        ValueEnum.SamplesNotFound => "Samples not Found",
        
        // Fallback
        _ => throw new ArgumentOutOfRangeException()
    };
    
    public ProcessResult Set(ValueEnum value)
    {
        this.Value = value;

        return this;
    }
}