using System.Globalization;

namespace AgenteFinanceiro.Domain.Finance;

public static class MoneyText
{
    public static string Format(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
