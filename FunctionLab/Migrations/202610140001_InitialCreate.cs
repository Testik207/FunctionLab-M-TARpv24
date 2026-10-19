using FunctionLab.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace FunctionLab.Migrations;
[DbContext(typeof(UurimiseKontekst))]
[Migration("202610140001_InitialCreate")]
public class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name:"FunktsiooniUurimised",columns:table=>new {
            Id=table.Column<int>(type:"INTEGER",nullable:false).Annotation("Sqlite:Autoincrement",true),
            Valem=table.Column<string>(type:"TEXT",maxLength:400,nullable:false),
            Latex=table.Column<string>(type:"TEXT",maxLength:1000,nullable:false),
            Maaramispiirkond=table.Column<string>(type:"TEXT",nullable:false),
            Nullkohad=table.Column<string>(type:"TEXT",nullable:false),
            Tuletis=table.Column<string>(type:"TEXT",nullable:false),
            KriitilisedPunktid=table.Column<string>(type:"TEXT",nullable:false),
            Ekstreemumid=table.Column<string>(type:"TEXT",nullable:false),
            LahendusJson=table.Column<string>(type:"TEXT",nullable:false),
            VahemikAlgus=table.Column<double>(type:"REAL",nullable:false),
            VahemikLopp=table.Column<double>(type:"REAL",nullable:false),
            LuodudAeg=table.Column<DateTime>(type:"TEXT",nullable:false),
            MuudetudAeg=table.Column<DateTime>(type:"TEXT",nullable:true)
        },constraints:table=>table.PrimaryKey("PK_FunktsiooniUurimised",x=>x.Id));
        migrationBuilder.CreateIndex(name:"IX_FunktsiooniUurimised_LuodudAeg",table:"FunktsiooniUurimised",column:"LuodudAeg");
    }
    protected override void Down(MigrationBuilder migrationBuilder)=>migrationBuilder.DropTable("FunktsiooniUurimised");
    protected override void BuildTargetModel(ModelBuilder modelBuilder)=>InitialSchema.Build(modelBuilder);
}
