using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using JobLens.Domain.Abstractions;
using JobLens.Domain.Models;

namespace JobLens.Infrastructure.Profiles;

/// <summary>
/// IUserProfileStore backed by a single JSON file on disk. Ships with
/// profile.default.json (Sebastián's real CV data) so the app works
/// end-to-end out of the box — see Profiles.notas.md for why edits
/// overwrite that same file instead of a separate user-data copy.
/// </summary>
public sealed class JsonFileUserProfileStore : IUserProfileStore
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonFileUserProfileStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<UserProfile> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(_filePath);
        var profile = await JsonSerializer.DeserializeAsync<UserProfile>(stream, SerializerOptions, cancellationToken);

        return profile ?? throw new InvalidOperationException($"Profile file at {_filePath} deserialized to null.");
    }

    public async Task SaveAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, profile, SerializerOptions, cancellationToken);
    }
}
