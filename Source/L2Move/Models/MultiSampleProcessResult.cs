using System.Collections.Generic;

namespace L2Move.Models;

public sealed class MultiSampleProcessResult : ProcessResult
{
    public MultiSampleProcessResult(string sourceFilePath)
        : base(sourceFilePath)
    { }

    public MultiSampleProcessResult(ProcessResult result)
        : base(result.SourceFilePath)
    { }
    
    // <FileName, SampleList>
    public Dictionary<string, IEnumerable<Sample>> MultiSampleList { get; set; }
    
    public MultiSampleProcessResult Set(ValueEnum value, Dictionary<string, IEnumerable<Sample>> multiSampleList)
    {
        this.Value = value;
        this.MultiSampleList = multiSampleList;

        return this;
    }
}
