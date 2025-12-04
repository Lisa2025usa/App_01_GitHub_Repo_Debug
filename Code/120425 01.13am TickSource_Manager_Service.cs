// Updated: 2025-12-02 20:55 PST - Central tick manager that starts/stops sources and broadcasts latest ticks to the UI

using System;
using System.Collections.Generic;

namespace TradingAppProject.Service.DataConnection.MarketData
{
    public sealed class TickSource_Manager_Service : IDisposable
    {
        private readonly object _lock = new();
        private readonly List<ITickSource_Service> _sources = [];
        private readonly Dictionary<string, MarketTick> _latestTicks =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Service_Message _messageService;
        private bool _disposed;

        public event Action<MarketTick>? OnTickUpdated;

        public TickSource_Manager_Service(
            Service_Message messageService,
            IEnumerable<ITickSource_Service> tickSources)
        {
            _messageService = messageService;

            // 1) Collect sources and subscribe to their tick events.
            foreach (ITickSource_Service? source in tickSources)
            {
                if (source is null)
                {
                    continue;
                }

                _sources.Add(source);
                source.OnTick += HandleTick;

                _messageService.Add(
                    "TickSource_Manager",
                    $"Subscribed to tick source '{source.SourceName}'.");
            }

            _messageService.Add(
                "TickSource_Manager",
                $"Initialized with {_sources.Count} tick source(s).");

            // 2) Seed with a debug tick so the Tick Monitor can prove the manager works.
            var debugTick = new MarketTick
            {
                Symbol = "DBG",
                Last = 999.99m,
                Bid = 999.74m,
                Ask = 1000.24m,
                TimestampUtc = DateTime.UtcNow
            };

            HandleTick(debugTick);

            // 3) Only AFTER subscriptions are wired, start each source.
            foreach (ITickSource_Service source in _sources)
            {
                try
                {
                    source.Start();

                    _messageService.Add(
                        "TickSource_Manager",
                        $"Started tick source '{source.SourceName}'.");
                }
                catch (Exception ex)
                {
                    _messageService.Add(
                        "TickSource_Manager",
                        $"ERROR starting tick source '{source.SourceName}': {ex.Message}");
                }
            }
        }

        private void HandleTick(MarketTick tick)
        {
            if (tick is null || string.IsNullOrWhiteSpace(tick.Symbol))
            {
                return;
            }

            bool isNewSymbol;

            lock (_lock)
            {
                isNewSymbol = !_latestTicks.ContainsKey(tick.Symbol);
                _latestTicks[tick.Symbol] = tick;
            }

            if (isNewSymbol)
            {
                _messageService.Add(
                    "TickSource_Manager",
                    $"Now tracking symbol '{tick.Symbol}'.");
            }

            OnTickUpdated?.Invoke(tick);
        }

        public IReadOnlyDictionary<string, MarketTick> GetSnapshot()
        {
            lock (_lock)
            {
                return new Dictionary<string, MarketTick>(_latestTicks);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            foreach (ITickSource_Service source in _sources)
            {
                try
                {
                    source.OnTick -= HandleTick;
                    source.Stop();
                }
                catch
                {
                    // Ignore shutdown errors for now; real logging can be added later if needed.
                }
            }

            _disposed = true;
        }
    }
}

