namespace FunctionLab.Services;
public static class SolutionBuilder
{
    static string F(double x)=>StudyService.F(x);
    static string I(SignInterval v)=>$"{(v.LeftClosed?"[":"(")}{(v.Left.HasValue?F(v.Left.Value):"−∞")}; {(v.Right.HasValue?F(v.Right.Value):"+∞")}{(v.RightClosed?"]":")")}";
    static string U(IEnumerable<SignInterval> list){var values=list.Select(I).ToList();return values.Count==0?"∅":string.Join(" ∪ ",values);}
    static string Table(IEnumerable<ValueRow> values)=>string.Join("\n",values.Select(v=>$"x = {F(v.X)} → f(x) = {(v.Y.HasValue?F(v.Y.Value):"∉ D(f)")}"));
    public static List<StudyStep> Build(AnalysisResult a,StudyResult s)
    {
        var steps=new List<StudyStep>();var tree=a.FunctionTree;var p=Polynomial.From(tree);
        void Add(string key,string ru,string et,string explanationRu,string explanationEt,string resultRu,string? resultEt=null)=>steps.Add(new(key,new(ru,et),new(explanationRu,explanationEt),new(resultRu,resultEt??resultRu)));
        Add("representation","1. Понятие функции и способы представления","1. Funktsioonide mõiste ja esitusviisid",
            "Функция сопоставляет каждому допустимому x ровно одно значение y. Представляем её тремя способами: аналитически формулой, численно таблицей и графически. Для таблицы подставляем выбранные значения x в исходную формулу.",
            "Funktsioon seab igale lubatud x-ile vastavusse täpselt ühe y-väärtuse. Esitame seda kolmel viisil: analüütiliselt valemiga, arvuliselt tabeliga ja graafiliselt. Tabeli jaoks asendame valitud x-väärtused algses valemis.",
            $"y = f(x) = {a.Formula}\n\n"+Table(s.Table));
        var conditions=tree.Conditions();
        var domainWork=conditions.Count==0?new LocalText("Дополнительных ограничений нет.","Lisapiiranguid ei ole."):new LocalText(string.Join("\n",conditions),string.Join("\n",conditions).Replace("(вещественная степень через exp/ln)","(reaalne aste exp/ln abil)"));
        var rangeReason=p!=null&&a.AllRoots?(p.Length==1?new LocalText("Постоянная функция принимает одно значение.","Konstantne funktsioon omandab ühe väärtuse."):(p.Length-1)%2==1?new LocalText("У полинома нечётной степени пределы при x→±∞ имеют разные знаки; непрерывность даёт все вещественные y.","Paaritu astmega polünoomi piirväärtused x→±∞ korral on vastasmärgilised; pidevus annab kõik reaalsed y-väärtused."):new LocalText("У полинома чётной степени оба конца направлены одинаково. Сравниваем значения всех стационарных точек и выбираем глобальную нижнюю или верхнюю границу.","Paaris astmega polünoomi mõlemad otsad suunduvad samasse suunda. Võrdleme kõigi statsionaarsete punktide väärtusi ja valime globaalse alumise või ülemise piiri.")):new LocalText("Для стандартных функций используем известные области значений. Если глобальный вывод не установлен, сообщаем это явно: минимум сетки не доказывает глобальную область значений.","Standardfunktsioonide jaoks kasutame tuntud muutumispiirkondi. Kui globaalset järeldust ei ole leitud, ütleme seda selgelt: arvulise võrgu miinimum ei tõesta globaalset muutumispiirkonda.");
        Add("domain-range","2. Область определения и область значений","2. Määramis- ja muutumispiirkond",
            "D(f) содержит допустимые x, E(f) — реально достигаемые y. Для D(f) знаменатели должны быть ненулевыми, аргумент ln положительным, выражение под sqrt неотрицательным. Пересекаем все ограничения исходной формулы; сокращение дроби не возвращает исключённые точки. "+rangeReason.Ru,
            "D(f) sisaldab lubatud x-väärtusi, E(f) tegelikult saavutatavaid y-väärtusi. D(f) leidmiseks peavad nimetajad olema nullist erinevad, ln-i argument positiivne ja sqrt-i alune avaldis mittenegatiivne. Võtame kõigi algse valemi tingimuste ühisosa; murdu taandades ei taastu välja jäetud punktid. "+rangeReason.Et,
            domainWork.Ru+"\nD(f) = "+s.Domain.Ru+"\nE(f) = "+s.Range.Ru,
            domainWork.Et+"\nD(f) = "+s.Domain.Et+"\nE(f) = "+s.Range.Et);
        var negativeX=System.Text.RegularExpressions.Regex.Replace(tree.ToString(),@"\bx\b","(−x)");
        Add("parity","3. Чётная и нечётная функция","3. Paaris ja paaritu funktsioon",
            "Сначала область определения должна быть симметрична относительно 0: вместе с x должна содержать −x. Подставляем −x вместо x. Если f(−x)=f(x), функция чётная; если f(−x)=−f(x), нечётная. Для полинома проверяем чётность степеней ненулевых слагаемых; для составных выражений используем правила симметрии. Отдельные числовые примеры не являются доказательством.",
            "Esmalt peab määramispiirkond olema nulli suhtes sümmeetriline: koos x-iga peab sinna kuuluma −x. Asendame x-i väärtusega −x. Kui f(−x)=f(x), on funktsioon paaris; kui f(−x)=−f(x), on see paaritu. Polünoomi korral kontrollime nullist erinevate liikmete astmete paarsust; liitavaldiste korral kasutame sümmeetriareegleid. Üksikud arvulised näited ei ole tõestus.",
            "f(−x) = "+negativeX+"\n"+s.Parity.Ru,"f(−x) = "+negativeX+"\n"+s.Parity.Et);
        string SignTable(IEnumerable<SignInterval> values,string name)=>string.Join("\n",values.OrderBy(v=>v.TestX).Select(v=>$"{I(v)}: {name}({F(v.TestX)}) ≈ {F(v.TestValue)} {(v.Sign>0?">":"<")} 0"));
        var rootsWork=RootWork(a);
        var zeros=a.IdenticallyZero?new LocalText("Все x ∈ ℝ","Kõik x ∈ ℝ"):a.Roots.Count==0?new LocalText("Корни не найдены.","Nullkohti ei leitud."):new LocalText(string.Join("\n",a.Roots.Select(x=>$"x ≈ {F(x)}; f(x) ≈ {F(tree.Eval(x))}")),string.Join("\n",a.Roots.Select(x=>$"x ≈ {F(x)}; f(x) ≈ {F(tree.Eval(x))}")));
        var signs=$"\nf(x)>0: {U(s.Positive)}\nf(x)<0: {U(s.Negative)}\n"+SignTable(s.Positive.Concat(s.Negative),"f");
        Add("roots-sign","4. Нули, положительность и отрицательность","4. Nullkohad, positiivsus ja negatiivsus",
            "Решаем f(x)=0, проверяем принадлежность каждого корня D(f) и подстановку в исходную формулу. Нули и разрывы делят ось на промежутки. Выбираем в каждом тестовую точку и определяем знак. Нули не включаются в строгие области f>0 и f<0. Корень чётной кратности может касаться оси без смены знака.",
            "Lahendame f(x)=0, kontrollime iga juure kuulumist D(f)-i ja asendame selle algses valemis. Nullkohad ja katkestused jaotavad telje vahemikeks. Valime igas vahemikus proovipunkti ning määrame märgi. Nullkohad ei kuulu rangetesse piirkondadesse f>0 ega f<0. Paarisarvulise kordsusega juur võib telge puudutada ilma märgimuutuseta.",
            rootsWork.Ru+"\n"+zeros.Ru+signs,rootsWork.Et+"\n"+zeros.Et+signs);
        var extremaRu=a.Extrema.Count==0?"Строгие локальные экстремумы не найдены.":string.Join("\n",a.Extrema.Select(e=>$"{e.Type}: x≈{F(e.X)}, y=f({F(e.X)})≈{F(e.Y)}; f′: {(e.Type=="Максимум"?"+ → −":"− → +")}"));
        var extremaEt=a.Extrema.Count==0?"Rangeid kohalikke ekstreemumeid ei leitud.":string.Join("\n",a.Extrema.Select(e=>$"{(e.Type=="Максимум"?"Maksimum":"Miinimum")}: x≈{F(e.X)}, y=f({F(e.X)})≈{F(e.Y)}; f′: {(e.Type=="Максимум"?"+ → −":"− → +")}"));
        var monotonic=$"f′>0: {U(s.Increasing)}\nf′<0: {U(s.Decreasing)}\n"+SignTable(s.Increasing.Concat(s.Decreasing),"f′");
        Add("monotonic-extrema","5. Возрастание, убывание и экстремумы","5. Kasvamine, kahanemine ja ekstreemumkohad",
            "Если f′>0 на промежутке, функция возрастает; если f′<0 — убывает. Смена знака +→− даёт локальный максимум, −→+ — локальный минимум. Если знак не меняется, строгого экстремума нет. Для координаты y подставляем найденный x в исходную функцию. Для неполиномов найденные интервалы являются численными кандидатами.",
            "Kui f′>0 vahemikus, funktsioon kasvab; kui f′<0, siis kahaneb. Märgimuutus +→− annab kohaliku maksimumi, −→+ kohaliku miinimumi. Kui märk ei muutu, ranget ekstreemumit ei ole. y-koordinaadi saamiseks asendame leitud x-i algses funktsioonis. Mittepolünoomide korral on leitud vahemikud arvulised kandidaadid.",
            monotonic+"\n"+extremaRu,monotonic+"\n"+extremaEt);
        Add("study","6. Исследование функции: сводка","6. Funktsiooni uurimine",
            "Собираем свойства в единую картину: область, симметрия, нули, знаки, направление изменения и экстремумы. Сопоставляем их с таблицей и графиком. Отсутствие численно найденной точки не доказывает её отсутствия.",
            "Koondame omadused tervikuks: piirkond, sümmeetria, nullkohad, märgid, muutumissuund ja ekstreemumid. Võrdleme neid tabeli ning graafikuga. Arvuliselt leidmata punkt ei tähenda tõestatud puudumist.",
            $"D(f)={s.Domain.Ru}\nE(f)={s.Range.Ru}\n{s.Parity.Ru}\nf′(x)={a.Derivative}\n{s.Scope.Ru}",
            $"D(f)={s.Domain.Et}\nE(f)={s.Range.Et}\n{s.Parity.Et}\nf′(x)={a.Derivative}\n{s.Scope.Et}");
        Add("derivative","7. Производная: понятие, нахождение, применение","7. Tuletise mõiste, leidmine ja kasutamine",
            "f′(x)=lim[h→0](f(x+h)−f(x))/h. Производная описывает мгновенную скорость изменения и наклон касательной. Дифференцируем исходное выражение по правилам суммы, произведения, частного, степени и цепному правилу. Ниже показаны правила и промежуточные производные каждого отличающегося подвыражения.",
            "f′(x)=lim[h→0](f(x+h)−f(x))/h. Tuletis kirjeldab hetkelist muutumiskiirust ja puutuja tõusu. Diferentseerime algset avaldist summa, korrutise, jagatise ja astme reeglite ning ahelreegli abil. Allpool on reeglid ja iga erineva alamavaldise vahetuletised.",
            string.Join("\n\n",DerivativeWork(tree))+"\n\nf′(x) = "+a.Derivative);
        var lineValues=$"x₀={F(s.Lines.X)}; y₀={(s.Lines.Y.HasValue?F(s.Lines.Y.Value):"—")}; k={(s.Lines.Slope.HasValue?F(s.Lines.Slope.Value):"—")}";
        Add("tangent-normal","8. Касательная и нормаль к графику","8. Funktsiooni graafiku puutuja ja normaal",
            "Вычисляем y₀=f(x₀), k=f′(x₀). Касательная: y−y₀=k(x−x₀). Нормаль перпендикулярна касательной и имеет наклон −1/k. Если k=0, касательная горизонтальна, нормаль вертикальна x=x₀. Если конечная производная не существует, требуется отдельная проверка предела, и эти формулы автоматически не применяются.",
            "Arvutame y₀=f(x₀), k=f′(x₀). Puutuja: y−y₀=k(x−x₀). Normaal on puutujaga risti ning selle tõus on −1/k. Kui k=0, on puutuja horisontaalne ja normaal vertikaalne x=x₀. Kui lõplikku tuletist ei ole, tuleb piirmäära eraldi kontrollida ning neid valemeid automaatselt ei rakendata.",
            lineValues+"\nКасательная: "+s.Lines.Tangent.Ru+"\nНормаль: "+s.Lines.Normal.Ru,
            lineValues+"\nPuutuja: "+s.Lines.Tangent.Et+"\nNormaal: "+s.Lines.Normal.Et);
        var dp=p==null?null:Polynomial.Derivative(p);
        var stationary=dp is {Length:1}&&dp[0]==0?new LocalText("Все точки D(f) стационарные; строгих экстремумов нет.","Kõik D(f)-i punktid on statsionaarsed; rangeid ekstreemumeid ei ole."):a.CriticalPoints.Count==0?new LocalText("Стационарные точки не найдены.","Statsionaarseid punkte ei leitud."):new LocalText(string.Join("\n",a.CriticalPoints.Select(x=>$"x≈{F(x)}: f′(x)≈{F(a.DerivativeTree.Eval(x))}, f(x)≈{F(tree.Eval(x))}")),string.Join("\n",a.CriticalPoints.Select(x=>$"x≈{F(x)}: f′(x)≈{F(a.DerivativeTree.Eval(x))}, f(x)≈{F(tree.Eval(x))}")));
        Add("derivative-study","9. Исследование функции с помощью производной","9. Funktsiooni uurimine tuletise abil",
            "Последовательность: находим f′, решаем f′=0, учитываем разрывы и точки без производной, составляем таблицу знаков f′ и делаем выводы о росте и экстремумах. Найденные стационарные точки не обязательно являются всеми критическими точками. Границы области и точки недифференцируемости нужно проверить отдельно; для неполиномов поиск может быть неполным.",
            "Järjekord: leiame f′, lahendame f′=0, arvestame katkestusi ja tuletiseta punkte, koostame f′ märgitabeli ning järeldame kasvamise ja ekstreemumid. Leitud statsionaarsed punktid ei pruugi olla kõik kriitilised punktid. Määramispiirkonna piire ja mittediferentseeruvaid punkte tuleb eraldi kontrollida; mittepolünoomide otsing võib olla mittetäielik.",
            "f′(x) = "+a.Derivative+" = 0\n"+stationary.Ru+"\n"+monotonic+"\n"+extremaRu,
            "f′(x) = "+a.Derivative+" = 0\n"+stationary.Et+"\n"+monotonic+"\n"+extremaEt);
        var o=s.Optimization;var task=Tasks.Get(s.TaskCode);
        var optimumRu=o.Minimum==null?"Глобальный оптимум не установлен.":$"min≈{F(o.Minimum.Y)} при x≈{F(o.Minimum.X)}\nmax≈{F(o.Maximum!.Y)} при x≈{F(o.Maximum.X)}";
        var optimumEt=o.Minimum==null?"Globaalset optimumi ei ole leitud.":$"min≈{F(o.Minimum.Y)}, kui x≈{F(o.Minimum.X)}\nmax≈{F(o.Maximum!.Y)}, kui x≈{F(o.Maximum.X)}";
        Add("optimization","10. Экстремумные задачи (применение)","10. Ekstreemumülesanded (rakendusülesanded)",
            "Для прикладной задачи обозначаем переменную, составляем целевую функцию и допустимые ограничения. Здесь введённая функция используется как цель, выбранный отрезок — как ограничение. Для непрерывного полинома сравниваем концы и все стационарные точки внутри отрезка. Затем связываем ответ с единицами задачи, проверяем целочисленность и допустимость. Приложение не переводит произвольный текст задачи в формулу. "+o.Note.Ru+(task==null?"":"\n"+task.Explanation.Ru),
            "Rakendusülesandes määrame muutuja, koostame sihifunktsiooni ja lubatud piirangud. Siin kasutatakse sisestatud funktsiooni eesmärgina ning valitud lõiku piiranguna. Pideva polünoomi korral võrdleme otspunkte ja kõiki lõigu sees olevaid statsionaarseid punkte. Seejärel seome vastuse ülesande ühikutega ning kontrollime täisarvulisust ja lubatavust. Rakendus ei teisenda vaba ülesandeteksti automaatselt valemiks. "+o.Note.Et+(task==null?"":"\n"+task.Explanation.Et),
            $"x ∈ [{F(a.Min)}; {F(a.Max)}]\nf(x)={a.Formula}\n"+Table(o.Candidates)+"\n"+optimumRu,
            $"x ∈ [{F(a.Min)}; {F(a.Max)}]\nf(x)={a.Formula}\n"+Table(o.Candidates)+"\n"+optimumEt);
        return steps;
    }
    static LocalText RootWork(AnalysisResult a)
    {
        var p=Polynomial.From(a.FunctionTree);var equation=$"{a.Formula} = 0";
        if(p is {Length:1})return p[0]==0?new("0=0: равенство верно для всех x ∈ D(f).","0=0: võrdus kehtib iga x ∈ D(f) korral."):new($"{F(p[0])}≠0: нулей нет.",$"{F(p[0])}≠0: nullkohti ei ole.");
        if(p is {Length:2}){var result=$"{F(p[1])}·x + ({F(p[0])}) = 0\nx = −({F(p[0])}) / ({F(p[1])}) ≈ {F(-p[0]/p[1])}";return new(result,result);}
        string Quadratic(double c,double b,double leading){var d=b*b-4*leading*c;return $"a={F(leading)}, b={F(b)}, c={F(c)}\nD=b²−4ac={F(d)}\nx=(−b±√D)/(2a)";}
        if(p is {Length:3}){var result=equation+"\n"+Quadratic(p[0],p[1],p[2]);return new(result,result);}
        if(p is {Length:4} && p[0]==0){var q=Polynomial.Format(p.Skip(1).ToArray());var result=equation+$"\nx·({q}) = 0\nx=0 ∨ ({q})=0\n"+Quadratic(p[1],p[2],p[3]);return new(result,result);}
        if(a.AllRoots && p!=null && p.Length>1){var bound=F(Polynomial.Bound(p));return new(equation+$"\nГраница Коши: |x|≤{bound}. Разбиваем интервал по корням производной; уточняем корни бисекцией и проверяем кратные корни.",equation+$"\nCauchy piir: |x|≤{bound}. Jaotame vahemiku tuletise nullkohtade järgi, täpsustame juured poolitusmeetodiga ning kontrollime kordseid juuri.");}
        return new(equation+"\nЧисленный поиск на выбранном интервале; полнота для общей функции не гарантируется.",equation+"\nArvuline otsing valitud vahemikus; üldise funktsiooni korral ei ole täielikkus tagatud.");
    }
    static IEnumerable<string> DerivativeWork(Expr tree)
    {
        var result=new List<string>();var seen=new HashSet<string>();
        void Visit(Expr e){if(e.A!=null)Visit(e.A);if(e.B!=null)Visit(e.B);if(e.Op is "number" or "x")return;var expression=e.ToString();if(!seen.Add(expression))return;
            string rule=e.Op switch {"+"=>"(u+v)′=u′+v′","-"=>"(u−v)′=u′−v′","*"=>"(uv)′=u′v+uv′","/"=>"(u/v)′=(u′v−uv′)/v²","^" when e.B!.Op=="number"=>"(uⁿ)′=n·uⁿ⁻¹·u′","^"=>"(uᵛ)′=uᵛ(v′ln(u)+v·u′/u)","sin"=>"(sin u)′=cos(u)·u′","cos"=>"(cos u)′=−sin(u)·u′","tan"=>"(tan u)′=u′/cos²(u)","ln" or "log"=>"(ln u)′=u′/u","sqrt"=>"(√u)′=u′/(2√u)","exp"=>"(exp u)′=exp(u)·u′",_=>"(−u)′=−u′"};
            var derivative=e.Derivative();var p=Polynomial.From(derivative);result.Add(rule+"\n["+expression+"]′ = "+(p==null?derivative.ToString():Polynomial.Format(p)));}
        Visit(tree);if(result.Count==0)result.Add(tree.Op=="x"?"(x)′ = 1":"(c)′ = 0");return result;
    }
}
