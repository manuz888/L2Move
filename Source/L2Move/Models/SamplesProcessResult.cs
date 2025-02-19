using System.Collections.Generic;

namespace L2Move.Models;

public sealed class SamplesProcessResult : ProcessResult
{
    public SamplesProcessResult(string sourceFilePath)
        : base(sourceFilePath)
    { }

    public SamplesProcessResult(ProcessResult result)
        : base(result.SourceFilePath)
    { }
    
    public IEnumerable<Sample> SampleList { get; set; }
    
    public SamplesProcessResult Set(ValueEnum value, IEnumerable<Sample> sampleList)
    {
        this.Value = value;
        this.SampleList = sampleList;

        return this;
    }
}