namespace DispatcherServer.Entities;

public class LogEntity
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public string Author { get; set; }
    public string Division { get; set; }
    public string Type { get; set; }
    public string Text { get; set; }

    public LogEntity() {
        Id = Guid.NewGuid();
    }
}
