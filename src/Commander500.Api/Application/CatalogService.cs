using Commander500.Api.Contracts;
using Commander500.Api.Data;
using Commander500.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Commander500.Api.Application;

/// <summary>Players, decks and commanders CRUD.</summary>
public class CatalogService(AppDbContext db, TimeProvider clock)
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
    public async Task<IReadOnlyList<CommanderDto>> Commanders(string? search)
    {
        var q = db.Commanders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var key = CommanderName.Normalize(search);
            q = q.Where(c => c.NormalizedName.Contains(key));
        }
        return await q.OrderBy(c => c.Name).Select(c => new CommanderDto(c.Id, c.Name)).ToListAsync();
    }

    public async Task<CommanderDto> CreateCommander(CommanderUpsertRequest req)
    {
        var c = (await ResolveCommanders([req.Name])).Single();
        await db.SaveChangesAsync();
        return c.ToDto();
    }

    /// <summary>Renames (fixes spelling). Fails if another commander already has the same normalized name.</summary>
    public async Task<CommanderDto> UpdateCommander(int id, CommanderUpsertRequest req)
    {
        var c = await db.Commanders.FindAsync(id) ?? throw new NotFoundException($"Comandante {id} não encontrado.");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ValidationException("Nome do comandante é obrigatório.");
        var key = CommanderName.Normalize(req.Name);
        if (await db.Commanders.AnyAsync(x => x.Id != id && x.NormalizedName == key))
            throw new DomainException("Já existe um comandante com esse nome.");
        c.Name = CommanderName.Clean(req.Name);
        c.NormalizedName = key;
        await db.SaveChangesAsync();
        return c.ToDto();
    }

    public async Task DeleteCommander(int id)
    {
        var c = await db.Commanders.FindAsync(id) ?? throw new NotFoundException($"Comandante {id} não encontrado.");
        if (await db.DeckCommanders.AnyAsync(dc => dc.CommanderId == id))
            throw new DomainException("Comandante em uso por algum deck.");
        db.Commanders.Remove(c);
        await db.SaveChangesAsync();
    }

    /// <summary>Finds commanders by normalized name, creating missing ones (not yet saved).</summary>
    public async Task<List<Commander>> ResolveCommanders(IReadOnlyList<string> names)
    {
        var cleaned = names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(CommanderName.Clean).ToList();
        if (cleaned.Count is < 1 or > 2) throw new ValidationException("O deck deve ter 1 ou 2 comandantes.");
        var keys = cleaned.Select(CommanderName.Normalize).ToList();
        if (keys.Distinct().Count() != keys.Count) throw new ValidationException("Comandantes repetidos.");

        var existing = await db.Commanders.Where(c => keys.Contains(c.NormalizedName)).ToDictionaryAsync(c => c.NormalizedName);
        var result = new List<Commander>();
        for (var i = 0; i < keys.Count; i++)
        {
            if (!existing.TryGetValue(keys[i], out var c))
            {
                c = new Commander { Name = cleaned[i], NormalizedName = keys[i] };
                db.Commanders.Add(c);
            }
            result.Add(c);
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
        if (string.IsNullOrWhiteSpace(req.Name)) throw new ValidationException("Nome do deck é obrigatório.");
        if (!await db.Players.AnyAsync(p => p.Id == req.PlayerId)) throw new ValidationException("Jogador não encontrado.");
        if (!string.IsNullOrWhiteSpace(req.ListUrl) &&
            (!Uri.TryCreate(req.ListUrl.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https")))
            throw new ValidationException("Link da lista inválido (use http/https).");

        var commanders = await ResolveCommanders(req.Commanders ?? []);
        deck.PlayerId = req.PlayerId;
        deck.Name = req.Name.Trim();
        deck.ListUrl = string.IsNullOrWhiteSpace(req.ListUrl) ? null : req.ListUrl.Trim();

        deck.Commanders.RemoveAll(dc => commanders.All(c => c.Id == 0 || c.Id != dc.CommanderId));
        foreach (var c in commanders.Where(c => c.Id == 0 || deck.Commanders.All(dc => dc.CommanderId != c.Id)))
            deck.Commanders.Add(new DeckCommander { Deck = deck, Commander = c });
    }
}
