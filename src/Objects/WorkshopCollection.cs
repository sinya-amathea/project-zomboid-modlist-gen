namespace Sinya.PZModsTool.Objects;

internal class WorkshopCollection
{
    public string Id { get; set; }

    public List<string> Mods { get; set; } = [];

    public List<string> WorkshopItems { get; set; } = [];
}