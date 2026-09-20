using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class CpuManagerCatalog_Tests
{
    [Fact]
    public void TheDefault_IsTheEfficiencyCallsCpu_AndComesFirst()
    {
        Assert.Equal(typeof(EfficiencyCallsCpuManagerPivot), CpuManagerCatalog.Default.Type);
        Assert.Same(CpuManagerCatalog.Default, CpuManagerCatalog.Implementations[0]);
    }

    [Fact]
    public void CreateDefault_BuildsTheCpuOfTheDefaultOption()
    {
        var round = RoundSetup.NewRound(1);

        Assert.IsType(CpuManagerCatalog.Default.Type, CpuManagerCatalog.CreateDefault(round));
    }

    [Fact]
    public void TheBasicCpu_StaysInTheCatalogAsAVariant()
    {
        Assert.Contains(CpuManagerCatalog.Implementations, o => o.Type == typeof(BasicCpuManagerPivot));
    }

    [Fact]
    public void EveryOption_IsACpuWithItsOwnTypeAndName()
    {
        Assert.All(CpuManagerCatalog.Implementations, o => Assert.True(o.Type.IsAssignableTo(typeof(CpuManagerBasePivot))));
        Assert.Equal(CpuManagerCatalog.Implementations.Count, CpuManagerCatalog.Implementations.Select(o => o.Type).Distinct().Count());
        Assert.Equal(CpuManagerCatalog.Implementations.Count, CpuManagerCatalog.Implementations.Select(o => o.DisplayName).Distinct().Count());
    }
}
