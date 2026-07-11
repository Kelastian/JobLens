using JobLens.Domain.Enums;
using JobLens.Domain.Models;
using JobLens.Infrastructure.Profiles;
using Xunit;

namespace JobLens.Infrastructure.Tests.Profiles;

public sealed class JsonFileUserProfileStoreTests : IDisposable
{
    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"joblens-profile-test-{Guid.NewGuid()}.json");

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    private static readonly UserProfile SampleProfile = new(
        FullName: "Sebastián Pizarro",
        TechStack: ["C#", ".NET", "React"],
        YearsOfExperience: 2,
        JapaneseLevel: JapaneseLevel.N4,
        EnglishLevel: EnglishLevel.C1,
        Location: "Greater Tokyo, Japan",
        Summary: "Computer engineer."
    );

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsEveryField()
    {
        var sut = new JsonFileUserProfileStore(_filePath);

        await sut.SaveAsync(SampleProfile);
        var loaded = await sut.LoadAsync();

        Assert.Equal(SampleProfile.FullName, loaded.FullName);
        Assert.Equal(SampleProfile.YearsOfExperience, loaded.YearsOfExperience);
        Assert.Equal(SampleProfile.JapaneseLevel, loaded.JapaneseLevel);
        Assert.Equal(SampleProfile.EnglishLevel, loaded.EnglishLevel);
        Assert.Equal(SampleProfile.Location, loaded.Location);
        Assert.Equal(SampleProfile.Summary, loaded.Summary);
        Assert.Equal(SampleProfile.TechStack, loaded.TechStack);
    }

    [Fact]
    public async Task SaveAsync_WritesAccentedCharactersLiterally_NotEscaped()
    {
        var sut = new JsonFileUserProfileStore(_filePath);

        await sut.SaveAsync(SampleProfile);
        var rawJson = await File.ReadAllTextAsync(_filePath);

        Assert.Contains("Sebastián", rawJson);
        Assert.DoesNotContain("\\u00e1", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ThrowsFileNotFoundException()
    {
        var sut = new JsonFileUserProfileStore(_filePath); // never saved to

        await Assert.ThrowsAsync<FileNotFoundException>(() => sut.LoadAsync());
    }
}
