# Blazor .NET 8 Tick Manager Debugging Issue  
**Date:** 2025-12-02  
**Context:** Blazor Server (.NET 8, C# 12) – Testing a central tick manager with a fake tick source.

## Overview
I'm building a central tick manager (`TickSource_Manager_Service`) that listens to one or more `ITickSource_Service` implementations, stores the most recent tick per symbol, and notifies Blazor components via an `OnTickUpdated` event.

To test this pipeline, I created a fake tick source (`TickSource_Service_Test`) that generates ticks for `MESZ5`, `MNQZ5`, `MYMZ5`, `GC`, and `CL` in a background loop.

The debug tick `"DBG"` appears correctly.  
The real fake ticks never appear in the manager.

## Symptoms
### Logging shows:
- `TickSource_Service_Test` is running  
- The broker sync (`Service_Sync` + `Service_Sierra_Rithmic`) is running  
- `TickSource_Manager_Service` logs:
  - "Subscribed to tick source 'Test_TickSource'"
  - "Initialized with 1 tick source(s)"
  - "Now tracking symbol 'DBG'"

### Missing logs:
- Never logs "Now tracking symbol 'MESZ5'"  
- Never logs "Now tracking symbol 'MNQZ5'"  
- Never logs "Now tracking symbol 'MYMZ5'"  
- Never logs "Now tracking symbol 'GC'"  
- Never logs "Now tracking symbol 'CL'"

### UI shows only one row:

Symbol Last Bid Ask Time
DBG 999.99 999.74 1000.24 <timestamp>


So the manager appears to receive the debug tick, but never the real ones.

---

# Relevant Code

## 1. DTO + Interface

```csharp
namespace TradingAppProject.Service.DataConnection.MarketData
{
    public sealed class MarketTick
    {
        public string Symbol { get; set; } = "";
        public decimal Last { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    public interface ITickSource_Service
    {
        string SourceName { get; }
        event Action<MarketTick>? OnTick;
        Task SubscribeAsync(string symbol, CancellationToken cancellationToken = default);
        Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default);
    }
}

2. Test Tick Source

namespace TradingAppProject.Service.DataConnection.MarketData
{
    public sealed class TickSource_Service_Test : ITickSource_Service, IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Random _rng = new();
        private readonly string[] _symbols = ["MESZ5", "MNQZ5", "MYMZ5", "GC", "CL"];

        public string SourceName => "Test_TickSource";
        public event Action<MarketTick>? OnTick;

        public TickSource_Service_Test()
        {
            _ = Task.Run(GenerateTicksAsync);
        }

        private async Task GenerateTicksAsync()
        {
            var ct = _cts.Token;

            var basePrices = new Dictionary<string, decimal>
            {
                ["MESZ5"] = 5000m,
                ["MNQZ5"] = 18000m,
                ["MYMZ5"] = 40000m,
                ["GC"]    = 2400m,
                ["CL"]    = 75m
            };

            while (!ct.IsCancellationRequested)
            {
                foreach (string symbol in _symbols)
                {
                    decimal basePrice = basePrices[symbol];
                    decimal delta = (decimal)(_rng.NextDouble() - 0.5) * 2m;
                    basePrice += delta;
                    basePrices[symbol] = basePrice;

                    decimal last = Math.Round(basePrice, 2);
                    decimal bid = last - 0.25m;
                    decimal ask = last + 0.25m;

                    OnTick?.Invoke(new MarketTick
                    {
                        Symbol = symbol,
                        Last = last,
                        Bid = bid,
                        Ask = ask,
                        TimestampUtc = DateTime.UtcNow
                    });
                }

                await Task.Delay(500, ct);
            }
        }

        public Task SubscribeAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UnsubscribeAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}

3. Central Tick Manager

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

            foreach (ITickSource_Service? source in tickSources)
            {
                if (source is null) continue;

                _sources.Add(source);
                source.OnTick += HandleTick;

                _messageService.Add(
                    "TickSource_Manager",
                    $"Subscribed to tick source '{source.SourceName}'.");
            }

            _messageService.Add(
                "TickSource_Manager",
                $"Initialized with {_sources.Count} tick source(s).");

            HandleTick(new MarketTick
            {
                Symbol = "DBG",
                Last = 999.99m,
                Bid = 999.74m,
                Ask = 1000.24m,
                TimestampUtc = DateTime.UtcNow
            });
        }

        private void HandleTick(MarketTick tick)
        {
            if (string.IsNullOrWhiteSpace(tick.Symbol)) return;

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
            if (_disposed) return;

            foreach (ITickSource_Service source in _sources)
                source.OnTick -= HandleTick;

            _disposed = true;
        }
    }
}

4. DI Registration

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<Service_Message>();
builder.Services.AddSingleton<IService_BrokerConnection, Service_Sierra_Rithmic>();
builder.Services.AddSingleton<ITickSource_Service, TickSource_Service_Test>();
builder.Services.AddSingleton<TickSource_Manager_Service>();
builder.Services.AddSingleton<Service_Sync>();
builder.Services.AddSingleton<Service_Grid_All>();

var app = builder.Build();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

5. Blazor Tick Monitor UI

@inject TickSource_Manager_Service TickSourceManager
@implements IDisposable

<div class="tick-monitor">
    <h3>Live Tick Monitor (Test)</h3>

    @if (_snapshot.Count == 0)
    {
        <p>No ticks received yet.</p>
    }
    else
    {
        <table class="grid">
            <thead>
                <tr>
                    <th>Symbol</th>
                    <th>Last</th>
                    <th>Bid</th>
                    <th>Ask</th>
                    <th>Time (Local)</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var kvp in _snapshot.OrderBy(x => x.Key))
                {
                    var tick = kvp.Value;
                    <tr>
                        <td>@tick.Symbol</td>
                        <td>@tick.Last</td>
                        <td>@tick.Bid</td>
                        <td>@tick.Ask</td>
                        <td>@tick.TimestampUtc.ToLocalTime()</td>
                    </tr>
                }
            </tbody>
        </table>
    }
</div>

@code {
    private IReadOnlyDictionary<string, MarketTick> _snapshot =
        new Dictionary<string, MarketTick>();

    protected override void OnInitialized()
    {
        _snapshot = TickSourceManager.GetSnapshot();
        TickSourceManager.OnTickUpdated += HandleTick;
    }

    private void HandleTick(MarketTick tick)
    {
        _snapshot = TickSourceManager.GetSnapshot();
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        TickSourceManager.OnTickUpdated -= HandleTick;
    }
}

What I Need Help With

Why does the tick manager only ever track the debug symbol "DBG" and never track:

MESZ5, MNQZ5, MYMZ5, GC, CL
even though TickSource_Service_Test is registered and firing ticks?

Could this be caused by:

DI lifetime issues

How IEnumerable<ITickSource_Service> is populated

Starting a background Task.Run inside a singleton in Blazor Server

Is there a better or more idiomatic pattern for:

creating fake/test tick sources

wiring multiple tick sources into a central manager

pushing real-time updates to Blazor components via events



