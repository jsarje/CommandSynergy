namespace CommandSynergy.Domain.Cards;

/// <summary>
/// Represents a single card face for multi-face cards.
/// </summary>
public sealed record CardFaceProfile(
    string FaceId,
    string Name,
    string? ManaCost,
    string TypeLine,
    string? OracleText,
    string? ImageUri,
    bool IsPrimaryFace);