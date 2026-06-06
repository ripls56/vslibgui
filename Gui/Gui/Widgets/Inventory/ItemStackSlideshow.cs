using System;
using System.Collections.Generic;
using Gui.Widgets.Animations;
using Gui.Widgets.Framework;
using Gui.Widgets.Layout;
using Vintagestory.API.Common;

namespace Gui.Widgets.Inventory;

/// <summary>
///     Displays a cycling slideshow of <see cref="ItemStack" /> icons, advancing
///     one element per second. Uses a <see cref="Ticker" /> to accumulate elapsed
///     time and <see cref="SlideshowIndex.At" /> to compute the visible index.
///     Built directly on <see cref="ItemStackDisplay" />.
/// </summary>
public class ItemStackSlideshow : StatefulWidget
{
    /// <summary>Creates an <see cref="ItemStackSlideshow" />.</summary>
    /// <param name="stacks">Ordered stacks to cycle through.</param>
    /// <param name="size">Icon width and height in pixels.</param>
    public ItemStackSlideshow(IReadOnlyList<ItemStack> stacks, float size = 40f)
    {
        Stacks = stacks;
        Size = size;
    }

    /// <summary>The stacks to cycle through.</summary>
    public IReadOnlyList<ItemStack> Stacks { get; }

    /// <summary>Icon width and height in pixels.</summary>
    public float Size { get; }

    /// <inheritdoc />
    public override State CreateState() => new SlideshowState();

    private class SlideshowState : State<ItemStackSlideshow>
    {
        private long _elapsedMs;
        private Ticker _ticker = null!;

        /// <inheritdoc />
        public override void InitState()
        {
            base.InitState();
            _ticker = Element.Owner!.GetTickerProvider().CreateTicker(OnTick);
            _ticker.Start();
        }

        private void OnTick(TimeSpan delta) =>
            SetState(() => _elapsedMs += (long)delta.TotalMilliseconds);

        /// <inheritdoc />
        public override void Dispose()
        {
            _ticker?.Dispose();
            base.Dispose();
        }

        /// <inheritdoc />
        public override Widget Build(BuildContext context)
        {
            if (Widget.Stacks.Count == 0)
            {
                return new SizedBox(Widget.Size, Widget.Size);
            }

            var count = Widget.Stacks.Count;
            var curIndex = SlideshowIndex.At(_elapsedMs, count);

            const int FadeStartMs = 800;
            var phaseMs = _elapsedMs % 1000;
            if (count > 1 && phaseMs >= FadeStartMs)
            {
                var rawT = (float)(phaseMs - FadeStartMs) / (1000 - FadeStartMs);
                var t = (float)Curves.EaseInOut.Transform(rawT);
                var nextIndex = (curIndex + 1) % count;
                return new SlideshowCrossfade(Widget.Stacks[curIndex], Widget.Stacks[nextIndex], t,
                    Widget.Size);
            }

            return new ItemStackDisplay(Widget.Stacks[curIndex], Widget.Size, Widget.Size,
                (int)(Widget.Size * 2));
        }
    }
}
