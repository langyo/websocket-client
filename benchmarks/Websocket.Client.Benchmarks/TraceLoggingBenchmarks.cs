using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Websocket.Client.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
[ShortRunJob]
public class TraceLoggingBenchmarks
{
    private const string Name = "benchmark";
    private const long PayloadLength = 256;
    private static readonly ILogger Logger = NullLogger.Instance;

    [Benchmark(Baseline = true)]
    public void UnguardedDisabledTrace()
    {
        Logger.LogTrace("[WEBSOCKET {name}] Sending binary, length: {length}", Name, PayloadLength);
    }

    [Benchmark]
    public void GuardedDisabledTrace()
    {
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace("[WEBSOCKET {name}] Sending binary, length: {length}", Name, PayloadLength);
        }
    }
}
