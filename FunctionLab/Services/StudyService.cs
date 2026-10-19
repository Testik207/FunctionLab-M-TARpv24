using System.Globalization;
namespace FunctionLab.Services;
public record LocalText(string Ru,string Et);
public record StudyStep(string Key,LocalText Title,LocalText Explanation,LocalText Result);
public record ValueRow(double X,double? Y);
public record SignInterval(double? Left,double? Right,int Sign,double TestX,double TestValue)
{
    public bool LeftClosed { get; init; }
    public bool RightClosed { get; init; }
}
public record LineResult(double X,double? Y,double? Slope,LocalText Tangent,LocalText Normal,bool Available);
public record Optimum(double X,double Y);
public record OptimizationResult(bool Guaranteed,Optimum? Minimum,Optimum? Maximum,List<ValueRow> Candidates,LocalText Note);
public record StudyResult(int Version,LocalText Domain,LocalText Range,LocalText Parity,LocalText Scope,List<SignInterval> Positive,List<SignInterval> Negative,List<SignInterval> Increasing,List<SignInterval> Decreasing,List<ValueRow> Table,LineResult Lines,OptimizationResult Optimization,List<StudyStep> Steps,string? TaskCode,bool IntervalsGlobal);
public static class StudyService
{
    static LocalText L(string ru,string et)=>new(ru,et);
    public static string F(double v)=>Math.Abs(v)<1e-9?"0":v.ToString("G8",CultureInfo.InvariantCulture);
    static string Interval(double? l,double? r,bool lc=false,bool rc=false)=>$"{(lc?"[":"(")}{(l.HasValue?F(l.Value):"−∞")}; {(r.HasValue?F(r.Value):"+∞")}{(rc?"]":")")}";
    static string Union(IEnumerable<SignInterval> items)=>string.Join(" ∪ ",items.Select(v=>Interval(v.Left,v.Right)));
    enum Symmetry { Unknown,Even,Odd,Neither,Zero }
    record Guard(Expr Expression,string Relation);
    static List<Guard> Guards(Expr tree)
    {
        var list=new List<Guard>();
        void Visit(Expr e) {
            if(e.A!=null)Visit(e.A);if(e.B!=null)Visit(e.B);
            if(e.Op=="/")list.Add(new(e.B!,"≠"));
            if(e.Op is "ln" or "log")list.Add(new(e.A!,">"));
            if(e.Op=="sqrt")list.Add(new(e.A!,"≥"));
            if(e.Op=="tan")list.Add(new(new("cos",null,e.A),"≠"));
            if(e.Op=="^") {
                if(e.B!.Op!="number" || e.B.Value%1!=0)list.Add(new(e.A!,">"));
                else if(e.B.Value<=0)list.Add(new(e.A!,"≠"));
            }
        }
        Visit(tree);return list;
    }
    static Symmetry Parity(Expr e)
    {
        var p=Polynomial.From(e);
        if(p!=null){if(p.All(v=>v==0))return Symmetry.Zero;bool even=true,odd=true;for(int i=0;i<p.Length;i++)if(p[i]!=0){if(i%2==0)odd=false;else even=false;}return even?Symmetry.Even:odd?Symmetry.Odd:Symmetry.Neither;}
        var a=e.A==null?Symmetry.Unknown:Parity(e.A);var b=e.B==null?Symmetry.Unknown:Parity(e.B);
        if(e.Op=="neg")return a;
        if(e.Op is "+" or "-"){if(a==Symmetry.Zero)return b;if(b==Symmetry.Zero)return a;return a==b?a:Symmetry.Unknown;}
        if(e.Op is "*" or "/"){
            if(a is Symmetry.Zero)return Symmetry.Zero;
            if(a is Symmetry.Even or Symmetry.Odd && b is Symmetry.Even or Symmetry.Odd)return a==b?Symmetry.Even:Symmetry.Odd;
        }
        if(e.Op=="^" && e.B!.Op=="number"){
            if(a==Symmetry.Even)return Symmetry.Even;
            if(a==Symmetry.Odd && e.B.Value%1==0)return e.B.Value%2==0?Symmetry.Even:Symmetry.Odd;
        }
        if(e.Op=="cos" && a is Symmetry.Even or Symmetry.Odd)return Symmetry.Even;
        if(e.Op is "sin" or "tan" && a is Symmetry.Even or Symmetry.Odd)return a;
        if(e.Op is "sqrt" or "ln" or "log" or "exp" && a==Symmetry.Even)return Symmetry.Even;
        return Symmetry.Unknown;
    }
    static List<double> FindRoots(Expr e,double min,double max)
    {
        var p=Polynomial.From(e);if(p!=null){try{return Polynomial.Roots(p);}catch(ArgumentException){}}
        return AnalysisService.Scan(e.Eval,Expr.X,min,max);
    }
    static double Probe(double? l,double? r)=>l is null?(r??0)-Math.Max(1,Math.Abs(r??0)+1):r is null?l.Value+Math.Max(1,Math.Abs(l.Value)+1):(l.Value+r.Value)/2;
    static bool BoundaryAllowed(Guard g,double x)
    {
        var y=g.Expression.Eval(x);if(!double.IsFinite(y))return false;
        var p=Polynomial.From(g.Expression);double scale=0;
        if(p!=null){for(int i=p.Length-1;i>=0;i--)scale=scale*Math.Abs(x)+Math.Abs(p[i]);}
        var zero=p!=null?Math.Abs(y)<=1e-8*scale:Math.Abs(y)<=1e-10;
        return zero?g.Relation=="≥":g.Relation=="≠" || y>0;
    }
    static bool SafeInterval(Expr tree,double? left,double? right)
    {
        double l=left??Probe(null,right),r=right??Probe(left,null);
        return new[]{.1,.3,.5,.7,.9}.All(t=>tree.InDomain(l+(r-l)*t));
    }
    public static StudyResult Build(AnalysisResult a,double tangentX,string? taskCode)
    {
        var tree=a.FunctionTree;var derivative=a.DerivativeTree;var p=Polynomial.From(tree);var guards=Guards(tree);
        var algebraicDomain=guards.All(g=>Polynomial.From(g.Expression)!=null);
        var boundaries=new List<double>();foreach(var g in guards)foreach(var x in FindRoots(g.Expression,a.Min,a.Max))Polynomial.Add(boundaries,x);boundaries.Sort();
        // Exact interval domain for polynomial constraints. General constraints retain set-builder notation.
        LocalText domain;
        if(algebraicDomain) {
            var cuts=new List<double?>{null};cuts.AddRange(boundaries.Select(v=>(double?)v));cuts.Add(null);var pieces=new List<string>();
            for(int i=0;i<cuts.Count-1;i++){
                var l=cuts[i];var r=cuts[i+1];var x=Probe(l,r);
                if(tree.InDomain(x))pieces.Add(Interval(l,r,l.HasValue&&guards.All(g=>BoundaryAllowed(g,l.Value)),r.HasValue&&guards.All(g=>BoundaryAllowed(g,r.Value))));
            }
            // Isolated included points, e.g. sqrt(-x^2), must not be lost.
            foreach(var x in boundaries)if(guards.All(g=>BoundaryAllowed(g,x)) && !tree.InDomain(x-1e-5) && !tree.InDomain(x+1e-5))pieces.Add("{"+F(x)+"}");
            var text=pieces.Count==0?"∅":pieces.Count==1&&pieces[0]=="(−∞; +∞)"?"ℝ":string.Join(" ∪ ",pieces);
            domain=L(text,text);
        }else {var c=tree.Conditions();var text="{ x ∈ ℝ | "+string.Join("; ",c)+" }";domain=L(text,text.Replace("(вещественная степень через exp/ln)","(reaalne astendamine exp/ln abil)"));}
        var parity=Parity(tree);
        bool symmetricDomain=guards.All(g=>{var s=Parity(g.Expression);return g.Relation=="≠"?s is Symmetry.Even or Symmetry.Odd:s is Symmetry.Even or Symmetry.Zero;});
        LocalText parityText;
        if(symmetricDomain && parity==Symmetry.Zero)parityText=L("Чётная и нечётная: f(x) ≡ 0","Paaris ja paaritu: f(x) ≡ 0");
        else if(symmetricDomain && parity==Symmetry.Even)parityText=L("Чётная: f(−x) = f(x)","Paaris: f(−x) = f(x)");
        else if(symmetricDomain && parity==Symmetry.Odd)parityText=L("Нечётная: f(−x) = −f(x)","Paaritu: f(−x) = −f(x)");
        else if(p!=null || guards.Any(g=>g.Relation!="≠" && Polynomial.From(g.Expression) is {Length:2} q && q[1]!=0))parityText=L("Ни чётная, ни нечётная","Ei paaris ega paaritu");
        else parityText=L("Символьная проверка не дала доказательства","Sümboolne kontroll ei andnud tõestust");
        bool global=a.AllRoots;
        List<SignInterval> Signs(Expr expression,IEnumerable<double> zeros,int sign)
        {
            var rootList=zeros.ToList();
            var splits=rootList.Concat(boundaries).Where(x=>global || x>a.Min&&x<a.Max).Distinct().Order().ToList();
            var cuts=new List<double?>{global?null:a.Min};cuts.AddRange(splits.Select(x=>(double?)x));cuts.Add(global?null:a.Max);
            var found=new List<SignInterval>();for(int i=0;i<cuts.Count-1;i++){
                var l=cuts[i];var r=cuts[i+1];if(l.HasValue&&r.HasValue&&r.Value-l.Value<1e-8)continue;
                var x=Probe(l,r);var y=expression.Eval(x);
                bool Closed(double? point) {
                    if(!point.HasValue || rootList.Any(z=>Math.Abs(z-point.Value)<1e-7*Math.Max(1,Math.Abs(z))))return false;
                    var value=expression.Eval(point.Value);
                    return tree.InDomain(point.Value) && guards.All(g=>BoundaryAllowed(g,point.Value)) && double.IsFinite(value) && Math.Sign(value)==sign;
                }
                if(double.IsFinite(y) && Math.Sign(y)==sign && SafeInterval(tree,l,r))found.Add(new(l,r,sign,x,y){LeftClosed=Closed(l),RightClosed=Closed(r)});
            }return found;
        }
        var positive=Signs(tree,a.Roots,1);var negative=Signs(tree,a.Roots,-1);
        var increasing=Signs(derivative,a.CriticalPoints,1);var decreasing=Signs(derivative,a.CriticalPoints,-1);
        // Absolute extrema on a closed interval: endpoints + stationary points for continuous polynomials.
        var candidateXs=a.CriticalPoints.Where(x=>x>=a.Min&&x<=a.Max).Concat(new[]{a.Min,a.Max}).Distinct().Order().ToList();
        var candidates=candidateXs.Select(x=>new ValueRow(x,tree.InDomain(x)?tree.Eval(x):null)).ToList();
        bool guaranteed=a.AllRoots;
        var finite=candidates.Where(v=>v.Y.HasValue).ToList();
        Optimum? low=null,high=null;
        if(guaranteed&&finite.Count>0){var lo=finite.MinBy(v=>v.Y!.Value)!;var hi=finite.MaxBy(v=>v.Y!.Value)!;low=new(lo.X,lo.Y!.Value);high=new(hi.X,hi.Y!.Value);}
        var opt=new OptimizationResult(guaranteed,low,high,candidates,guaranteed?
            L("На замкнутом отрезке сравниваем концы и все стационарные точки. При равных значениях таблица показывает дополнительные решения.","Kinnisel lõigul võrdleme otspunkte ja kõiki statsionaarseid punkte. Võrdsete väärtuste korral näitab tabel ka teisi lahendeid."):
            L("Для этой функции глобальный оптимум не сертифицирован. Проверьте непрерывность, границы области, разрывы и полноту списка критических точек. Таблица содержит только кандидатов.","Selle funktsiooni globaalset optimumi ei ole tõestatud. Kontrolli pidevust, määramispiirkonna piire, katkestusi ja kriitiliste punktide loendi täielikkust. Tabel sisaldab ainult kandidaate."));
        LocalText range;
        if(a.AllRoots && p!=null){
            if(p.Length==1)range=L("{"+F(p[0])+"}","{"+F(p[0])+"}");
            else if((p.Length-1)%2==1)range=L("ℝ","ℝ");
            else{var values=a.CriticalPoints.Select(tree.Eval).ToList();var v=p[^1]>0?values.Min():values.Max();var text=p[^1]>0?"["+F(v)+"; +∞)":"(−∞; "+F(v)+"]";range=L(text,text);}
        }else if(tree.Op is "sin" or "cos" && Polynomial.From(tree.A!) is {Length:2} lin && lin[1]!=0)range=L("[−1; 1]","[−1; 1]");
        else if(tree.Op is "ln" or "log" && Polynomial.From(tree.A!) is {Length:2} logArg && logArg[1]!=0)range=L("ℝ","ℝ");
        else if(tree.Op=="exp" && Polynomial.From(tree.A!) is {Length:2} expArg && expArg[1]!=0)range=L("(0; +∞)","(0; +∞)");
        else if(tree.Op=="sqrt" && Polynomial.From(tree.A!) is {Length:2} sqrtArg && sqrtArg[1]!=0)range=L("[0; +∞)","[0; +∞)");
        else if(tree.Op=="/" && tree.A!.Op=="number" && tree.A.Value!=0 && Polynomial.From(tree.B!) is {Length:2} denom && denom[1]!=0)range=L("ℝ ∖ {0}","ℝ ∖ {0}");
        else range=L("Глобальная область значений не установлена; численная таблица ниже не доказывает её полноту.","Globaalset muutumispiirkonda ei ole leitud; allolev arvuline tabel ei tõesta selle täielikkust.");
        var table=new[]{a.Min,a.Min+(a.Max-a.Min)/4,(a.Min+a.Max)/2,a.Min+3*(a.Max-a.Min)/4,a.Max}.Concat(tree.InDomain(0)?new[]{0.0}:Array.Empty<double>()).Distinct().Order().Select(x=>new ValueRow(x,tree.InDomain(x)?tree.Eval(x):null)).ToList();
        var y0=tree.InDomain(tangentX)?tree.Eval(tangentX):(double?)null;var slope=y0.HasValue&&double.IsFinite(derivative.Eval(tangentX))&&derivative.InDomain(tangentX)?derivative.Eval(tangentX):(double?)null;
        LocalText tangent,normal;
        if(!y0.HasValue){tangent=normal=L("x₀ не принадлежит области определения.","x₀ ei kuulu määramispiirkonda.");}
        else if(!slope.HasValue){tangent=normal=L("Конечная производная в x₀ не найдена. Возможна вертикальная касательная; нужна отдельная проверка предела.","Lõplikku tuletist punktis x₀ ei leitud. Võimalik on vertikaalne puutuja; piirmäära tuleb eraldi kontrollida.");}
        else{var t=$"y = {F(y0.Value)} + ({F(slope.Value)})·(x − ({F(tangentX)}))";var n=slope.Value==0?$"x = {F(tangentX)}":$"y = {F(y0.Value)} + ({F(-1/slope.Value)})·(x − ({F(tangentX)}))";tangent=L(t,t);normal=L(n,n);}
        var lines=new LineResult(tangentX,y0,slope,tangent,normal,slope.HasValue);
        var scope=global?L("Полином: интервалы знака и монотонности на всей ℝ; координаты приближённые.","Polünoom: märgi- ja monotoonsusvahemikud kogu hulgal ℝ; koordinaadid on ligikaudsed."):
            L($"Численное исследование на [{F(a.Min)}; {F(a.Max)}]. Интервалы знака и монотонности — кандидаты, а не глобальное доказательство.",$"Arvuline uurimine vahemikus [{F(a.Min)}; {F(a.Max)}]. Märgi- ja monotoonsusvahemikud on kandidaadid, mitte globaalne tõestus.");
        var study=new StudyResult(3,domain,range,parityText,scope,positive,negative,increasing,decreasing,table,lines,opt,[],taskCode,global);
        return study with { Steps=SolutionBuilder.Build(a,study) };
    }
}

