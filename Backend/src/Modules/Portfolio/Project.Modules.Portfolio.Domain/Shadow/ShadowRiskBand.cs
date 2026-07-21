using Project.Modules.Portfolio.Domain.Portfolios;

namespace Project.Modules.Portfolio.Domain.Shadow;

/// <summary>
/// The representative investor a template's shadow portfolio stands in for (§ 6.1).
/// A shadow portfolio has no real profile, so it is optimized for the band at the
/// midpoint of the template's own [RiskMin, RiskMax] range — using the same band
/// thresholds as the Phase-2 scoring engine (&lt;40 Conservative, 40–69 Moderate,
/// ≥70 Aggressive).
/// </summary>
public static class ShadowRiskBand
{
    public static RiskProfile ForTemplate(int riskMin, int riskMax)
    {
        int midpoint = (riskMin + riskMax) / 2;
        return midpoint switch
        {
            < 40 => RiskProfile.Conservative,
            < 70 => RiskProfile.Moderate,
            _ => RiskProfile.Aggressive,
        };
    }
}
