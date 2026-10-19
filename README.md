# FunctionLab

Funktsioonide uurimise veebirakendus kooliprojektiks. **C#, .NET 10, ASP.NET Core Minimal API, Entity Framework Core ja SQLite.** Lahendus sisaldab ühte projekti: `FunctionLab`.

## Käivitamine

1. Paki arhiiv lahti **uude tühja kausta**. Ära kopeeri seda vana ehituskausta peale.
2. Ava Visual Studios `FunctionLab.slnx`.
3. Veendu, et valitud käivitusprojekt on `FunctionLab` ja arvutis on .NET 10 SDK.
4. Taasta NuGeti paketid ning käivita projekt nupuga Start või F5.
5. Ava `http://localhost:5080`. Kui aadressi on muudetud, vaata serveri käivituslogi.

Käsurealt sama lahenduse juurkaustas:

```bash
dotnet restore FunctionLab.slnx
dotnet build FunctionLab.slnx
dotnet run --project FunctionLab/FunctionLab.csproj
```

Windowsis saab kasutada ka `run.bat`, Linuxis ja macOS-is `bash run.sh`.

Esimene käivitus rakendab kaasasoleva EF Core'i migratsiooni `InitialCreate`, loob SQLite'i andmebaasi ning lisab kolm näidet. Andmebaasifail ei kuulu repositooriumi. Ühendusstring on `FunctionLab/appsettings.json` failis; suhteline failitee sõltub käivituskaustast.

## Funktsioonid

- Matemaatiline sisestus MathLive'iga ja kompaktne klaviatuur, sealhulgas murd **a/b**.
- Funktsiooni ja tuletise graafikud Chart.js-iga.
- Määramis- ja muutumispiirkond, paarsus, nullkohad, märgivahemikud, kasvamine, kahanemine ja ekstreemumid.
- Puutuja ja normaal valitud punktis.
- Täislahendus kõigi kümne õppeteema kohta.
- Uurimiste salvestamine, lugemine, muutmine ja kustutamine EF Core'i kaudu.
- Eraldi ajalugu ning eesti ja vene kasutajaliides.

MathLive, Chart.js, vajalikud fondid ja litsentsid on kohalikult kaasas. Internetti on vaja NuGeti pakettide esimeseks taastamiseks.

Näited: `x^3-3*x`, `x^2-4*x+3`, `sqrt(x)`, `ln(x)`, `sin(x)`, `1/x`. Sisesta ainult funktsiooni parem pool. Kümnenderaldaja on punkt; `log` tähendab naturaallogaritmi.

## Struktuur

```text
FunctionLab.slnx
FunctionLab/
  FunctionLab.csproj
  Program.cs
  Models/
  Data/
  Services/
  Migrations/
  Properties/
  wwwroot/
.config/dotnet-tools.json
.gitignore
README.md
run.bat
run.sh
```

Eraldi `FunctionLab.Tests` projekt ja selle käivitusskriptid on sellest versioonist eemaldatud. Rakenduse funktsioonid ei sõltu sellest projektist.

## GitHubi esitamine

Lisa repositooriumi selle kausta sisu: lähtekood, lahendusfail, migratsioonid, kohalikud veebiteegid, `.config`, `.gitignore` ja käivitusjuhend. Ära lisa `bin`, `obj`, `.vs`, `BuildHost-net472`, `BuildHost-netcore` ega kohalikku andmebaasi.

`.gitignore` välistab need genereeritavad failid. See ei eemalda faile, mis olid juba varasemas commit'is. Kui kasutad olemasolevat repositooriumi, eemalda vanad testifailid ning juba jälgitavad ehitusväljundid enne järgmise commit'i tegemist. Uues repositooriumis piisab puhta projekti lisamisest.

`BuildHost-*` kaustad ei ole rakenduse lisaprojektid ega tähenda, et FunctionLab nõuaks .NET Framework 4.7.2. Need võivad tekkida arendustööriistade sõltuvustest. Neid pole arhiivis ja neid ei ole vaja GitHubi saata. Projekt ise sihib ainult `net10.0`.

## Migratsioonid

Olemasolevad migratsioonifailid on vajalik osa ülesandest ja jäävad projekti. `Microsoft.EntityFrameworkCore.Design` on säilitatud EF Core'i arendustööriistade jaoks; seda ei ole põhjust koos ajutiste ehitusfailidega eemaldada.

Vajaduse korral:

```bash
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project FunctionLab
dotnet ef database update --project FunctionLab
```

Mudeli edasisel muutmisel lisa uus migratsioon:

```bash
dotnet ef migrations add NextChange --project FunctionLab
```

## Kontrollimine ja piirangud

Enne hindamiseks esitamist käivita projekt kooli .NET 10 keskkonnas ning kontrolli valemi uurimist, ajaloo CRUD-toiminguid ja andmete säilimist pärast taaskäivitust. Eraldi testiprojekti selles paketis pole.

Puhastamise käigus võrreldi rakenduse lähtefaile varasema versiooniga, kontrolliti lahendusfaili ning arhiivi sisu. Varasema versiooni kliendiloogika kontrollid olid läbitud. Täielikku .NET-ehitust ettevalmistuskeskkonnas ei kinnitata: CLR ei käivitu selles keskkonnas. See tuleb kinnitada kohalikus .NET 10 keskkonnas.

Polünoomide ja üldiste avaldiste tulemuste kindlus erineb. Polünoomide puhul kasutatakse juurte piiri ja tuletisel põhinevat otsingut; üldiste funktsioonide puhul on uurimine valitud vahemikus arvuline ning võib olla mittetäielik. Rakendus märgib need piirangud tulemuses. Vabatekstilist ülesannet automaatselt valemiks ei teisendata. Ühine kohalik ajalugu ei sisalda kasutajapõhist autentimist.
