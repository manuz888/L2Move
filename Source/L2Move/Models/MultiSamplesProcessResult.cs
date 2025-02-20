using System.Collections.Generic;

namespace L2Move.Models;

public sealed class MultiSamplesProcessResult : ProcessResult
{
    public MultiSamplesProcessResult(string sourceFilePath)
        : base(sourceFilePath)
    { }

    public MultiSamplesProcessResult(ProcessResult result)
        : base(result.SourceFilePath)
    { }
    
    // <FileName, SampleList>
    public Dictionary<string, IEnumerable<Sample>> MultiSampleList { get; set; }
    
    public MultiSamplesProcessResult Set(ValueEnum value, Dictionary<string, IEnumerable<Sample>> multiSampleList)
    {
        this.Value = value;
        this.MultiSampleList = multiSampleList;

        return this;
    }
}
