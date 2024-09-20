using System.Collections;
using System.Collections.Frozen;

namespace PerformanceOptimization.Mapper.TwoArrays.Tests;

public class Tests
{
    private string _input = null!;
    private Output[] _output = null!;
    private readonly Generator _generator = new();

    [SetUp]
    public void Setup()
    {
        (_input, _output) = _generator.GetInputString(100);
    }

    [Test]
    public void OriginalMappingIsCorrect()
    {
        //Arrange, Act
        var outputs = Original.Map(_input);

        CollectionAssert.AreEquivalent(outputs, _output);
    }

    [Test]
    public void OptimizedMappingIsCorrect()
    {
        //Arrange, Act
        var optimized = Optimized.Map(_input);

        //Assert
        CollectionAssert.AreEquivalent(optimized, _output);
    }

    [Test]
    public void OptimizedStructMappingIsCorrect()
    {
        //Arrange, Act
        var optimized = OptimizedStruct.Map(_input);

        var convertedToClass = optimized
            .Select(st => new Output { Date = st.Date, Value = st.Value, Season = st.Season, State = st.State })
            .ToArray();
        var convertedToStruct = _output
            .Select(o => new OutputSt { Date = o.Date, Value = o.Value, Season = o.Season, State = o.State })
            .ToArray();

        //Assert
        CollectionAssert.AreEquivalent(convertedToClass, _output);
        CollectionAssert.AreEquivalent(convertedToStruct, optimized);
    }


    [Test]
    public void OptimizedStructMarshalMappingIsCorrect()
    {
        //Arrange, Act
        var optimized = OptimizedStructMarshal
            .Map(_input)
            .ToFrozenDictionary(o => o.Date, o => o);

        //Assert
        foreach (var output in _output)
        {
            Assert.Multiple(() =>
            {
                Assert.That(optimized.TryGetValue(output.Date, out var outputSt));
                
                Assert.That(output.Value, Is.EqualTo(outputSt.Value));
                Assert.That(output.State, Is.EqualTo(outputSt.State));
                Assert.That(output.Season, Is.EqualTo(outputSt.Season));
            });
        }
    }
}