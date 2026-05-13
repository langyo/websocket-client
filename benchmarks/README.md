# Websocket.Client Benchmarks

This folder contains BenchmarkDotNet benchmarks for the allocation-sensitive Websocket.Client hot paths.

The latest representative run below was captured on Windows 11, AMD Ryzen 9 3900X, .NET SDK 10.0.201, running the benchmarks on .NET 8.0.25 with BenchmarkDotNet `ShortRun`.

Run all benchmarks:

```powershell
dotnet run --configuration Release --project benchmarks\Websocket.Client.Benchmarks -- --filter "*"
```

Run the most relevant receive/send benchmarks:

```powershell
dotnet run --configuration Release --project benchmarks\Websocket.Client.Benchmarks -- --filter "*ReceiveBufferBenchmarks*"
dotnet run --configuration Release --project benchmarks\Websocket.Client.Benchmarks -- --filter "*TextSendEncodingBenchmarks*"
```

Results are written to `BenchmarkDotNet.Artifacts\results`.

## What The Benchmarks Cover

- `ReceiveBufferBenchmarks` compares the old per-message `RecyclableMemoryStream` receive path, PR #158's `ArrayBufferWriter<byte>` path, and the current pooled receive buffer.
- `TextSendEncodingBenchmarks` compares outgoing text encoding through `Encoding.GetBytes(string)` against encoding into an `ArrayPool<byte>` buffer.
- `ResponseMessageBenchmarks` measures the `ResponseMessage.ToString()` stream-copy fix.
- `ObservablePropertyBenchmarks` measures cached observable properties versus creating an `AsObservable()` wrapper on every access.
- `ClientReceiveBenchmarks` measures the current public `WebsocketClient` receive path using an in-memory scripted `WebSocket`.

Use the Ratio and Allocated columns for the quick answer. Values below 1.00 in Ratio are faster than the baseline, and lower Allocated values mean less GC pressure.

## Representative Results

### Receive Buffering

`ReceiveBufferBenchmarks` compares the original receive path, PR #158, and the current pooled buffer. These benchmarks include UTF-8 decoding to `string`, so large text messages are dominated by the required output string allocation.

| Message size | Before: RecyclableMemoryStream | PR #158: ArrayBufferWriter | Current: pooled buffer | Current impact |
| ---: | ---: | ---: | ---: | --- |
| 128 B | 355.76 ns, 560 B | 63.18 ns, 280 B | 54.36 ns, 280 B | 85% faster, 50% less allocation |
| 4 KB | 1.130 us, 8496 B | 847.05 ns, 8216 B | 738.98 ns, 8216 B | 35% faster, slightly less allocation |
| 32 KB | 4.966 us, 65840 B | 5.440 us, 65560 B | 5.166 us, 65560 B | roughly neutral |
| 128 KB | 98.302 us, 262476 B | 98.932 us, 262196 B | 105.350 us, 262224 B | roughly neutral; avoids retaining a large receive buffer |

### Text Send Encoding

`TextSendEncodingBenchmarks` measures the change from `Encoding.GetBytes(string)` to encoding into a rented `ArrayPool<byte>` buffer before calling `WebSocket.SendAsync`.

| Message length | Before: `Encoding.GetBytes` | Current: ArrayPool encode | Current impact |
| ---: | ---: | ---: | --- |
| 32 chars | 16.39 ns, 56 B | 40.13 ns, 0 B | removes allocation with a small CPU cost |
| 1024 chars | 134.04 ns, 1048 B | 87.44 ns, 0 B | 35% faster, allocation-free |
| 8192 chars | 974.85 ns, 8216 B | 501.86 ns, 0 B | 48% faster, allocation-free |

### Binary Message Logging

`ResponseMessageBenchmarks` measures `ResponseMessage.ToString()` for stream-backed binary messages. The previous implementation copied the stream with `ToArray()` just to print the length.

| Message size | Before: stream `ToArray()` | Current: stream `Length` | Current impact |
| ---: | ---: | ---: | --- |
| 64 B | 56.69 ns, 160 B | 98.88 ns, 96 B | less allocation, slightly slower |
| 4 KB | 348.65 ns, 4192 B | 103.42 ns, 96 B | 70% faster, 98% less allocation |
| 32 KB | 1.880 us, 32872 B | 101.23 ns, 104 B | 95% faster, near-zero payload allocation |

### Observable Property Access

`ObservablePropertyBenchmarks` measures repeated access to public observable properties such as `MessageReceived`.

| Scenario | Mean | Allocated |
| --- | ---: | ---: |
| Before: `AsObservable()` per access | 17.81 ns | 24 B |
| Current: cached observable wrapper | 0.69 ns | 0 B |

### Current Client Receive Path

`ClientReceiveBenchmarks` measures the current public `WebsocketClient` receive flow using an in-memory scripted `WebSocket`. It is not a before/after benchmark; it is a package-level smoke benchmark for the real client path.

| Messages | Message size | Mean | Allocated |
| ---: | ---: | ---: | ---: |
| 100 | 64 B | 15.24 us | 29.4 KB |
| 100 | 1024 B | 32.86 us | 216.9 KB |
| 1000 | 64 B | 135.71 us | 240.34 KB |
| 1000 | 1024 B | 275.40 us | 2115.34 KB |
