using System.Globalization;
using System.Text.Json;
using FunctionLab.Models;
namespace FunctionLab.Services;
public record SolutionStep(string Title, string Explanation, string Formula);
public record Extremum(double X,double Y,string Type);
public record AnalysisResult(string Formula,string Derivative,string Domain,string Scope,List<double> Roots,List<double> CriticalPoints,List<Extremum> Extrema,List<SolutionStep> Steps,Expr FunctionTree,Expr DerivativeTree,double Min,double Max,bool AllRoots,bool IdenticallyZero)
{
    public StudyResult? Study { get; init; }
}
public class AnalysisService
{
    static string F(double x) => Math.Abs(x)<1e-9?"0":x.ToString("G8",CultureInfo.InvariantCulture);
    public AnalysisResult Analyze(UurimiseSisend input)
    {
        if(!double.IsFinite(input.VahemikAlgus) || !double.IsFinite(input.VahemikLopp) || input.VahemikAlgus < -100 || input.VahemikLopp > 100 || input.VahemikLopp-input.VahemikAlgus<0.1)
            throw new ArgumentException("Выберите интервал внутри [−100; 100], длиной от 0.1.");
        if(!double.IsFinite(input.PuutujaX) || Math.Abs(input.PuutujaX)>100) throw new ArgumentException("x₀ должен быть внутри [−100; 100].");
        if(input.TaskCode!=null && Tasks.Get(input.TaskCode)==null) throw new ArgumentException("Неизвестная прикладная задача.");
        if(input.Latex?.Length>1000) throw new ArgumentException("LaTeX не должен превышать 1000 символов.");
        var tree=new ExpressionParser().Parse(input.Valem);var derivative=tree.Derivative();
        var polynomial=Polynomial.From(tree);var dp=polynomial is null?null:Polynomial.Derivative(polynomial);
        if(polynomial != null && polynomial.Any(v=>!double.IsFinite(v) || Math.Abs(v)>1e100)) throw new ArgumentException("Слишком большие коэффициенты полинома.");
        var conditions=tree.Conditions();var all=polynomial != null && conditions.Count==0;
        var domain=conditions.Count==0?"ℝ": "{ x ∈ ℝ | "+string.Join("; ",conditions)+" }";
        var min=input.VahemikAlgus;var max=input.VahemikLopp;
        var roots=polynomial != null?Polynomial.Roots(polynomial):Scan(tree.Eval,tree,min,max);
        roots=roots.Where(tree.InDomain).ToList();
        var critical=dp!=null?Polynomial.Roots(dp):derivative.Op=="number" && derivative.Value==0?new List<double>():Scan(derivative.Eval,tree,min,max);
        critical=critical.Where(x=>tree.InDomain(x) && double.IsFinite(derivative.Eval(x))).ToList();
        var extrema=new List<Extremum>();
        foreach(var x in critical) {
            var delta=Math.Min(1e-3*Math.Max(1,Math.Abs(x)),critical.Where(v=>v!=x).Select(v=>Math.Abs(v-x)/4).DefaultIfEmpty(1).Min());
            var left=derivative.Eval(x-delta);var right=derivative.Eval(x+delta);
            if(tree.InDomain(x-delta) && tree.InDomain(x+delta)) {
                if(left>0 && right<0)extrema.Add(new(x,tree.Eval(x),"Максимум"));
                if(left<0 && right>0)extrema.Add(new(x,tree.Eval(x),"Минимум"));
            }
        }
        var df=dp is null?derivative.ToString():Polynomial.Format(dp);
        var zero=polynomial is {Length:1} && polynomial[0]==0;
        var scope=all?"Полином: поиск всех вещественных корней по границе Коши; значения вычислены численно.":$"Численное исследование на [{F(min)}; {F(max)}]. Список корней и стационарных точек может быть неполным; границы области и точки недифференцируемости нужно проверить отдельно.";
        var steps=new List<SolutionStep> {
            new("1. Область определения",conditions.Count==0?"Операции исходной формулы определены для всех вещественных x.":"Одновременно применяем ограничения каждой операции исходной формулы. Они сохраняются даже при сокращении дроби.","D(f) = "+domain),
            new("2. Нули функции",zero?"Нулевой полином равен нулю при каждом x.":all?"Решаем f(x)=0. Граница Коши ограничивает все корни. Корни производной разбивают интервал на монотонные части; бисекция ищет смены знака, отдельная проверка сохраняет кратные корни.":"Ищем смены знака и касания нуля на сетке; затем уточняем кандидаты и проверяем невязку. Численный поиск не гарантирует нахождение всех корней.",zero?"x ∈ ℝ":roots.Count==0?"Корни не найдены":string.Join("; ",roots.Select(x=>"x ≈ "+F(x)))),
            new("3. Производная","Используем правила суммы, произведения, частного, степени и цепное правило. d(xⁿ)/dx = n·xⁿ⁻¹; d(g(h(x)))/dx = g′(h(x))·h′(x).","f′(x) = "+df),
            new("4. Стационарные точки",dp is {Length:1} && dp[0]==0?"Производная тождественно нулевая: все точки области стационарные, строгих экстремумов нет.":"Решаем f′(x)=0 и проверяем принадлежность области определения. Для общих функций это только стационарные точки; другие критические точки требуют отдельной проверки.",dp is {Length:1} && dp[0]==0?"Все x ∈ D(f)":critical.Count==0?"Изолированные стационарные точки не найдены":string.Join("; ",critical.Select(x=>"x ≈ "+F(x)))),
            new("5. Экстремумы","Смена знака f′ с + на − даёт локальный максимум, с − на + — локальный минимум. Если знак не меняется, стационарная точка не является строгим экстремумом.",extrema.Count==0?"Строгие локальные экстремумы не найдены":string.Join("; ",extrema.Select(e=>$"{e.Type}: ({F(e.X)}; {F(e.Y)})")))
        };
        var cuts=new List<double>{min};cuts.AddRange(critical.Where(x=>x>min && x<max));cuts.Add(max);cuts=cuts.Order().ToList();
        var intervals=new List<string>();for(int i=0;i<cuts.Count-1;i++) {
            var middle=(cuts[i]+cuts[i+1])/2;var d=derivative.Eval(middle);
            intervals.Add($"({F(cuts[i])}; {F(cuts[i+1])}): "+(!tree.InDomain(middle)||!double.IsFinite(d)?"есть ограничения области":d>0?"f′>0, рост в тестовой точке":d<0?"f′<0, убывание в тестовой точке":"f′=0 в тестовой точке"));
        }
        steps.Add(new("6. Поведение на интервале",all?"Для полинома знак производной между соседними стационарными точками постоянен; проверяем его в середине каждого интервала.":"Знак проверен в серединах промежутков. Это ориентир для графика, а не доказательство монотонности: возможны пропущенные корни и разрывы.",string.Join("; ",intervals)));
        steps.Add(new("7. Точность и границы исследования",scope,"Все координаты с ≈ — приближённые. Разрывы на графике не соединяются."));
        var result=new AnalysisResult(input.Valem,df,domain,scope,roots,critical,extrema,steps,tree,derivative,min,max,all,zero);
        var task=Tasks.Get(input.TaskCode);
        var taskCode=task!=null && tree.ToString()==new ExpressionParser().Parse(task.Formula).ToString() && min==task.Min && max==task.Max ? input.TaskCode : null;
        return result with { Study=StudyService.Build(result,input.PuutujaX,taskCode) };
    }
    internal static List<double> Scan(Func<double,double> fn,Expr domain,double min,double max)
    {
        const int count=4000;var found=new List<double>();var step=(max-min)/count;
        double Safe(double x)=>domain.InDomain(x)?fn(x):double.NaN;
        bool Valid(double x)=>double.IsFinite(Safe(x)) && Math.Abs(Safe(x))<1e-7;
        var previous=Safe(min);
        for(int i=0;i<=count;i++) {
            var x=min+i*step;var y=Safe(x);
            if(double.IsFinite(y) && y==0)Polynomial.Add(found,x);
            if(i>0 && double.IsFinite(y) && double.IsFinite(previous) && Math.Sign(y)!=Math.Sign(previous)) {
                var l=x-step;var r=x;var fl=previous;
                for(int k=0;k<60;k++){var m=(l+r)/2;var fm=Safe(m);if(!double.IsFinite(fm))break;if(fm==0){l=r=m;break;}if(Math.Sign(fm)==Math.Sign(fl)){l=m;fl=fm;}else r=m;}
                var root=(l+r)/2;if(Valid(root))Polynomial.Add(found,root);
            }
            // Minimize |f| locally to detect tangential (even multiplicity) roots.
            if(i>0 && i<count && double.IsFinite(y) && Math.Abs(y)<Math.Abs(previous) && Math.Abs(y)<Math.Abs(Safe(x+step))) {
                var l=x-step;var r=x+step;for(int k=0;k<55;k++){var u=l+(r-l)/3;var v=r-(r-l)/3;if(Math.Abs(Safe(u))<Math.Abs(Safe(v)))r=v;else l=u;}
                var root=(l+r)/2;if(Valid(root))Polynomial.Add(found,root);
            }
            previous=y;
        }
        return found.Order().ToList();
    }
    public FunktsiooniUurimine Fill(FunktsiooniUurimine entity,UurimiseSisend input)
    {
        var a=Analyze(input);entity.Valem=input.Valem;entity.Latex=input.Latex??"";entity.Maaramispiirkond=a.Domain;entity.Tuletis=a.Derivative;
        entity.Nullkohad=a.IdenticallyZero?"Все x ∈ ℝ":string.Join("; ",a.Roots.Select(F));
        entity.KriitilisedPunktid=a.Steps[3].Formula;entity.Ekstreemumid=a.Steps[4].Formula;
        entity.VahemikAlgus=a.Min;entity.VahemikLopp=a.Max;
        entity.LahendusJson=JsonSerializer.Serialize(a,new JsonSerializerOptions(JsonSerializerDefaults.Web));return entity;
    }
}
