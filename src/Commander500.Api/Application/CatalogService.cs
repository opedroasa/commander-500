using Commander500.Api.Contracts;
using Commander500.Api.Data;
using Commander500.Api.Domain;
using Commander500.Api.Infrastructure.Scryfall;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Api.Application;

/// <summary>Players and decks CRUD. Commanders are not registered by hand: they are created from the deck form.</summary>
public class CatalogService(AppDbContext db, TimeProvider clock, IScryfallClient scryfall, ILogger<CatalogService> logger)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    // ---------- Players
    public async Task<IReadOnlyList<PlayerDto>> Players() =>
        await db.Players.AsNoTracking().OrderBy(p => p.Name)
            .Select(p => new PlayerDto(p.Id, p.Name, p.Nickname, p.IsActive, p.CreatedAt, p.Decks.Count))
            .ToListAsync();

    public async Task<PlayerDto> Player(int id) =>
        await db.Players.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new PlayerDto(p.Id, p.Name, p.Nickname, p.IsActive, p.CreatedAt, p.Decks.Count))
            .FirstOrDefaultAsync() ?? throw new NotFoundException($"Jogador {id} não encontrado.");

    public async Task<PlayerDto> CreatePlayer(PlayerUpsertRequest req)
    {
        var p = new Player { CreatedAt = Now };
        Apply(p, req);
        db.Players.Add(p);
        await db.SaveChangesAsync();
        return await Player(p.Id);
    }

    public async Task<PlayerDto> UpdatePlayer(int id, PlayerUpsertRequest req)
    {
        var p = await db.Players.FindAsync(id) ?? throw new NotFoundException($"Jogador {id} não encontrado.");
        Apply(p, req);
        await db.SaveChangesAsync();
        return await Player(id);
    }

    public async Task DeletePlayer(int id)
    {
        var p = await db.Players.FindAsync(id) ?? throw new NotFoundException($"Jogador {id} não encontrado.");
        if (await db.RoundEntries.AnyAsync(e => e.PlayerId == id))
            throw new DomainException("O jogador já participou de rodadas; desative-o em vez de excluir.");
        db.Players.Remove(p);
        await db.SaveChangesAsync();
    }

    private static void Apply(Player p, PlayerUpsertRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ValidationException("Nome do jogador é obrigatório.");
        p.Name = req.Name.Trim();
        p.Nickname = string.IsNullOrWhiteSpace(req.Nickname) ? null : req.Nickname.Trim();
        p.IsActive = req.IsActive;
    }

    // ---------- Commanders

    /// <summary>Commander picker: searches Scryfall (commanders and backgrounds).</summary>
    public async Task<IReadOnlyList<ScryfallCardDto>> SearchCommanders(string? query)
    {
        try
        {
            return (await scryfall.SearchCommanders(query ?? "")).Select(c => c.ToDto()).ToList();
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("Não foi possível consultar o Scryfall agora. Digite o nome do comandante manualmente.");
        }
    }

    /// <summary>
    /// Finds or creates the deck's commanders (not saved yet). With a Scryfall id the card data is fetched and cached;
    /// with a name only, an existing commander is matched by normalized name or a new one is created (the
    /// enrichment job fills its art later).
    /// </summary>
    public async Task<List<Commander>> ResolveCommanders(IReadOnlyList<CommanderInput> inputs)
    {
        var picked = inputs.Where(i => i.ScryfallId is not null || !string.IsNullOrWhiteSpace(i.Name)).ToList();
        if (picked.Count is < 1 or > 2) throw new ValidationException("O deck deve ter 1 ou 2 comandantes.");

        var result = new List<Commander>();
        foreach (var input in picked)
        {
            Commander? commander = null;
            ScryfallCard? card = null;

            if (input.ScryfallId is { } sid)
            {
                commander = await db.Commanders.FirstOrDefaultAsync(c => c.ScryfallId == sid)
                            ?? db.Commanders.Local.FirstOrDefault(c => c.ScryfallId == sid);
                if (commander is null)
                {
                    try { card = await scryfall.GetById(sid); }
                    catch (HttpRequestException ex) { logger.LogInformation("Scryfall indisponível ao salvar deck: {Message}", ex.Message); }
                }
            }

            if (commander is null)
            {
                var name = CommanderName.Clean(card?.Name ?? input.Name);
                if (name.Length == 0) throw new ValidationException("Nome do comandante é obrigatório.");
                var key = CommanderName.Normalize(name);
                commander = await db.Commanders.FirstOrDefaultAsync(c => c.NormalizedName == key)
                            ?? db.Commanders.Local.FirstOrDefault(c => c.NormalizedName == key);
                if (commander is null)
                {
                    commander = new Commander { Name = name, NormalizedName = key };
                    db.Commanders.Add(commander);
                }
                if (card is not null && commander.ScryfallId is null) CommanderCards.Apply(commander, card);
            }

            if (result.Contains(commander)) throw new ValidationException("Comandantes repetidos.");
            result.Add(commander);
        }
        return result;
    }

    // ---------- Decks
    private IQueryable<Deck> DeckGraph() => db.Decks.Include(d => d.Player).Include(d => d.Commanders).ThenInclude(dc => dc.Commander);

    public async Task<IReadOnlyList<DeckDto>> Decks(int? playerId, DeckStatus? status)
    {
        var q = DeckGraph().AsNoTracking();
        if (playerId is { } pid) q = q.Where(d => d.PlayerId == pid);
        if (status is { } st) q = q.Where(d => d.Status == st);
        var decks = await q.OrderBy(d => d.Player!.Name).ThenBy(d => d.Name).ToListAsync();
        return decks.Select(d => d.ToDto()).ToList();
    }

    public async Task<DeckDto> Deck(int id) =>
        (await DeckGraph().AsNoTracking().FirstOrDefaultAsync(d => d.Id == id) ?? throw new NotFoundException($"Deck {id} não encontrado.")).ToDto();

    public async Task<DeckDto> CreateDeck(DeckUpsertRequest req)
    {
        var deck = new Deck { CreatedAt = Now, Status = DeckStatus.Pending };
        await Apply(deck, req);
        db.Decks.Add(deck);
        await db.SaveChangesAsync();
        return await Deck(deck.Id);
    }

    public async Task<DeckDto> UpdateDeck(int id, DeckUpsertRequest req)
    {
        var deck = await DeckGraph().FirstOrDefaultAsync(d => d.Id == id) ?? throw new NotFoundException($"Deck {id} não encontrado.");
        if (deck.PlayerId != req.PlayerId && await db.RoundEntries.AnyAsync(e => e.DeckId == id))
            throw new DomainException("O deck já foi usado em rodadas; não é possível trocar o dono.");
        await Apply(deck, req);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return await Deck(id);
    }

    public async Task<DeckDto> ReviewDeck(int id, DeckReviewRequest req)
    {
        var deck = await db.Decks.FindAsync(id) ?? throw new NotFoundException($"Deck {id} não encontrado.");
        if (req.Status == DeckStatus.Rejected && string.IsNullOrWhiteSpace(req.Note))
            throw new ValidationException("Informe uma observação ao reprovar o deck.");
        deck.Status = req.Status;
        deck.ReviewNote = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim();
        await db.SaveChangesAsync();
        return await Deck(id);
    }

    public async Task DeleteDeck(int id)
    {
        var deck = await db.Decks.FindAsync(id) ?? throw new NotFoundException($"Deck {id} não encontrado.");
        if (await db.RoundEntries.AnyAsync(e => e.DeckId == id))
            throw new DomainException("O deck já foi usado em rodadas e não pode ser excluído.");
        db.Decks.Remove(deck);
        await db.SaveChangesAsync();
    }

    private async Task Apply(Deck deck, DeckUpsertRequest req)
    {
        if (!await db.Players.AnyAsync(p => p.Id == req.PlayerId)) throw new ValidationException("Jogador não encontrado.");
        if (!string.IsNullOrWhiteSpace(req.ListUrl) &&
            (!Uri.TryCreate(req.ListUrl.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")))
            throw new ValidationException("Link da lista inválido (use http/https).");

        var commanders = await ResolveCommanders(req.Commanders ?? []);
        deck.PlayerId = req.PlayerId;
        // The name is optional: defaults to the commander(s). Statistics always use the commanders, not this name.
        deck.Name = string.IsNullOrWhiteSpace(req.Name)
            ? string.Join(" + ", commanders.Select(c => c.Name).Order())
            : req.Name.Trim();
        if (deck.Name.Length > 120) deck.Name = deck.Name[..120];
        deck.ListUrl = string.IsNullOrWhiteSpace(req.ListUrl) ? null : req.ListUrl.Trim();

        deck.Commanders.RemoveAll(dc => !commanders.Any(c => c.Id != 0 && c.Id == dc.CommanderId));
        foreach (var c in commanders.Where(c => c.Id == 0 || deck.Commanders.All(dc => dc.CommanderId != c.Id)))
            deck.Commanders.Add(new DeckCommander { Deck = deck, Commander = c });
    }
}
