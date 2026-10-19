using System.Globalization;
using System.Text.RegularExpressions;
namespace FunctionLab.Services;
// Restricted syntax tree: no compilation, reflection, eval, or execution of user code.
public record Expr(string Op, double? Value = null, Expr? A = null, Expr? B = null)
{
    public static Expr N(double n) => new("number", n);
    public static Expr X => new("x");
    public double Eval(double x)
    {
        var a = A?.Eval(x) ?? 0; var b = B?.Eval(x) ?? 0;
        return Op switch {
            "number" => Value!.Value, "x" => x, "+" => a+b, "-" => a-b, "*" => a*b,
            "/" => b == 0 ? double.NaN : a/b,
            "^" => a == 0 && b <= 0 ? double.NaN : Math.Pow(a,b),
            "neg" => -a, "sin" => Math.Sin(a), "cos" => Math.Cos(a),
            "tan" => Math.Abs(Math.Cos(a)) < 1e-12 ? double.NaN : Math.Tan(a),
            "ln" or "log" => a > 0 ? Math.Log(a) : double.NaN,
            "sqrt" => a >= 0 ? Math.Sqrt(a) : double.NaN, "exp" => Math.Exp(a),
            _ => double.NaN };
    }
    public static Expr S(string op, Expr a, Expr? b = null)
    {
        bool Is(Expr? e, double n) => e?.Op == "number" && e.Value == n;
        if (op == "neg" && a.Op == "number") return N(-a.Value!.Value);
        if (op == "+" && Is(a,0)) return b!;
        if (op is "+" or "-" && Is(b,0)) return a;
        if (op == "*" && (Is(a,0) || Is(b,0))) return N(0);
        if (op is "*" or "/" && Is(b,1)) return a;
        if (op == "*" && Is(a,1)) return b!;
        if (op == "^" && Is(b,0)) return N(1);
        if (op == "^" && Is(b,1)) return a;
        var e = new Expr(op, null, a,b);
        if (a.Op == "number" && b?.Op == "number") {
            var v = e.Eval(0); if (double.IsFinite(v)) return N(v);
        }
        return e;
    }
    public Expr Derivative()
    {
        var a=A!; var b=B!;
        return Op switch {
            "number" => N(0), "x" => N(1),
            "+" or "-" => S(Op,a.Derivative(),b.Derivative()),
            "neg" => S("neg",a.Derivative()),
            "*" => S("+",S("*",a.Derivative(),b),S("*",a,b.Derivative())),
            "/" => S("/",S("-",S("*",a.Derivative(),b),S("*",a,b.Derivative())),S("^",b,N(2))),
            "^" when b.Op == "number" => S("*",S("*",b,S("^",a,N(b.Value!.Value-1))),a.Derivative()),
            "^" => S("*",this,S("+",S("*",b.Derivative(),S("ln",a)),S("/",S("*",b,a.Derivative()),a))),
            "sin" => S("*",S("cos",a),a.Derivative()),
            "cos" => S("*",S("neg",S("sin",a)),a.Derivative()),
            "tan" => S("/",a.Derivative(),S("^",S("cos",a),N(2))),
            "ln" or "log" => S("/",a.Derivative(),a),
            "sqrt" => S("/",a.Derivative(),S("*",N(2),S("sqrt",a))),
            "exp" => S("*",S("exp",a),a.Derivative()),
            _ => throw new ArgumentException("Неизвестная операция.") };
    }
    public override string ToString() => Op switch {
        "number" => Value!.Value.ToString("G10",CultureInfo.InvariantCulture), "x" => "x",
        "neg" => $"(-{A})", "+" or "-" or "*" or "/" or "^" => $"({A}{Op}{B})", _ => $"{Op}({A})" };
    public List<string> Conditions()
    {
        var list = new List<string>();
        void Visit(Expr e) {
            if(e.A != null) Visit(e.A); if(e.B != null) Visit(e.B);
            if(e.Op == "/" && !(e.B!.Op == "number" && e.B.Value != 0)) list.Add($"{e.B} ≠ 0");
            if(e.Op is "ln" or "log") list.Add($"{e.A} > 0");
            if(e.Op == "sqrt") list.Add($"{e.A} ≥ 0");
            if(e.Op == "tan") list.Add($"cos({e.A}) ≠ 0");
            if(e.Op == "^") {
                if(e.B!.Op != "number" || e.B.Value % 1 != 0) list.Add($"{e.A} > 0 (вещественная степень через exp/ln)");
                else if(e.B.Value <= 0) list.Add($"{e.A} ≠ 0");
            }
        }
        Visit(this); return list.Distinct().ToList();
    }
    // Domain preserved on the original tree even if the derivative is simplified.
    public bool InDomain(double x) {
        if(A != null && !A.InDomain(x) || B != null && !B.InDomain(x)) return false;
        if(Op == "^" && (B!.Op != "number" || B.Value % 1 != 0) && A!.Eval(x) <= 0) return false;
        return double.IsFinite(Eval(x));
    }
}
public class ExpressionParser
{
    private List<string> tokens = []; private int pos; private int depth;
    public Expr Parse(string text)
    {
        if(string.IsNullOrWhiteSpace(text) || text.Length > 400) throw new ArgumentException("Формула обязательна, максимум 400 символов.");
        text=text.ToLowerInvariant().Replace(" ","").Replace("**","^").Replace("−","-").Replace("·","*").Replace("×","*");
        tokens=Regex.Matches(text,@"(?:\d+(?:\.\d*)?|\.\d+)(?:e[+-]?\d+)?|[a-z]+|[()+*/^,-]").Select(m=>m.Value).ToList();
        if(string.Concat(tokens)!=text || tokens.Count>120) throw new ArgumentException("Используйте x, числа, + − * / ^ и sin, cos, tan, ln, log, sqrt, exp. Десятичный разделитель — точка.");
        pos=0; depth=0; var result=Sum();
        if(pos!=tokens.Count) throw new ArgumentException($"Неожиданный символ: {tokens[pos]}.");
        int TreeDepth(Expr e) => 1 + Math.Max(e.A is null ? 0 : TreeDepth(e.A), e.B is null ? 0 : TreeDepth(e.B));
        if(TreeDepth(result)>24) throw new ArgumentException("Слишком глубокая формула: максимум 24 уровня операций.");
        return result;
    }
    string? Peek => pos<tokens.Count ? tokens[pos] : null;
    bool Take(string t) { if(Peek!=t) return false; pos++; return true; }
    Expr Sum() { var a=Product(); while(Peek is "+" or "-"){var op=tokens[pos++]; a=new(op,null,a,Product());} return a; }
    Expr Product() {
        var a=Unary();
        while(true) {
            if(Peek is "*" or "/") {var op=tokens[pos++]; a=new(op,null,a,Unary());}
            else if(Peek is "(" || Peek != null && (char.IsLetter(Peek[0]) || char.IsDigit(Peek[0]))) a=new("*",null,a,Unary());
            else return a;
        }
    }
    Expr Unary() {
        if(++depth>32) throw new ArgumentException("Слишком сложная формула.");
        Expr e;
        if(Take("+")) e=Unary(); else if(Take("-")) e=new("neg",null,Unary());
        else {e=Atom(); if(Take("^")) {
            var b=Unary(); if(b.Op=="neg" && b.A!.Op=="number") b=Expr.N(-b.A.Value!.Value);
            if(b.Op=="number" && Math.Abs(b.Value!.Value)>20) throw new ArgumentException("Модуль постоянной степени не должен превышать 20.");
            e=new("^",null,e,b);
        }}
        depth--; return e;
    }
    Expr Atom() {
        if(Take("(")) {var e=Sum(); if(!Take(")")) throw new ArgumentException("Не хватает закрывающей скобки."); return e;}
        var t=Peek ?? throw new ArgumentException("Формула не завершена."); pos++;
        if(double.TryParse(t,NumberStyles.Float,CultureInfo.InvariantCulture,out var n) && double.IsFinite(n)) { if(Math.Abs(n)>1e12) throw new ArgumentException("Модуль числа не должен превышать 10¹²."); return Expr.N(n); }
        if(t=="x") return Expr.X; if(t=="pi") return Expr.N(Math.PI); if(t=="e") return Expr.N(Math.E);
        if(t is "sin" or "cos" or "tan" or "ln" or "log" or "sqrt" or "exp") {
            if(!Take("(")) throw new ArgumentException($"Аргумент {t} нужно заключить в скобки.");
            var a=Sum(); if(!Take(")")) throw new ArgumentException("Не хватает закрывающей скобки."); return new(t,null,a);
        }
        throw new ArgumentException($"Неизвестный символ: {t}.");
    }
}
