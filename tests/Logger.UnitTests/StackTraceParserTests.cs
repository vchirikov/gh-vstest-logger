
namespace GitHub.VsTest.Logger.UnitTests;
public class StackTraceParserTests
{
    [Fact]
    public void ParseAndNormalize_Should_Return_RelativePath()
    {
        const string stackTrace = @"System.Collections.Generic.KeyNotFoundException : Unable to find price for ServiceId=1, Quantity=1
   at SomeCompany.SomeNamespace.Tests.AccountTests.BillForService_Works_ForPlatformServices() in /__NOT_FROM_WORKSPACE__/src/tests/Domain.Tests/AccountTests.cs:line 87
   at SomeCompany.SomeNamespace.Plan.GetPriceTier(Service service, Int64 quantity) in /src/src/Domain/Plan.cs:line 31
   at SomeCompany.SomeNamespace.BillingPeriod.BillForPlatformService(PriceList priceList, ServiceRecord serviceRecord)+MoveNext() in /src/src/Domain/BillingPeriod.cs:line 71
   at SomeCompany.SomeNamespace.Tests.AccountTests.BillForService_Works_ForPlatformServices() in /__NOT_FROM_WORKSPACE__/src/tests/Domain.Tests/AccountTests.cs:line 87
   at System.Collections.Generic.LargeArrayBuilder`1.AddRange(IEnumerable`1 items)
   at System.Collections.Generic.EnumerableHelpers.ToArray[T](IEnumerable`1 source)
   at System.Linq.Enumerable.ToArray[TSource](IEnumerable`1 source)
   at SomeCompany.SomeNamespace.Account.BillForService(PriceList priceList, BillingPeriod period, ServiceRecord serviceRecord) in /src/src/Domain/Account.cs:line 79
   at SomeCompany.SomeNamespace.Tests.AccountTests.BillForService_Works_ForPlatformServices() in /src/tests/Domain.Tests/AccountTests.cs:line 87

";
        var parser = new StackTraceParser(workspacePath: "/src");
        var result = parser.ParseAndNormalize(stackTrace);

        var outFromWorkspace = result.FirstOrDefault();
        var projectFile = result.FirstOrDefault(x => x.File.StartsWith("src/", StringComparison.Ordinal));

        Assert.Equal("/__NOT_FROM_WORKSPACE__/src/tests/Domain.Tests/AccountTests.cs", outFromWorkspace?.File);
        Assert.Equal("src/Domain/Plan.cs", projectFile?.File);
    }
}