using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Cadastro de jogadores.</summary>
public class ServicoJogador(IContextoBanco banco, TimeProvider relogio)
{
    public async Task<IReadOnlyList<JogadorResposta>> Listar() =>
        await banco.Jogadores.AsNoTracking().OrderBy(j => j.Nome)
            .Select(j => new JogadorResposta(j.Id, j.Nome, j.Apelido, j.Ativo, j.DataCadastro, j.Decks.Count))
            .ToListAsync();

    public async Task<JogadorResposta> Obter(int id) =>
        await banco.Jogadores.AsNoTracking().Where(j => j.Id == id)
            .Select(j => new JogadorResposta(j.Id, j.Nome, j.Apelido, j.Ativo, j.DataCadastro, j.Decks.Count))
            .FirstOrDefaultAsync() ?? throw new NaoEncontradoException($"Jogador {id} não encontrado.");

    public async Task<JogadorResposta> Criar(JogadorEnvio envio)
    {
        var jogador = new Jogador { DataCadastro = relogio.GetUtcNow().UtcDateTime };
        PreencherDados(jogador, envio);
        banco.Jogadores.Add(jogador);
        await banco.SalvarAsync();
        return await Obter(jogador.Id);
    }

    public async Task<JogadorResposta> Alterar(int id, JogadorEnvio envio)
    {
        var jogador = await banco.Jogadores.FindAsync(id) ?? throw new NaoEncontradoException($"Jogador {id} não encontrado.");
        PreencherDados(jogador, envio);
        await banco.SalvarAsync();
        return await Obter(id);
    }

    /// <summary>Exclui (com os decks). Quem já jogou alguma rodada não pode ser excluído: deve ser desativado.</summary>
    public async Task Excluir(int id)
    {
        var jogador = await banco.Jogadores.FindAsync(id) ?? throw new NaoEncontradoException($"Jogador {id} não encontrado.");
        if (await banco.Participacoes.AnyAsync(p => p.IdJogador == id))
            throw new RegraNegocioException("O jogador já participou de rodadas; desative-o em vez de excluir.");
        banco.Jogadores.Remove(jogador);
        await banco.SalvarAsync();
    }

    private static void PreencherDados(Jogador jogador, JogadorEnvio envio)
    {
        if (string.IsNullOrWhiteSpace(envio.Nome)) throw new DadosInvalidosException("Nome do jogador é obrigatório.");
        jogador.Nome = envio.Nome.Trim();
        jogador.Apelido = string.IsNullOrWhiteSpace(envio.Apelido) ? null : envio.Apelido.Trim();
        jogador.Ativo = envio.Ativo;
    }
}
