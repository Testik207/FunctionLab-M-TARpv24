using FunctionLab.Data;
using FunctionLab.Models;
using FunctionLab.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<UurimiseKontekst>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Uurimised") ?? "Data Source=uurimised.db"));
builder.Services.AddSingleton<AnalysisService>();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context,next) => {
    context.Response.Headers["X-Content-Type-Options"]="nosniff";
    // Same-origin writes. No CORS enabled. Local classroom application, not a multi-user service.
    if(context.Request.Method is "POST" or "PUT" or "DELETE") {
        if(context.Request.Headers["X-FunctionLab"]!="1" || context.Request.Headers["Sec-Fetch-Site"]=="cross-site") {
            context.Response.StatusCode=403;await context.Response.WriteAsJsonAsync(new { error="Запрос должен исходить из интерфейса приложения." });return;
        }
    }
    await next();
});
app.UseDefaultFiles();app.UseStaticFiles();
app.MapPost("/api/analyze", (UurimiseSisend input, AnalysisService service) => {
    try {return Results.Ok(service.Analyze(input));}catch(ArgumentException ex){return Results.BadRequest(new {error=ex.Message,errorEt=ErrorLocalization.Estonian(ex.Message)});}
});
app.MapGet("/api/history",async(UurimiseKontekst db,int? take)=>Results.Ok(await db.FunktsiooniUurimised.AsNoTracking().OrderByDescending(e=>e.LuodudAeg).ThenByDescending(e=>e.Id).Take(Math.Clamp(take??50,1,200)).Select(e=>new{e.Id,e.Valem,e.Latex,e.Tuletis,e.LuodudAeg,e.MuudetudAeg,e.VahemikAlgus,e.VahemikLopp}).ToListAsync()));
app.MapGet("/api/history/{id:int}",async(int id,UurimiseKontekst db,AnalysisService service)=> {
    var entity=await db.FunktsiooniUurimised.AsNoTracking().FirstOrDefaultAsync(e=>e.Id==id);
    if(entity is null)return Results.NotFound(new{error="Запись не найдена.",errorEt="Kirjet ei leitud."});
    var analysis=JsonSerializer.Deserialize<JsonElement>(entity.LahendusJson);
    // Preserve old databases. Enrich v1 solutions in memory without changing their saved record.
    if(!analysis.TryGetProperty("study",out var study) || study.ValueKind==JsonValueKind.Null || !study.TryGetProperty("version",out var version) || version.GetInt32()<3) {
        var tangentX=study.ValueKind==JsonValueKind.Object && study.TryGetProperty("lines",out var lines) && lines.TryGetProperty("x",out var x) ? x.GetDouble() : 0;
        var taskCode=study.ValueKind==JsonValueKind.Object && study.TryGetProperty("taskCode",out var code) && code.ValueKind==JsonValueKind.String ? code.GetString() : null;
        return Results.Ok(new{record=entity,analysis=(object)service.Analyze(new(entity.Valem,entity.Latex,entity.VahemikAlgus,entity.VahemikLopp,tangentX,taskCode))});
    }
    return Results.Ok(new{record=entity,analysis=(object)analysis});
});
app.MapPost("/api/history",async(UurimiseSisend input,UurimiseKontekst db,AnalysisService service)=> {
    try {var entity=service.Fill(new(),input);await db.FunktsiooniUurimised.AddAsync(entity);await db.SaveChangesAsync();return Results.Created($"/api/history/{entity.Id}",new{entity.Id});}
    catch(ArgumentException ex){return Results.BadRequest(new{error=ex.Message,errorEt=ErrorLocalization.Estonian(ex.Message)});}
});
app.MapPut("/api/history/{id:int}",async(int id,UurimiseSisend input,UurimiseKontekst db,AnalysisService service)=> {
    var entity=await db.FunktsiooniUurimised.FindAsync(id);if(entity is null)return Results.NotFound(new{error="Запись не найдена.",errorEt="Kirjet ei leitud."});
    try {service.Fill(entity,input);entity.MuudetudAeg=DateTime.UtcNow;await db.SaveChangesAsync();return Results.Ok(new{entity.Id});}
    catch(ArgumentException ex){return Results.BadRequest(new{error=ex.Message,errorEt=ErrorLocalization.Estonian(ex.Message)});}
});
app.MapDelete("/api/history/{id:int}",async(int id,UurimiseKontekst db)=> {
    var entity=await db.FunktsiooniUurimised.FindAsync(id);if(entity is null)return Results.NotFound();db.FunktsiooniUurimised.Remove(entity);await db.SaveChangesAsync();return Results.NoContent();
});
using(var scope=app.Services.CreateScope()) {
    var db=scope.ServiceProvider.GetRequiredService<UurimiseKontekst>();await db.Database.MigrateAsync();
    if(!await db.FunktsiooniUurimised.AnyAsync()) {
        var service=scope.ServiceProvider.GetRequiredService<AnalysisService>();
        foreach(var example in new[]{new UurimiseSisend("x^3-3*x",@"x^3-3x"),new UurimiseSisend("x^2-4*x+3",@"x^2-4x+3",-2,6),new UurimiseSisend("x^3-12*x",@"x^3-12x")})await db.FunktsiooniUurimised.AddAsync(service.Fill(new(),example));
        await db.SaveChangesAsync();
    }
}
app.Run();
