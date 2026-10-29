using FluentAssertions;
using Xunit;

namespace ShipManagement.Tests.DomainRuleTests;

public class FiscalYearCalculationTests
{
    public static string GetFiscalYearStartPeriod(string fiscalYearCode, string targetPeriod)
    {
        int targetYear = int.Parse(targetPeriod.Substring(0, 4));
        int targetMonth = int.Parse(targetPeriod.Substring(5, 2));
        int startMonth = int.Parse(fiscalYearCode.Substring(0, 2));

        int startYear;
        if (startMonth == 1)
        {
            startYear = targetYear;
        }
        else
        {
            startYear = targetMonth >= startMonth ? targetYear : targetYear - 1;
        }

        return $"{startYear:D4}-{startMonth:D2}";
    }

    public static List<string> GetFiscalYearPeriods(string fiscalYearCode, string targetPeriod)
    {
        var startPeriod = GetFiscalYearStartPeriod(fiscalYearCode, targetPeriod);
        var startDate = DateTime.Parse(startPeriod + "-01");
        var targetDate = DateTime.Parse(targetPeriod + "-01");

        var periods = new List<string>();
        var curr = startDate;
        while (curr <= targetDate)
        {
            periods.Add(curr.ToString("yyyy-MM"));
            curr = curr.AddMonths(1);
        }
        return periods;
    }

    [Theory]
    [InlineData("0112", "2025-01", "2025-01")]
    [InlineData("0112", "2025-07", "2025-01")]
    [InlineData("0112", "2025-12", "2025-01")]
    [InlineData("0403", "2025-04", "2025-04")] // Month >= 4: same year
    [InlineData("0403", "2025-07", "2025-04")] // Month >= 4: same year
    [InlineData("0403", "2025-01", "2024-04")] // Month < 4: previous year start
    [InlineData("0403", "2025-03", "2024-04")] // Month < 4: previous year start
    [InlineData("0706", "2025-09", "2025-07")] // Month >= 7: same year
    [InlineData("0706", "2025-05", "2024-07")] // Month < 7: previous year start
    public void FiscalYearStart_ShouldBeCalculatedCorrectly(string fyCode, string period, string expectedStart)
    {
        var result = GetFiscalYearStartPeriod(fyCode, period);
        result.Should().Be(expectedStart);
    }

    [Fact]
    public void CalendarFiscalYear_0112_ForJuly_ShouldHave7Periods()
    {
        var periods = GetFiscalYearPeriods("0112", "2025-07");
        periods.Should().HaveCount(7);
        periods.Should().ContainInOrder("2025-01", "2025-02", "2025-03", "2025-04", "2025-05", "2025-06", "2025-07");
    }

    [Fact]
    public void CrossCalendarFiscalYear_0403_ForJuly_ShouldHave4Periods()
    {
        var periods = GetFiscalYearPeriods("0403", "2025-07");
        periods.Should().HaveCount(4);
        periods.Should().ContainInOrder("2025-04", "2025-05", "2025-06", "2025-07");
    }

    [Fact]
    public void CrossCalendarFiscalYear_0403_ForFebruary_ShouldHave11Periods()
    {
        var periods = GetFiscalYearPeriods("0403", "2025-02");
        periods.Should().HaveCount(11);
        periods.First().Should().Be("2024-04");
        periods.Last().Should().Be("2025-02");
    }
}

public class CrewStatusCalculationTests
{
    public static string DetermineCrewStatus(DateTime signOnDate, DateTime? signOffDate, DateTime endOfContractDate, DateTime asOfDate)
    {
        if (signOffDate.HasValue)
            return "Signed Off";

        if (signOnDate > asOfDate)
            return "Planned";

        if ((asOfDate - endOfContractDate).TotalDays > 30)
            return "Relief Due";

        return "Onboard";
    }

    [Fact]
    public void WhenSignOffDateIsSet_StatusShouldBeSignedOff()
    {
        var asOf = new DateTime(2026, 10, 28);
        var signOn = new DateTime(2026, 1, 1);
        var signOff = new DateTime(2026, 6, 30);
        var endOfContract = new DateTime(2026, 6, 30);

        var status = DetermineCrewStatus(signOn, signOff, endOfContract, asOf);
        status.Should().Be("Signed Off");
    }

    [Fact]
    public void WhenSignOnDateIsInFuture_StatusShouldBePlanned()
    {
        var asOf = new DateTime(2026, 10, 28);
        var signOn = new DateTime(2026, 11, 15);
        var endOfContract = new DateTime(2027, 5, 15);

        var status = DetermineCrewStatus(signOn, null, endOfContract, asOf);
        status.Should().Be("Planned");
    }

    [Fact]
    public void WhenContractEndsInFuture_StatusShouldBeOnboard()
    {
        var asOf = new DateTime(2026, 10, 28);
        var signOn = new DateTime(2026, 8, 1);
        var endOfContract = new DateTime(2026, 12, 1);

        var status = DetermineCrewStatus(signOn, null, endOfContract, asOf);
        status.Should().Be("Onboard");
    }

    [Fact]
    public void WhenContractEndedMoreThan30DaysAgo_StatusShouldBeReliefDue()
    {
        var asOf = new DateTime(2026, 10, 28);
        var signOn = new DateTime(2026, 1, 1);
        var endOfContract = new DateTime(2026, 8, 1); // 88 days ago

        var status = DetermineCrewStatus(signOn, null, endOfContract, asOf);
        status.Should().Be("Relief Due");
    }

    [Fact]
    public void WhenContractEnded15DaysAgo_StatusShouldBeOnboardGracePeriod()
    {
        var asOf = new DateTime(2026, 10, 28);
        var signOn = new DateTime(2026, 4, 1);
        var endOfContract = new DateTime(2026, 10, 13); // 15 days ago, <= 30 days

        var status = DetermineCrewStatus(signOn, null, endOfContract, asOf);
        status.Should().Be("Onboard");
    }
}

public class AgeCalculationTests
{
    public static int CalculateAge(DateTime birthDate, DateTime asOfDate)
    {
        int age = asOfDate.Year - birthDate.Year;
        if (birthDate.Date > asOfDate.AddYears(-age))
            age--;
        return age;
    }

    [Fact]
    public void BirthdayHasPassedThisYear_ShouldReturnFullYears()
    {
        var birthDate = new DateTime(1980, 5, 10);
        var asOf = new DateTime(2026, 10, 28);

        CalculateAge(birthDate, asOf).Should().Be(46);
    }

    [Fact]
    public void BirthdayNotYetReachedThisYear_ShouldReturnFullYearsMinusOne()
    {
        var birthDate = new DateTime(1980, 12, 25);
        var asOf = new DateTime(2026, 10, 28);

        CalculateAge(birthDate, asOf).Should().Be(45);
    }

    [Fact]
    public void BirthdayToday_ShouldReturnFullYears()
    {
        var birthDate = new DateTime(1980, 10, 28);
        var asOf = new DateTime(2026, 10, 28);

        CalculateAge(birthDate, asOf).Should().Be(46);
    }
}
