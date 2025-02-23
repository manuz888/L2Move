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
    
    public SamplesProcessResult Set(Value adgValue, IEnumerable<Sample> sampleList)
    {
        this.SampleList = sampleList;
        
        _ = base.Set(adgValue);

        return this;
    }
}