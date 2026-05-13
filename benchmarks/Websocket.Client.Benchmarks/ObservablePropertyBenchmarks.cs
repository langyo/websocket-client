using BenchmarkDotNet.Attributes;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Websocket.Client.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[RankColumn]
public class ObservablePropertyBenchmarks
{
    private readonly Subject<ResponseMessage> _subject = new();
    private IObservable<ResponseMessage> _cached = null!;

    [GlobalSetup]
    public void Setup()
    {
        _cached = _subject.AsObservable();
    }

    [Benchmark(Baseline = true, Description = "Before: AsObservable per access")]
    public IObservable<ResponseMessage> Before_AsObservableEachAccess()
    {
        return _subject.AsObservable();
    }

    [Benchmark(Description = "Current: cached observable")]
    public IObservable<ResponseMessage> Current_CachedObservable()
    {
        return _cached;
    }
}
