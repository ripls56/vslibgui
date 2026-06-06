using System;
using Gui.Widgets.Framework;

namespace Gui.Widgets.Animations;

public class AnimatedBuilder : StatefulWidget
{
    public AnimatedBuilder(
        AnimationController animation,
        Func<BuildContext, Widget> builder
    )
    {
        Animation = animation;
        Builder = builder;
    }

    public AnimationController Animation { get; }
    public Func<BuildContext, Widget> Builder { get; }

    public override State CreateState() => new AnimatedBuilderState();

    private class AnimatedBuilderState : State<AnimatedBuilder>
    {
        public override void InitState()
        {
            base.InitState();
            Widget.Animation.OnValueChanged += HandleTick;
        }

        private void HandleTick(
            double animationValue
        ) =>
            SetState(() => { });

        public override Widget Build(
            BuildContext context
        ) =>
            Widget.Builder(context);

        public override void Dispose()
        {
            Widget.Animation.OnValueChanged -= HandleTick;
            base.Dispose();
        }
    }
}
