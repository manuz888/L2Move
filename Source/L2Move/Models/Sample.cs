namespace L2Move.Models;

public class Sample
{
    public Sample(string id, string receivingNote, string path)
    {
        this.Id = id;
        this.ReceivingNote = receivingNote;
        this.Path = path;
    }

    public string Id { get; }
        
    public string Path { get; }
        
    public string ReceivingNote { get; }
}