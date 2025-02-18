using System.Collections.Generic;

namespace L2Move.Models;

public sealed class SamplesProcessResult : ProcessResult
{
    public SamplesProcessResult(string filePath)
        : base(filePath)
    { }
    
    public List<string> SamplePathList { get; set; }
    
    public SamplesProcessResult Set(ValueEnum value, List<string> samplePathList = null)
    {
        this.Value = value;
        this.SamplePathList = samplePathList;

        return this;
    }
}