using Gui.Tests.Helpers;
using Gui.Widgets.Basic;
using Gui.Widgets.Framework;
using SkiaSharp;

namespace Gui.Tests.Framework;

[TestFixture, NonParallelizable]
public class ErrorBoundaryTests
{
    private BuildOwner _owner = null!;
    private Exception? _capturedError;

    [SetUp]
    public void SetUp()
    {
        _owner = TestHelpers.NewBuildOwner();
        _capturedError = null;
        _owner.OnError = (_, ex) => _capturedError = ex;
    }

    [Test]
    public void ThrowingChild_OnErrorCalledAndBoundaryRendersError()
    {
        TriggerableState? state = null;
        var widget = new ErrorBoundary { Child = new TriggerableWidget(s => state = s) };
        var element = widget.CreateElement();
        element.AssignOwner(_owner);
        element.Mount(null);
        _owner.BuildDirtyElements();

        state!.TriggerThrow();
        _owner.BuildDirtyElements();

        Assert.That(_capturedError, Is.Not.Null);
        Assert.That(element.RenderObject, Is.Not.Null);
    }

    [Test]
    public void ThrowingBoundary_DoesNotAffectSiblingBoundary()
    {
        TriggerableState? state = null;
        var el1 = new ErrorBoundary { Child = new TriggerableWidget(s => state = s) }
            .CreateElement();
        el1.AssignOwner(_owner);
        el1.Mount(null);

        var el2 = new ErrorBoundary { Child = new Container() }
            .CreateElement();
        el2.AssignOwner(_owner);
        el2.Mount(null);

        _owner.BuildDirtyElements();

        state!.TriggerThrow();
        _owner.BuildDirtyElements();

        Assert.That(_capturedError, Is.Not.Null);
        Assert.That(el2.RenderObject, Is.Not.Null);
    }

    [Test]
    public void ErrorBoundary_LocalizesError_OuterBoundaryUnaffected()
    {
        TriggerableState? state = null;
        var outerFallbackCalled = false;

        var outer = new ErrorBoundary
        {
            Child = new ErrorBoundary
            {
                Child = new TriggerableWidget(s => state = s),
                FallbackBuilder = _ => new Container()
            },
            FallbackBuilder = _ =>
            {
                outerFallbackCalled = true;
                return new Container();
            }
        };

        var element = outer.CreateElement();
        element.AssignOwner(_owner);
        element.Mount(null);
        _owner.BuildDirtyElements();

        state!.TriggerThrow();
        _owner.BuildDirtyElements();

        Assert.That(_capturedError, Is.Not.Null);
        Assert.That(outerFallbackCalled, Is.False);
    }

    [Test]
    public void ErrorBoundary_ClearError_RecoverSuccessfully()
    {
        TriggerableState? state = null;
        var widget = new ErrorBoundary { Child = new TriggerableWidget(s => state = s) };
        var element = widget.CreateElement();
        element.AssignOwner(_owner);
        element.Mount(null);
        _owner.BuildDirtyElements();

        state!.TriggerThrow();
        _owner.BuildDirtyElements();
        Assert.That(_capturedError, Is.Not.Null);

        var boundaryState = ((StatefulElement)element).State as ErrorBoundaryState;
        boundaryState!.ClearError();
        _owner.BuildDirtyElements();

        Assert.That(element.RenderObject, Is.Not.Null);
    }

    [Test]
    public void DefaultErrorWidget_Golden()
    {
        GoldenAssert.Matches(
            ErrorWidget.Builder(new InvalidOperationException("test error")),
            "error_widget_default",
            new SKSizeI(300, 80));
    }

    private sealed class TriggerableWidget : StatefulWidget
    {
        private readonly Action<TriggerableState> _onCreated;

        public TriggerableWidget(
            Action<TriggerableState> onCreated
        ) =>
            _onCreated = onCreated;

        public override State CreateState()
        {
            var s = new TriggerableState();
            _onCreated(s);
            return s;
        }
    }

    private sealed class TriggerableState : State<TriggerableWidget>
    {
        public bool ShouldThrow { get; set; }

        public void TriggerThrow() => SetState(() => ShouldThrow = true);

        public override Widget Build(
            BuildContext context
        )
        {
            if (ShouldThrow)
            {
                throw new InvalidOperationException("deliberate test error");
            }

            return new Container();
        }
    }
}
