using System.ComponentModel.DataAnnotations;
namespace FunctionLab.Models;
public class FunktsiooniUurimine
{
    public int Id { get; set; }
    [Required, MaxLength(400)] public string Valem { get; set; } = string.Empty;
    [MaxLength(1000)] public string Latex { get; set; } = string.Empty;
    public string Maaramispiirkond { get; set; } = string.Empty;
    public string Nullkohad { get; set; } = string.Empty;
    public string Tuletis { get; set; } = string.Empty;
    public string KriitilisedPunktid { get; set; } = string.Empty;
    public string Ekstreemumid { get; set; } = string.Empty;
    public string LahendusJson { get; set; } = string.Empty;
    public double VahemikAlgus { get; set; }
    public double VahemikLopp { get; set; }
    public DateTime LuodudAeg { get; set; } = DateTime.UtcNow;
    public DateTime? MuudetudAeg { get; set; }
}
public record UurimiseSisend(string Valem, string? Latex, double VahemikAlgus = -5, double VahemikLopp = 5, double PuutujaX = 0, string? TaskCode = null);
