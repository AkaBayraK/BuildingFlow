using BuildingFlow.Application;
using BuildingFlow.Domain;
using Xunit;
namespace BuildingFlow.UnitTests;
public sealed class PersonCodeTests
{
    [Theory]
    [InlineData("123456")] [InlineData("654321")] [InlineData("901234")] [InlineData("876543")]
    [InlineData("111111")] [InlineData("411172")] [InlineData("121212")] [InlineData("123123")] [InlineData("707077")]
    [InlineData("012345")] [InlineData("12345")] [InlineData("1234567")] [InlineData("12a456")] [InlineData("")]
    public void WeakOrMalformedCodesAreRejected(string code) => Assert.False(PersonCodePolicy.IsAcceptable(code));

    [Theory] [InlineData("482917")] [InlineData("905173")] [InlineData("310846")] [InlineData("772940")]
    public void IrregularSixDigitCodesAreAccepted(string code) => Assert.True(PersonCodePolicy.IsAcceptable(code));

    [Fact] public void GeneratorSkipsWeakCandidatesAndAlwaysStartsWithNonZero()
    {
        var digits = new Queue<int>([1, 2, 3, 4, 5, 6, 4, 8, 2, 9, 1, 7]);
        Assert.Equal("482917", PersonCodePolicy.Generate((_, _) => digits.Dequeue()));
        var calls = new List<(int Min, int Max)>();
        PersonCodePolicy.Generate((min, max) => { calls.Add((min, max)); return new[] { 4, 8, 2, 9, 1, 7 }[calls.Count - 1]; });
        Assert.Equal((1, 10), calls[0]); Assert.All(calls.Skip(1), call => Assert.Equal((0, 10), call));
    }

    [Fact] public void GeneratorFailsInsteadOfReturningAWeakCode() =>
        Assert.Throws<InvalidOperationException>(() => PersonCodePolicy.Generate((min, _) => Math.Max(min, 1)));

    [Fact] public void RandomCodesAreAcceptableAndNotSequential()
    {
        var issuer = new PersonCodeIssuer(new FakeHasher());
        var codes = Enumerable.Range(0, 2000).Select(_ => issuer.Issue()).ToArray();
        Assert.All(codes, code => { Assert.True(PersonCodePolicy.IsAcceptable(code.Code)); Assert.Equal(code.Code[^2..], code.Hint); Assert.Equal("h" + code.Code, code.Hash); });
        Assert.True(codes.Select(code => code.Code).Distinct().Count() > 1990);
        var steps = codes.Zip(codes.Skip(1), (a, b) => int.Parse(b.Code) - int.Parse(a.Code));
        Assert.True(steps.Distinct().Count() > 1900);
    }

    [Fact] public void PersonStoresOnlyHashHintAndIssueTime()
    {
        var person = new Person(Guid.NewGuid(), "Ada", "Kaya"); var at = DateTimeOffset.UtcNow; var hash = new string('a', 64);
        person.AssignCode(hash, "17", at);
        Assert.Equal(hash, person.PersonCodeHash); Assert.Equal("17", person.PersonCodeHint); Assert.Equal(at, person.PersonCodeIssuedAt);
        Assert.Throws<ArgumentException>(() => person.AssignCode(new string('A', 64), "17", at));
        Assert.Throws<ArgumentException>(() => person.AssignCode(hash, "1x", at));
        Assert.Equal(hash, person.PersonCodeHash);
    }

    private sealed class FakeHasher : IPersonCodeHasher { public string Hash(string code) => "h" + code; }
}
