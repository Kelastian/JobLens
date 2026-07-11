using JobLens.Domain.Models;

namespace JobLens.Domain.Abstractions;

/// <summary>
/// Loads and saves the candidate's profile used for match scoring.
/// Application depends on this interface, not on the fact that the profile
/// happens to live in a JSON file on disk.
/// </summary>
public interface IUserProfileStore
{
    Task<UserProfile> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