public record AppliedTask(string Code,LocalText Title,LocalText Explanation,string Formula,string Latex,double Min,double Max,LocalText Units);
public static class Tasks
{
    public static readonly AppliedTask[] All=[
        new("rectangle",new("Прямоугольник: наибольшая площадь","Ristkülik: suurim pindala"),new("Периметр прямоугольника равен 40 м. Обозначим одну сторону x, тогда другая 20−x. Площадь A=x(20−x), 0≤x≤20. Концы соответствуют вырожденной фигуре; для настоящего прямоугольника 0<x<20. Сравнив кандидатов, получаем x=10 м, вторая сторона 10 м и A=100 м².","Ristküliku ümbermõõt on 40 m. Üks külg olgu x, teine on siis 20−x. Pindala A=x(20−x), 0≤x≤20. Otspunktid annavad degenereerunud kujundi; tegeliku ristküliku korral 0<x<20. Kandidaate võrreldes saame x=10 m, teine külg 10 m ja A=100 m²."),"x*(20-x)","x(20-x)",0,20,new("x — м, A — м²","x — m, A — m²")),
        new("box",new("Коробка: наибольший объём","Karp: suurim ruumala"),new("Из листа 20×30 см вырезаем в углах квадраты со стороной x и сгибаем коробку без крышки. V=x(20−2x)(30−2x), 0<x<10. На замыкании [0;10] концы дают V=0, а внутренний максимум соответствует реальной коробке. Ищем V′=0 и выбираем допустимый корень; x≈3.92375 см, V≈1056.31 см³.","20×30 cm lehest lõikame nurkadest välja ruudud küljega x ja voldime kaaneta karbi. V=x(20−2x)(30−2x), 0<x<10. Sulundil [0;10] annavad otspunktid V=0 ning sisemine maksimum vastab tegelikule karbile. Lahendame V′=0 ja valime lubatud juure; x≈3.92375 cm, V≈1056.31 cm³."),"x*(20-2*x)*(30-2*x)","x(20-2x)(30-2x)",0,10,new("x — см, V — см³","x — cm, V — cm³")),
        new("profit",new("Прибыль: оптимальный объём продаж","Kasum: optimaalne müügimaht"),new("Модель прибыли P(x)=−2x²+120x−400 евро; x — число проданных единиц, 0≤x≤60. Исследуем непрерывную модель, затем проверяем целочисленное ограничение. P′=−4x+120=0 даёт x=30, это целое число. Максимальная прибыль P(30)=1400 евро.","Kasumimudel on P(x)=−2x²+120x−400 eurot; x on müüdud ühikute arv, 0≤x≤60. Uurime pidevat mudelit ning kontrollime täisarvulist piirangut. P′=−4x+120=0 annab x=30, mis on täisarv. Suurim kasum P(30)=1400 eurot."),"-2*x^2+120*x-400","-2x^2+120x-400",0,60,new("x — единицы, P — €","x — ühikud, P — €"))
    ];
    public static AppliedTask? Get(string? code)=>All.FirstOrDefault(t=>t.Code==code);
}
