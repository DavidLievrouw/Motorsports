using Vogen;

[assembly: VogenDefaults(
    underlyingType: typeof(int),
    conversions: Conversions.Default,
    fromPrimitiveCasting: CastOperator.Explicit,
    toPrimitiveCasting: CastOperator.Explicit,
    parsableForStrings: ParsableForStrings.GenerateMethodsAndInterface,
    parsableForPrimitives: ParsableForPrimitives.HoistMethodsAndInterfaces,
    primitiveEqualityGeneration: PrimitiveEqualityGeneration.Omit,
    isInitializedMethodGeneration: IsInitializedMethodGeneration.Generate,
    tryFromGeneration: TryFromGeneration.GenerateBoolAndErrorOrMethods,
    deserializationStrictness: DeserializationStrictness.AllowValidAndKnownInstances,
    debuggerAttributes: DebuggerAttributeGeneration.Basic
)]

namespace Motorsports.Common;

[ValueObject<int>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct ParticipantId;

[ValueObject<int>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct RoundId;

[ValueObject<int>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct RoundWinnerId;

[ValueObject<int>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct SeasonId;

[ValueObject<int>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct SeasonWinnerId;

[ValueObject<int>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct TeamId;

[ValueObject<string>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct CountryIso;

[ValueObject<string>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct SportName
{
    public static readonly SportName F1 = new("F1");
    public static readonly SportName FE = new("FE");
    public static readonly SportName IndyCar = new("IndyCar");
    public static readonly SportName MotoGP = new("MotoGP");
    public static readonly SportName NASCAR = new("NASCAR");
    public static readonly SportName WEC = new("WEC");
}

[ValueObject<string>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct StatusName
{
    public static readonly StatusName Scheduled = new("Scheduled");
    public static readonly StatusName ReadyToWatch = new("ReadyToWatch");
    public static readonly StatusName Postponed = new("Postponed");
    public static readonly StatusName Finished = new("Finished");
    public static readonly StatusName Stopped = new("Stopped");
    public static readonly StatusName Cancelled = new("Cancelled");
}

[ValueObject<string>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct UserId;

[ValueObject<string>(comparison: ComparisonGeneration.Omit, stringComparers: StringComparersGeneration.Generate)]
public readonly partial struct VenueName;
