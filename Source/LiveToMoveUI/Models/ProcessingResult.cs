using System;

namespace LiveToMoveUI.Models;

public sealed class ProcessingResult
{
    #region Enums
    
    public enum ValueEnum
    {
        Ok,
        GenericError,
        SamplesNotFound
    }

    #endregion
   
    public ProcessingResult()
    { }
    
    public ProcessingResult(string path, ValueEnum value)
    {
        this.Path = path;
        this.Value = value;
    }
    
    public string Path { get; set; }
    
    public ValueEnum Value { get; set; }

    public string FileName => System.IO.Path.GetFileName(this.Path);
    
    public string ValueString => this.Value switch
    {
        ValueEnum.Ok => "Ok",
        ValueEnum.GenericError => "Generic error",
        ValueEnum.SamplesNotFound => "Samples not Found",
        
        _ => throw new ArgumentOutOfRangeException()
    };
    
    public ProcessingResult SetValue(ValueEnum value)
    {
        this.Value = value;

        return this;
    }
}