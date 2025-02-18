using System.Xml.Linq;

namespace L2Move.Models;

public sealed class XmlSample : Sample
{
    public XmlSample(string id, XElement body, string receivingNote, string path)
     : base(id, receivingNote, path)
    {
        this.Body = body;
    }
    
    public XElement Body { get; }
}