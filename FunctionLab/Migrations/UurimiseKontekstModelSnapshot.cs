using FunctionLab.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace FunctionLab.Migrations;
[DbContext(typeof(UurimiseKontekst))]
public class UurimiseKontekstModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)=>InitialSchema.Build(modelBuilder);
}
// Immutable snapshot of the first migration; do not change when the live model changes.
internal static class InitialSchema
{
    internal static void Build(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion","10.0.0");
        modelBuilder.Entity("FunctionLab.Models.FunktsiooniUurimine",b=> {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            b.Property<string>("Valem").IsRequired().HasMaxLength(400).HasColumnType("TEXT");
            b.Property<string>("Latex").IsRequired().HasMaxLength(1000).HasColumnType("TEXT");
            foreach(var name in new[]{"Maaramispiirkond","Nullkohad","Tuletis","KriitilisedPunktid","Ekstreemumid","LahendusJson"}) b.Property<string>(name).IsRequired().HasColumnType("TEXT");
            b.Property<double>("VahemikAlgus").HasColumnType("REAL");
            b.Property<double>("VahemikLopp").HasColumnType("REAL");
            b.Property<DateTime>("LuodudAeg").HasColumnType("TEXT");
            b.Property<DateTime?>("MuudetudAeg").HasColumnType("TEXT");
            b.HasKey("Id");b.HasIndex("LuodudAeg");b.ToTable("FunktsiooniUurimised");
        });
    }
}
