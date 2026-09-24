using SchoolPlatform.Application.Timetabling;

namespace SchoolPlatform.UnitTests;

public sealed class ParallelTimetableCapacityTests
{
    [Fact]
    public void NoGroupsKeepRawCapacity()
    {
        var result = ParallelTimetableCapacity.Calculate(
            Periods(Enumerable.Repeat(3, 14).ToArray()),
            Array.Empty<IReadOnlyCollection<Guid>>());

        Assert.Equal(42, result.Raw);
        Assert.Equal(42, result.Effective);
        Assert.Equal(0, result.Savings);
    }

    [Fact]
    public void ThreeIndependentPairsSaveNineSlots()
    {
        var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        var result = ParallelTimetableCapacity.Calculate(
            ids.ToDictionary(id => id, _ => 3),
            new IReadOnlyCollection<Guid>[]
            {
                new[] { ids[0], ids[1] },
                new[] { ids[2], ids[3] },
                new[] { ids[4], ids[5] },
            });

        Assert.Equal(18, result.Raw);
        Assert.Equal(9, result.Effective);
        Assert.Equal(9, result.Savings);
    }

    [Fact]
    public void OneThreePeriodPairSavesThreeSlots()
    {
        var ids = Enumerable.Range(0, 2).Select(_ => Guid.NewGuid()).ToArray();
        var result = ParallelTimetableCapacity.Calculate(
            new Dictionary<Guid, int>
            {
                [ids[0]] = 3,
                [ids[1]] = 3,
            },
            new IReadOnlyCollection<Guid>[] { ids });

        Assert.Equal(6, result.Raw);
        Assert.Equal(3, result.Effective);
        Assert.Equal(3, result.Savings);
    }

    [Fact]
    public void ThreeMemberGroupConsumesOneSharedRequirement()
    {
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        var result = ParallelTimetableCapacity.Calculate(
            ids.ToDictionary(id => id, _ => 3),
            new IReadOnlyCollection<Guid>[] { ids });

        Assert.Equal(9, result.Raw);
        Assert.Equal(3, result.Effective);
        Assert.Equal(6, result.Savings);
    }

    [Fact]
    public void UnequalGroupRemainsIndependent()
    {
        var ids = Enumerable.Range(0, 2).Select(_ => Guid.NewGuid()).ToArray();
        var result = ParallelTimetableCapacity.Calculate(
            new Dictionary<Guid, int> { [ids[0]] = 3, [ids[1]] = 4 },
            new IReadOnlyCollection<Guid>[] { ids });

        Assert.Equal(7, result.Raw);
        Assert.Equal(7, result.Effective);
        Assert.Equal(0, result.Savings);
    }

    private static IReadOnlyDictionary<Guid, int> Periods(params int[] values)
    {
        return values.ToDictionary(_ => Guid.NewGuid(), value => value);
    }
}
