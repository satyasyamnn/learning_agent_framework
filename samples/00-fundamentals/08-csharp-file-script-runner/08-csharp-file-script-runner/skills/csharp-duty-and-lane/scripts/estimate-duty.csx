decimal GetDecimalArg(string key)
{
    if (!Args.TryGetValue(key, out var raw) || raw is null)
    {
        throw new InvalidOperationException($"Missing required argument: {key}");
    }

    return raw switch
    {
        decimal d => d,
        double d => Convert.ToDecimal(d),
        float f => Convert.ToDecimal(f),
        int i => i,
        long l => l,
        string s when decimal.TryParse(s, out var parsed) => parsed,
        _ => throw new InvalidOperationException($"Argument '{key}' could not be converted to decimal. Raw value: {raw}")
    };
}

var declaredValueUsd = GetDecimalArg("declaredValueUsd");
var dutyRatePercent = GetDecimalArg("dutyRatePercent");

var estimatedDutyUsd = Math.Round(declaredValueUsd * (dutyRatePercent / 100m), 2, MidpointRounding.AwayFromZero);
var formalEntryLikely = declaredValueUsd >= 2500m;

return JsonSerializer.Serialize(new
{
    declaredValueUsd,
    dutyRatePercent,
    estimatedDutyUsd,
    formalEntryLikely
});
