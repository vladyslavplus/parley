namespace Parley.Campaigns.Domain.Holdout;

/// <summary>The complementary error function, used for the p-values of the sample ratio check.</summary>
public static class ErrorFunction
{
    /// <summary>
    /// Chebyshev fit of erfc (Numerical Recipes "erfcc"), fractional error below 1.2e-7. The coefficients belong to
    /// that published approximation; the polynomial is evaluated by Horner's rule.
    /// </summary>
    private static readonly double[] FitCoefficients =
    [
        -1.26551223,
        1.00002368,
        0.37409196,
        0.09678418,
        -0.18628806,
        0.27886807,
        -1.13520398,
        1.48851587,
        -0.82215223,
        0.17087277,
    ];

    private const double HalfFactor = 0.5;

    public static double Erfc(double x)
    {
        var magnitude = Math.Abs(x);
        var t = 1 / (1 + HalfFactor * magnitude);

        var polynomial = 0d;
        for (var index = FitCoefficients.Length - 1; index >= 0; index--)
        {
            polynomial = FitCoefficients[index] + t * polynomial;
        }

        var result = t * Math.Exp(-magnitude * magnitude + polynomial);
        return x >= 0 ? result : 2 - result;
    }
}
