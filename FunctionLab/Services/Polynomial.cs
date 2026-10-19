using System.Globalization;
namespace FunctionLab.Services;
public static class Polynomial
{
    public static double[]? From(Expr e)
    {
        if(e.Op=="number") return [e.Value!.Value]; if(e.Op=="x") return [0,1];
        var a=e.A is null ? null : From(e.A); var b=e.B is null ? null : From(e.B);
        if(a is null) return null;
        if(e.Op=="neg") return a.Select(x=>-x).ToArray();
        if(b is null) return null;
        if(e.Op is "+" or "-") {
            var c=new double[Math.Max(a.Length,b.Length)]; for(int i=0;i<c.Length;i++) c[i]=(i<a.Length?a[i]:0)+(e.Op=="+"?1:-1)*(i<b.Length?b[i]:0); return Trim(c);
        }
        if(e.Op=="*") return Multiply(a,b);
        if(e.Op=="/" && b.Length==1 && b[0]!=0) return a.Select(x=>x/b[0]).ToArray();
        if(e.Op=="^" && b.Length==1 && b[0]>=0 && b[0]<=12 && b[0]%1==0) {
            double[]? c=[1]; for(int i=0;i<b[0];i++) {c=Multiply(c!,a);if(c is null)return null;} return c;
        }
        return null;
    }
    static double[]? Multiply(double[] a,double[] b) {
        if(a.Length+b.Length-2>12)return null;
        var c=new double[a.Length+b.Length-1];for(int i=0;i<a.Length;i++)for(int j=0;j<b.Length;j++)c[i+j]+=a[i]*b[j];return Trim(c);
    }
    public static double[] Trim(double[] a) { var n=a.Length;while(n>1 && a[n-1]==0)n--;return a[..n]; }
    public static double Eval(double[] a,double x) {double y=0;for(int i=a.Length-1;i>=0;i--)y=y*x+a[i];return y;}
    public static double[] Derivative(double[] a) => a.Length==1 ? [0] : a.Skip(1).Select((v,i)=>v*(i+1)).ToArray();
    public static double Bound(double[] a) => a.Length<=1?1:1+a[..^1].Max(v=>Math.Abs(v/a[^1]));
    public static List<double> Roots(double[] source)
    {
        var a=Trim(source); if(a.Length<=1)return [];
        if(a.Length==2)return [-a[0]/a[1]];
        var bound=Bound(a); if(!double.IsFinite(bound) || bound>1e6) throw new ArgumentException("Корни полинома могут быть слишком далеко: уменьшите масштаб коэффициентов (граница поиска 10⁶).");
        var stationary=Roots(Derivative(a)).Where(x=>x>-bound && x<bound).Order().ToList();
        var cuts=new List<double>{-bound};cuts.AddRange(stationary);cuts.Add(bound);var roots=new List<double>();
        // Derivative roots split a polynomial into monotone intervals, retaining even multiplicities.
        foreach(var x in stationary) if(NearZero(a,x)) Add(roots,x);
        for(int i=0;i<cuts.Count-1;i++) {
            var l=cuts[i];var r=cuts[i+1];var fl=Eval(a,l);var fr=Eval(a,r);
            if(fl==0)Add(roots,l);if(fr==0)Add(roots,r);
            if(Math.Sign(fl)==Math.Sign(fr))continue;
            for(int k=0;k<100;k++){var m=(l+r)/2;var fm=Eval(a,m);if(fm==0){l=r=m;break;}if(Math.Sign(fm)==Math.Sign(fl)){l=m;fl=fm;}else r=m;}
            Add(roots,(l+r)/2);
        }
        return roots.Order().ToList();
    }
    static bool NearZero(double[] a,double x) {
        double scale=0;for(int i=a.Length-1;i>=0;i--)scale=scale*Math.Abs(x)+Math.Abs(a[i]);
        return Math.Abs(Eval(a,x))<=1e-10*scale;
    }
    public static void Add(List<double> list,double x) {if(double.IsFinite(x) && !list.Any(v=>Math.Abs(v-x)<1e-7*Math.Max(1,Math.Abs(x))))list.Add(x);}
    public static string Format(double[] a) {
        var parts=new List<string>();for(int i=a.Length-1;i>=0;i--){var v=a[i];if(v==0)continue;var abs=Math.Abs(v);var term=(i>0 && abs==1?"":abs.ToString("G8",CultureInfo.InvariantCulture)+(i>0?"*":""))+(i==0?"":i==1?"x":$"x^{i}");parts.Add((parts.Count==0?(v<0?"-":""):(v<0?" - ":" + "))+term);}return parts.Count==0?"0":string.Concat(parts);
    }
}
