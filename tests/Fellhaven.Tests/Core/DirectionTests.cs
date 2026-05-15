using Fellhaven.Core;
using NUnit.Framework;

namespace Fellhaven.Tests.Core;

[TestFixture]
public class DirectionTests
{
    [Test]
    public void GetOpposite_ReturnsCorrectOpposite()
    {
        Assert.That(Direction.North.GetOpposite(), Is.EqualTo(Direction.South));
        Assert.That(Direction.East.GetOpposite(), Is.EqualTo(Direction.West));
        Assert.That(Direction.NorthEast.GetOpposite(), Is.EqualTo(Direction.SouthWest));
        Assert.That(Direction.Up.GetOpposite(), Is.EqualTo(Direction.Down));
    }

    [Test]
    public void ToShortString_ReturnsCorrectAbbreviation()
    {
        Assert.That(Direction.North.ToShortString(), Is.EqualTo("N"));
        Assert.That(Direction.NorthEast.ToShortString(), Is.EqualTo("NE"));
        Assert.That(Direction.Up.ToShortString(), Is.EqualTo("U"));
    }
}
