using Commander500.Aplicacao.Comum;
using Commander500.Aplicacao.Interfaces;
using Commander500.Aplicacao.Modelos;
using Commander500.Dominio.Comum;
using Commander500.Dominio.Entidades;
using Commander500.Dominio.Enumeradores;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Aplicacao.Servicos;

/// <summary>Cadastro e revisão de decks.</summary>
public class ServicoDeck(IContextoBanco banco, ServicoComandante servicoComandante, TimeProvider relogio)
{
    private IQueryable<Deck> DecksCompletos() =>
        banco.Decks.Include(d => d.Jogador).Include(d => d.Comandantes).ThenInclude(dc => dc.Comandante);

    public async Task<IReadOnlyList<DeckResposta>> Listar(int? idJogador, SituacaoDeck? situacao)
    {
        var consulta = DecksCompletos().AsNoTracking();
        if (idJogador is { } id) consulta = consulta.Where(d => d.IdJogador == id);
        if (situacao is { } s) consulta = consulta.Where(d => d.Situacao == s);
        var decks = await consulta.OrderBy(d => d.Jogador!.Nome).ThenBy(d => d.Nome).ToListAsync();
        return decks.Select(d => d.ParaResposta()).ToList();
    }

    public async Task<DeckResposta> Obter(int id) =>
        (await DecksCompletos().AsNoTracking().FirstOrDefaultAsync(d => d.Id == id)
         ?? throw new NaoEncontradoException($"Deck {id} não encontrado.")).ParaResposta();

    /// <summary>Deck novo entra como Pendente até a revisão do lojista.</summary>
    public async Task<DeckResposta> Criar(DeckEnvio envio)
    {
        var deck = new Deck { DataCadastro = relogio.GetUtcNow().UtcDateTime, Situacao = SituacaoDeck.Pendente };
        await PreencherDados(deck, envio);
        banco.Decks.Add(deck);
        await banco.SalvarAsync();
        return await Obter(deck.Id);
    }

    public async Task<DeckResposta> Alterar(int id, DeckEnvio envio)
    {
        var deck = await DecksCompletos().FirstOrDefaultAsync(d => d.Id == id) ?? throw new NaoEncontradoException($"Deck {id} não encontrado.");
        if (deck.IdJogador != envio.IdJogador && await banco.Participacoes.AnyAsync(p => p.IdDeck == id))
            throw new RegraNegocioException("O deck já foi usado em rodadas; não é possível trocar o dono.");
        await PreencherDados(deck, envio);
        await banco.SalvarAsync();
        banco.LimparRastreamento();
        return await Obter(id);
    }

    /// <summary>Aprovar / reprovar (observação obrigatória) / voltar a pendente. Não bloqueia o uso do deck.</summary>
    public async Task<DeckResposta> Revisar(int id, RevisaoDeckEnvio envio)
    {
        var deck = await banco.Decks.FindAsync(id) ?? throw new NaoEncontradoException($"Deck {id} não encontrado.");
        if (envio.Situacao == SituacaoDeck.Reprovado && string.IsNullOrWhiteSpace(envio.Observacao))
            throw new DadosInvalidosException("Informe uma observação ao reprovar o deck.");
        deck.Situacao = envio.Situacao;
        deck.ObservacaoRevisao = string.IsNullOrWhiteSpace(envio.Observacao) ? null : envio.Observacao.Trim();
        await banco.SalvarAsync();
        return await Obter(id);
    }

    public async Task Excluir(int id)
    {
        var deck = await banco.Decks.FindAsync(id) ?? throw new NaoEncontradoException($"Deck {id} não encontrado.");
        if (await banco.Participacoes.AnyAsync(p => p.IdDeck == id))
            throw new RegraNegocioException("O deck já foi usado em rodadas e não pode ser excluído.");
        banco.Decks.Remove(deck);
        await banco.SalvarAsync();
    }

    private async Task PreencherDados(Deck deck, DeckEnvio envio)
    {
        if (!await banco.Jogadores.AnyAsync(j => j.Id == envio.IdJogador)) throw new DadosInvalidosException("Jogador não encontrado.");
        if (!string.IsNullOrWhiteSpace(envio.UrlLista) &&
            (!Uri.TryCreate(envio.UrlLista.Trim(), UriKind.Absolute, out var endereco) || (endereco.Scheme != "http" && endereco.Scheme != "https")))
            throw new DadosInvalidosException("Link da lista inválido (use http/https).");

        var comandantes = await servicoComandante.Resolver(envio.Comandantes ?? []);
        deck.IdJogador = envio.IdJogador;
        // O nome é opcional: o padrão é o nome do(s) comandante(s). As estatísticas usam sempre os comandantes.
        deck.Nome = string.IsNullOrWhiteSpace(envio.Nome)
            ? string.Join(" + ", comandantes.Select(c => c.Nome).Order())
            : envio.Nome.Trim();
        if (deck.Nome.Length > 120) deck.Nome = deck.Nome[..120];
        deck.UrlLista = string.IsNullOrWhiteSpace(envio.UrlLista) ? null : envio.UrlLista.Trim();

        deck.Comandantes.RemoveAll(dc => !comandantes.Any(c => c.Id != 0 && c.Id == dc.IdComandante));
        foreach (var comandante in comandantes.Where(c => c.Id == 0 || deck.Comandantes.All(dc => dc.IdComandante != c.Id)))
            deck.Comandantes.Add(new DeckComandante { Deck = deck, Comandante = comandante });
    }
}
