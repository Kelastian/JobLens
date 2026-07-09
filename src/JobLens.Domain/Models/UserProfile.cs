using JobLens.Domain.Enums;

namespace JobLens.Domain.Models;

/// <summary>
/// The candidate's profile used for match scoring. Shipped as an editable JSON
/// file (profile.default.json) so anyone can adapt it to themselves — this type
/// is just the shape, not Sebastián-specific data.
/// </summary>
public sealed record UserProfile(
    string FullName,
    IReadOnlyList<string> TechStack,
    int YearsOfExperience,
    JapaneseLevel JapaneseLevel,
    EnglishLevel EnglishLevel,
    string Location,
    string Summary
);
