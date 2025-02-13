using System;
using System.Collections.Generic;

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
    
    public ProcessingResult(string filePath)
    {
        this.FilePath = filePath;
    }
    
    public string FilePath { get; }
    
    public List<string> SamplePathList { get; set; }
    
    public ValueEnum Value { get; set; }

    public string FileName => System.IO.Path.GetFileName(this.FilePath);
    
    public string ValueString => this.Value switch
    {
        ValueEnum.Ok => "Ok",
        ValueEnum.GenericError => "Generic error",
        ValueEnum.SamplesNotFound => "Samples not Found",
        
        _ => throw new ArgumentOutOfRangeException()
    };
    
    public ProcessingResult Set(ValueEnum value, List<string> samplePathList = null)
    {
        this.Value = value;
        this.SamplePathList = samplePathList;

        return this;
    }
}