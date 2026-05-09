using System.Text.Json.Serialization;

public partial class Map
{
    [JsonIgnore] public SpeciesManager _species { get; set; } = new();

    public void InitializeAllEntities() => _species.Initialize(this);
}
