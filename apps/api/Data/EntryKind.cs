using System.Text.Json.Serialization;

namespace SwarmApp.Api.Data;

/// <summary>
/// What was recorded. Serialised as a name so a client sends "Water", not a number that
/// silently changes meaning if the enum is reordered.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<EntryKind>))]
public enum EntryKind
{
    Water,
    Urination,
    Stool,
    NappyCheck,
    Food,
}
