using System.Collections.Generic;

public class Herd
{
    public int                       Id         { get; set; }
    public EntityId                  Species    { get; set; }
    public int                       Count      { get; set; } = 0;
    public int                       SizeCap    { get; set; }
    public int                       Generation { get; set; } = 0;
    public List<int>                 FounderIds { get; set; } = [];
    public Dictionary<string, float> Genes      { get; set; } = [];
}
