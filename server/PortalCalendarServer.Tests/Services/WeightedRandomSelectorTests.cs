using PortalCalendarServer.Services;

namespace PortalCalendarServer.Tests.Services;

public class WeightedRandomSelectorTests
{
    private record WeightedItem(string Name, double Weight);

    [Fact]
    public void Select_EmptyList_ReturnsNull()
    {
        var result = WeightedRandomSelector.Select(Array.Empty<WeightedItem>(), i => i.Weight, new Random(42));
        Assert.Null(result);
    }

    [Fact]
    public void Select_SingleItem_ReturnsThatItem()
    {
        var items = new[] { new WeightedItem("A", 1.0) };
        var result = WeightedRandomSelector.Select(items, i => i.Weight, new Random(42));
        Assert.Equal("A", result!.Name);
    }

    [Fact]
    public void Select_AllZeroWeights_ReturnsNull()
    {
        var items = new[]
        {
            new WeightedItem("A", 0.0),
            new WeightedItem("B", 0.0),
        };
        var result = WeightedRandomSelector.Select(items, i => i.Weight, new Random(42));
        Assert.Null(result);
    }

    [Fact]
    public void Select_AllNegativeWeights_ReturnsNull()
    {
        var items = new[]
        {
            new WeightedItem("A", -1.0),
            new WeightedItem("B", -5.0),
        };
        var result = WeightedRandomSelector.Select(items, i => i.Weight, new Random(42));
        Assert.Null(result);
    }

    [Fact]
    public void Select_MixedZeroAndPositiveWeights_NeverSelectsZero()
    {
        var items = new[]
        {
            new WeightedItem("Zero", 0.0),
            new WeightedItem("Positive", 3.0),
        };

        for (int seed = 0; seed < 100; seed++)
        {
            var result = WeightedRandomSelector.Select(items, i => i.Weight, new Random(seed));
            Assert.Equal("Positive", result!.Name);
        }
    }

    [Fact]
    public void Select_OneItemWithZeroWeight_OnlySelectsOther()
    {
        var items = new[]
        {
            new WeightedItem("A", 0.0),
            new WeightedItem("B", 1.0),
            new WeightedItem("C", 0.0),
        };

        for (int seed = 0; seed < 100; seed++)
        {
            var result = WeightedRandomSelector.Select(items, i => i.Weight, new Random(seed));
            Assert.Equal("B", result!.Name);
        }
    }

    [Fact]
    public void Select_EqualWeights_DistributesEvenly()
    {
        var items = new[]
        {
            new WeightedItem("A", 1.0),
            new WeightedItem("B", 1.0),
        };

        var counts = new Dictionary<string, int> { ["A"] = 0, ["B"] = 0 };
        const int iterations = 1000;

        for (int seed = 0; seed < iterations; seed++)
        {
            var result = WeightedRandomSelector.Select(items, i => i.Weight, new Random(seed));
            counts[result!.Name]++;
        }

        // With equal weights, expect ~50% each. Allow 5% tolerance.
        Assert.InRange(counts["A"], 450, 550);
        Assert.InRange(counts["B"], 450, 550);
    }

    [Fact]
    public void Select_3to1Ratio_DistributesAccordingly()
    {
        var items = new[]
        {
            new WeightedItem("Heavy", 3.0),
            new WeightedItem("Light", 1.0),
        };

        var counts = new Dictionary<string, int> { ["Heavy"] = 0, ["Light"] = 0 };
        const int iterations = 1000;

        for (int seed = 0; seed < iterations; seed++)
        {
            var result = WeightedRandomSelector.Select(items, i => i.Weight, new Random(seed));
            counts[result!.Name]++;
        }

        // Expect ~75% Heavy, ~25% Light. Allow reasonable tolerance.
        var heavyRatio = (double)counts["Heavy"] / iterations;
        Assert.InRange(heavyRatio, 0.70, 0.80);
    }

    [Fact]
    public void Select_ThreeItemsWithWeights_DistributesAccordingly()
    {
        var items = new[]
        {
            new WeightedItem("A", 1.0),
            new WeightedItem("B", 2.0),
            new WeightedItem("C", 7.0),
        };

        var counts = new Dictionary<string, int> { ["A"] = 0, ["B"] = 0, ["C"] = 0 };
        const int iterations = 1000;

        for (int seed = 0; seed < iterations; seed++)
        {
            var result = WeightedRandomSelector.Select(items, i => i.Weight, new Random(seed));
            counts[result!.Name]++;
        }

        // Expected: A ~10%, B ~20%, C ~70%
        var ratioA = (double)counts["A"] / iterations;
        var ratioB = (double)counts["B"] / iterations;
        var ratioC = (double)counts["C"] / iterations;

        Assert.InRange(ratioA, 0.05, 0.15);
        Assert.InRange(ratioB, 0.15, 0.25);
        Assert.InRange(ratioC, 0.65, 0.75);
    }

    [Fact]
    public void Select_VerySmallWeight_StillGetsPicked()
    {
        var items = new[]
        {
            new WeightedItem("Big", 999.0),
            new WeightedItem("Tiny", 1.0),
        };

        var tinyCount = 0;
        const int iterations = 10000;

        for (int seed = 0; seed < iterations; seed++)
        {
            var result = WeightedRandomSelector.Select(items, i => i.Weight, new Random(seed));
            if (result!.Name == "Tiny") tinyCount++;
        }

        // Tiny should be picked ~0.1% of the time. At least once in 10000 tries.
        Assert.True(tinyCount > 0, "Item with small weight was never selected in 10000 iterations");
    }

    [Fact]
    public void Select_DeterministicWithSameSeed()
    {
        var items = new[]
        {
            new WeightedItem("A", 1.0),
            new WeightedItem("B", 2.0),
            new WeightedItem("C", 3.0),
        };

        var result1 = WeightedRandomSelector.Select(items, i => i.Weight, new Random(12345));
        var result2 = WeightedRandomSelector.Select(items, i => i.Weight, new Random(12345));

        Assert.Equal(result1!.Name, result2!.Name);
    }
}
