# MyHttp

MyHttp is a minimal HTTP/1.1 implementation built directly on top of TCP.
The goal of this project is educational and architectural: to understand HTTP by implementing it from first principles. It is designed to be small, transparent, and easy to reason about, while still reflecting real-world HTTP behavior.

The project focuses on:
- Stream-oriented design
- Efficient serialization and parsing
- Minimal but correct end-to-end HTTP communication

---

## Quick Start

Run the server:
```bash
dotnet run --project MyHttp.Server
```

In another terminal, run the client:
```bash
dotnet run --project MyHttp.Client
```
This performs a single request–response cycle against `localhost:8000` and prints the response body to the console. A different port can be specified by passing it as an argument to both the server and client.


Alternatively, run:

```bash
dotnet run --project MyHttp.FileHost -- <port> <absolute directory path>
```

to spin up a simple static file hosting server on `localhost:<port>` that hosts files from the supplied absolute directory path.

--- 

## Features

- Custom `HttpRequest` / `HttpResponse` models and `HttpServer` / `HttpClient` abstractions
- Efficient span-based HTTP serialization and parsing
- Stream-based framing (currently only `Content-Length` supported)
- End-to-end serialization → parsing pipeline tested for both requests and responses
- Protocol and server tests using in-process duplex stream to mimic a TCP connection
- Functional static file hosting server
- No `StreamReader` / `StreamWriter` buffering pitfalls — raw stream control.

### Supported:
- Async handling of multiple client connections by `HttpServer`
- Efficient pipelined request handling via buffer-driven parsing (multiple requests parsed from a single read)
- Content-Length framing

### Limitations:
- Chunked transfer encoding not supported
- Compression encodings not supported
- Quoted commas are currently not ignored when splitting header values

---

## Project Structure

The solution consists of five projects:

### 1. **MyHttp.Core** (Class Library)
Contains the core HTTP logic:
- HTTP message models (`HttpRequest`, `HttpResponse`)
- Fluent API HTTP message builders (`HttpRequestBuilder`, `HttpResponseBuilder`)
- A single `HttpConnection` base class responsible for serializing and parsing, managing input and output buffers and owning the underlying `NetworkStream`
- Stream-based body parsing via `DecodingStream`
- Exposes `HttpServer` and `HttpClient` objects, wrapping respectively `TcpListener` and `TcpClient` from `System.Net.Sockets`.

The core protocol logic is completely transport agnostic and works on any full duplex `Stream` pair.

---

### 2. **MyHttp.Server** (Console App)

A minimal HTTP server based on `MyHttp.Core.HttpServer`.  

---

### 3. **MyHttp.Client** (Console App)

A minimal HTTP client based on `MyHttp.Core.HttpClient`.  

---

### 4. **MyHttp.Tests** (xUnit)

Test project containing:
- End-to-end serialization → parsing tests for both `HttpRequest` and `HttpResponse`.
- Concurrency tests.
- Full round trip tests.
- Request handling tests for server.

A TCP connection exposes a duplex stream between two processes (not necessarily on the same machine).
In tests, this is mimicked using an in-process duplex stream pair, enabling full client-server interaction within a single process.

### 5. **MyHttp.FileHost** (Console App)

A simple static file hosting server based on `MyHttp.Core.HttpServer`

---

## Server Benchmarks

Basic benchmarking of `MyHttp.Server` (with logging disabled) was performed using [`wrk`](https://github.com/wg/wrk) with both the server and `wrk` running inside **WSL2 Ubuntu 22.04** on a local machine (12th Gen Intel(R) Core(TM) i5-12450H, Windows 11). These tests target a minimal endpoint returning a small static response.

### Test Setup

- Server and `wrk` running in WSL2 (Ubuntu 22.04)
- Minimal HTTP endpoint returning simple text
- CPU: 12th Gen Intel i5-12450H
- Network: localhost (`127.0.0.1`) for all requests
- Build configuration: `dotnet run -c Release` inside WSL filesystem (`/home/...`) for best performance

### Example `wrk` Commands

```bash
# 2 threads, 80 connections, 5-second burst
wrk -t2 -c80 -d5s http://127.0.0.1:8000/

# 4 threads, 80 connections, 5-second burst
wrk -t4 -c80 -d5s http://127.0.0.1:8000/

# 4 threads, 80 connections, 30-second sustained
wrk -t4 -c80 -d30s http://127.0.0.1:8000/
```

### Benchmark Results (Ballpark)

| Threads | Connections | Duration | Requests/sec | Transfer/sec | Avg Latency |
|---------|-------------|----------|--------------|--------------|-------------|
| 2       | 80          | 5s       | ~250,000     | 55 MB/s      | 200-350 µs  |
| 4       | 80          | 5s       | ~320,000     | 82 MB/s      | 200-250 µs  |
| 4       | 80          | 30s      | ~200,000     | 59 MB/s      | 300-500 µs  |

**Notes:**

- These numbers represent **ideal local benchmarking**; actual throughput will vary depending on CPU, workload complexity, network setup, and duration of the test.
- Burst numbers (short 5s tests) are higher than sustained 30s runs due to CPU/Garbage Collector effects.
- Some minor socket read errors occurred under heavy load (~0.2% of requests) — normal for high concurrency testing.
- Always run benchmarks from the WSL filesystem (`/home/...`) rather than `/mnt/c/...` to avoid NTFS overhead and permission issues.
- Responses are small and generated per request using a lightweight builder (no disk I/O or external dependencies).
