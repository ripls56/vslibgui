using System;
using System.Collections.Generic;

namespace Gui.Widgets.Animations;

public class TickerScheduler : ITickerProvider, IDisposable
{
    private readonly List<Ticker> _tickers = [];

    public void Dispose()
    {
        foreach (var ticker in _tickers)
        {
            ticker.Dispose();
        }

        _tickers.Clear();
    }

    public Ticker CreateTicker(
        Action<TimeSpan> onTick
    )
    {
        var ticker = new Ticker(onTick);
        _tickers.Add(ticker);
        return ticker;
    }

    public void Update(
        TimeSpan elapsed
    )
    {
        var hasDisposed = false;
        // Process active tickers
        for (var i = 0; i < _tickers.Count; i++)
        {
            var ticker = _tickers[i];
            if (ticker.IsDisposed)
            {
                hasDisposed = true;
                continue;
            }

            ticker.Tick(elapsed);
        }

        // Clean up disposed tickers if any were found
        if (hasDisposed)
        {
            _tickers.RemoveAll(t => t.IsDisposed);
        }
    }
}
