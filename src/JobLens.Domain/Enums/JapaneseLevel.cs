namespace JobLens.Domain.Enums;

/// <summary>
/// Japanese proficiency on the JLPT scale (N5 = basic, N1 = near-native),
/// the scale Japanese job postings actually use.
/// </summary>
public enum JapaneseLevel
{
    NotSpecified,
    None,
    N5,
    N4,
    N3,
    N2,
    N1
}
