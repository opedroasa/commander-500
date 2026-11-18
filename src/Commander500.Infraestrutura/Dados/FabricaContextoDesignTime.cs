using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Commander500.Infraestrutura.Dados;

/// <summary>Usada pelo `dotnet ef` para gerar migrations sem precisar rodar a Api nem ter o banco no ar.</summary>
public class FabricaContextoDesignTime : IDesignTimeDbContextFactory<ContextoBanco>
{
    public ContextoBanco CreateDbContext(string[] argumentos) =>
        new(new DbContextOptionsBuilder<ContextoBanco>()
            .UseNpgsql("Host=localhost;Database=commander500;Username=commander500;Password=commander500")
            .Options);
}
