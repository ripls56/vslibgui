using System;

namespace Gui.Widgets.Framework;

public class ListenableBuilder : StatefulWidget
{
    public ListenableBuilder(
        IListenable listenable,
        Func<BuildContext, Widget> builder
    )
    {
        Listenable = listenable;
        Builder = builder;
    }

    public IListenable Listenable { get; }
    public Func<BuildContext, Widget> Builder { get; }

    public override State CreateState() => new ListenableBuilderState();

    private class ListenableBuilderState : State<ListenableBuilder>
    {
        public override void InitState()
        {
            base.InitState();
            Widget.Listenable.AddListener(HandleTick);
        }

        private void HandleTick() => SetState(() => { });

        public override Widget Build(
            BuildContext context
        ) =>
            Widget.Builder(context);

        public override void Dispose()
        {
            Widget.Listenable.RemoveListener(HandleTick);
            base.Dispose();
        }
    }
}
