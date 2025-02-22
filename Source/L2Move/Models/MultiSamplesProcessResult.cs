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
    
    public MultiSamplesProcessResult Set(Value adgValue,
                                         Dictionary<string, IEnumerable<Sample>> multiSampleList,
                                         Value? presetValue = null)
    {
        this.MultiSampleList = multiSampleList;

        _ = base.Set(adgValue, presetValue);

        return this;
    }
}
