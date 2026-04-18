namespace CommandSynergy.Domain.Cards;

/// <summary>
/// Describes the basis under which a card is eligible to serve as a commander.
/// </summary>
/// <remarks>
/// Only the official Commander rules define eligibility. Legendary creatures are eligible by
/// default. Cards whose oracle text explicitly states they may be your commander are eligible
/// via oracle-text exception. All other cards are not eligible.
/// </remarks>
public enum CommanderEligibilityBasis
{
    /// <summary>Eligibility has not yet been determined for this card.</summary>
    Unknown,

    /// <summary>Eligible because the card is a legendary creature.</summary>
    LegendaryCreature,

    /// <summary>
    /// Eligible because its oracle text contains an explicit "can be your commander"
    /// exception (e.g. Prismatic Piper, Dungeon Master).
    /// </summary>
    OracleTextException,

    /// <summary>Not eligible to serve as a commander under official Commander rules.</summary>
    NotEligible,
}
