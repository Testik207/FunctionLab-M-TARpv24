namespace FunctionLab.Services;
public static class ErrorLocalization
{
    public static string Estonian(string ru)
    {
        return ru switch {
            "Выберите интервал внутри [−100; 100], длиной от 0.1."=>"Vali vahemik hulgas [−100; 100], pikkusega vähemalt 0.1.",
            "x₀ должен быть внутри [−100; 100]."=>"x₀ peab jääma vahemikku [−100; 100].",
            "Неизвестная прикладная задача."=>"Tundmatu rakendusülesanne.",
            "LaTeX не должен превышать 1000 символов."=>"LaTeX ei tohi olla pikem kui 1000 märki.",
            "Формула обязательна, максимум 400 символов."=>"Valem on kohustuslik, kuni 400 märki.",
            "Используйте x, числа, + − * / ^ и sin, cos, tan, ln, log, sqrt, exp. Десятичный разделитель — точка."=>"Kasuta x-i, arve, + − * / ^ ning sin, cos, tan, ln, log, sqrt, exp. Kümnenderaldaja on punkt.",
            "Не хватает закрывающей скобки."=>"Puudub sulgev sulg.",
            "Формула не завершена."=>"Valem on lõpetamata.",
            "Слишком сложная формула."=>"Valem on liiga keeruline.",
            "Слишком глубокая формула: максимум 24 уровня операций."=>"Valem on liiga sügav: kuni 24 tehetasandit.",
            "Модуль числа не должен превышать 10¹²."=>"Arvu absoluutväärtus ei tohi ületada 10¹².",
            "Модуль постоянной степени не должен превышать 20."=>"Konstantse astendaja absoluutväärtus ei tohi ületada 20.",
            "Слишком большие коэффициенты полинома."=>"Polünoomi kordajad on liiga suured.",
            "Корни полинома могут быть слишком далеко: уменьшите масштаб коэффициентов (граница поиска 10⁶)."=>"Polünoomi nullkohad võivad olla liiga kaugel: vähenda kordajate skaalat (otsingupiir 10⁶).",
            _ when ru.StartsWith("Неизвестный символ: ")=>"Tundmatu sümbol: "+ru[19..],
            _ when ru.StartsWith("Неожиданный символ: ")=>"Ootamatu sümbol: "+ru[19..],
            _ when ru.StartsWith("Аргумент ")=>"Funktsiooni argument peab olema sulgudes.",
            _=>"Sisendit ei saanud töödelda. Kontrolli valemi süntaksit ja arvutusvahemikku."
        };
    }
}
