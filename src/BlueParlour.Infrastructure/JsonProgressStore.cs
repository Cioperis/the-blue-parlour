using System.Text.Json;
using BlueParlour.Application;

namespace BlueParlour.Infrastructure;

public sealed class JsonProgressStore(string path) : IProgressStore
{
    public Progress Load()
    {
        try
        {
            if (!File.Exists(path)) return new();
            var progress = JsonSerializer.Deserialize<Progress>(File.ReadAllText(path));
            return progress is { CompletedSessions: >= 0 and <= 1_000_000 } ? progress : new();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return new();
        }
    }

    public void Save(Progress progress)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(progress));
        File.Move(temporary, path, overwrite: true);
    }
}
