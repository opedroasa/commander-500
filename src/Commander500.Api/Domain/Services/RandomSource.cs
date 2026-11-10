using System.Security.Cryptography;

namespace Commander500.Api.Domain.Services;

public interface IRandomSource
{
    /// <summary>Uniform integer in [0, maxExclusive).</summary>
    int Next(int maxExclusive);
}

/// <summary>Production RNG (spec 4.3: cryptographic RNG).</summary>
public sealed class CryptoRandomSource : IRandomSource
{
    public int Next(int maxExclusive) => RandomNumberGenerator.GetInt32(maxExclusive);
}

/// <summary>Deterministic RNG for tests.</summary>
public sealed class SeededRandomSource(int seed) : IRandomSource
{
    private readonly Random _random = new(seed);
    public int Next(int maxExclusive) => _random.Next(maxExclusive);
}
