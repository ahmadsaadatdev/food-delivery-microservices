namespace FoodDelivery.ArchitectureTests;

public class SmokeTests
{
    [Fact]
    public void Solution_Targets_Net10()
    {
        var tfm = typeof(FoodDelivery.BuildingBlocks.DependencyInjection).Assembly
            .GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
            .OfType<System.Runtime.Versioning.TargetFrameworkAttribute>()
            .Single()
            .FrameworkName;

        Assert.Contains("net10.0", tfm, StringComparison.OrdinalIgnoreCase);
    }
}
