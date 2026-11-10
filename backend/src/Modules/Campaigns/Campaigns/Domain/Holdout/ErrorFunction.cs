namespace Parley.Campaigns.Domain.Holdout;

/// <summary>
/// The error function family used by the two-sided p-values of the lift statistics. Accurate to about 1e-14 relative,
/// which the committed statistics vectors require. Small arguments use the Maclaurin series of erf; large arguments use
/// the Laplace continued fraction of erfc, which converges fast there.
/// </summary>
public static class ErrorFunction
{
    /// <summary>Arguments at or above this use the continued fraction.</summary>
    private const double ContinuedFractionThreshold = 2d;

    /// <summary>Depth of the continued fraction. Far more than needed for double precision at the threshold.</summary>
    private const int ContinuedFractionDepth = 300;

    /// <summary>Stop once a series term is negligible against the sum.</summary>
    private const double SeriesTolerance = 1e-17;

    private const int SeriesMaximumTerms = 500;

    private static readonly double SqrtPi = Math.Sqrt(Math.PI);

    public static double Erfc(double x)
    {
        if (double.IsNaN(x))
        {
            return double.NaN;
        }

        if (x < 0)
        {
            return 2d - Erfc(-x);
        }

        return x < ContinuedFractionThreshold ? 1d - ErfSeries(x) : ErfcContinuedFraction(x);
    }

    /// <summary>erf(x) = 2 / sqrt(pi) * sum (-1)^n x^(2n+1) / (n! (2n+1)), for 0 &lt;= x &lt; 2.</summary>
    private static double ErfSeries(double x)
    {
        var sum = x;
        var term = x;
        var squared = x * x;
        for (var n = 1; n < SeriesMaximumTerms; n++)
        {
            term *= -squared / n;
            var contribution = term / (2 * n + 1);
            sum += contribution;
            if (Math.Abs(contribution) < SeriesTolerance * Math.Abs(sum))
            {
                break;
            }
        }

        return 2d / SqrtPi * sum;
    }

    /// <summary>erfc(x) = exp(-x^2) / sqrt(pi) / (x + (1/2)/(x + 1/(x + (3/2)/(x + ...)))), evaluated backwards.</summary>
    private static double ErfcContinuedFraction(double x)
    {
        var denominator = x;
        for (var k = ContinuedFractionDepth; k >= 1; k--)
        {
            denominator = x + (k / 2d) / denominator;
        }

        return Math.Exp(-x * x) / (SqrtPi * denominator);
    }
}
