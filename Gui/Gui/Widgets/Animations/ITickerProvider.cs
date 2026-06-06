using System;

namespace Gui.Widgets.Animations;

public interface ITickerProvider
{
    Ticker CreateTicker(
        Action<TimeSpan> onTick
    );
}
