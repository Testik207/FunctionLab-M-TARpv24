using FunctionLab.Models;
using Microsoft.EntityFrameworkCore;
namespace FunctionLab.Data;
public class UurimiseKontekst(DbContextOptions<UurimiseKontekst> options) : DbContext(options)
{
    public DbSet<FunktsiooniUurimine> FunktsiooniUurimised => Set<FunktsiooniUurimine>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        var e = builder.Entity<FunktsiooniUurimine>();
        e.ToTable("FunktsiooniUurimised");
        e.HasIndex(x => x.LuodudAeg);
        e.Property(x => x.Valem).IsRequired().HasMaxLength(400);
        e.Property(x => x.Latex).HasMaxLength(1000);
    }
}
