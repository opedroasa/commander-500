using System.Security.Cryptography;

namespace Commander500.Dominio.Regras;

/// <summary>Fonte de números aleatórios do sorteio. Trocável para os testes terem resultado previsível.</summary>
public interface IGeradorAleatorio
{
    /// <summary>Número inteiro aleatório entre 0 e <paramref name="maximoExclusivo"/> - 1.</summary>
    int Proximo(int maximoExclusivo);
}

/// <summary>Usado em produção (spec 4.3: sorteio com gerador criptográfico).</summary>
public sealed class GeradorAleatorioCriptografico : IGeradorAleatorio
{
    public int Proximo(int maximoExclusivo) => RandomNumberGenerator.GetInt32(maximoExclusivo);
}

/// <summary>Usado nos testes e no seed de demonstração: a mesma semente gera sempre o mesmo sorteio.</summary>
public sealed class GeradorAleatorioComSemente(int semente) : IGeradorAleatorio
{
    private readonly Random _aleatorio = new(semente);
    public int Proximo(int maximoExclusivo) => _aleatorio.Next(maximoExclusivo);
}
