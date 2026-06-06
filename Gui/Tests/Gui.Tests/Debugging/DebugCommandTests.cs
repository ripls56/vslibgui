using Gui.Debugging;

namespace Gui.Tests.Debugging;

public class DebugCommandTests
{
    private DebugCommandProcessor _processor;
    private DebugSettings _settings;

    [SetUp]
    public void Setup()
    {
        _settings = new DebugSettings();
        _processor = new DebugCommandProcessor(_settings);
    }

    [TearDown]
    public void TearDown() => _settings.Dispose();

    [Test]
    [TestCase("tree", true)]
    [TestCase("bounds", true)]
    [TestCase("paint", true)]
    [TestCase("hud", true)]
    public void Should_Toggle_Specific_Setting(string subCommand, bool expected)
    {
        _processor.Process(subCommand);

        var actual = subCommand switch
        {
            "tree" => _settings.ShowTree,
            "bounds" => _settings.ShowBounds,
            "paint" => _settings.ShowPaint,
            "hud" => _settings.ShowHud,
            _ => false
        };

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Should_Toggle_Violations()
    {
        _processor.Process("violations");
        Assert.That(_settings.ShowViolations, Is.True);

        _processor.Process("violations");
        Assert.That(_settings.ShowViolations, Is.False);
    }

    [Test]
    public void Should_Toggle_HeatMap()
    {
        _processor.Process("heatmap");
        Assert.That(_settings.ShowHeatMap, Is.True);

        _processor.Process("heatmap");
        Assert.That(_settings.ShowHeatMap, Is.False);
    }

    [Test]
    public void Flash_Command_UpdatesFlashDuration()
    {
        _processor.Process("flash", "1000");
        Assert.That(_settings.FlashDurationMs, Is.EqualTo(1000.0));
    }

    [Test]
    public void Flash_Command_IgnoresInvalidArg()
    {
        var original = _settings.FlashDurationMs;
        _processor.Process("flash", "notanumber");
        Assert.That(_settings.FlashDurationMs, Is.EqualTo(original));
    }

    [Test]
    public void Flash_Command_IgnoresNegativeArg()
    {
        var original = _settings.FlashDurationMs;
        _processor.Process("flash", "-100");
        Assert.That(_settings.FlashDurationMs, Is.EqualTo(original));
    }

    [Test]
    public void Should_Toggle_DebugAll()
    {
        _processor.Process("debugall");
        Assert.That(_settings.ShowTree, Is.True);
        Assert.That(_settings.ShowBounds, Is.True);
        Assert.That(_settings.ShowPaint, Is.True);
        Assert.That(_settings.ShowHud, Is.True);
        Assert.That(_settings.ShowViolations, Is.True);
        Assert.That(_settings.ShowHeatMap, Is.True);

        _processor.Process("debugall");
        Assert.That(_settings.IsAnyEnabled, Is.False);
    }

    [Test]
    public void Unknown_Command_ReturnsErrorMessage()
    {
        var result = _processor.Process("notacommand");
        Assert.That(result, Does.Contain("Unknown"));
    }
}
