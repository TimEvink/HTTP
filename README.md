# MyHttp

MyHttp is a minimal HTTP/1.1 implementation built directly on top of TCP using C#.  
The goal of this project is **educational and architectural**: to understand HTTP by implementing it from first principles.

The project focuses on:
- Stream-oriented design
- Explicit serialization and parsing
- Clear ownership of framing (Content-Length, Transfer-Encoding)
- Minimal but correct end-to-end HTTP communication

---

## Features

- Custom `HttpRequest` and `HttpResponse` models
- Manual HTTP serialization and parsing
- Stream-based framing (currently `Content-Length`)
- Serialization -> Parsing pipeline tested.
- Working TCP server and client proof-of-concept
- No `StreamReader` / `StreamWriter` buffering pitfalls — raw stream control

---

## Project Structure

The solution consists of four projects:

### 1. **MyHttp.Core** (Class Library)
Contains the core HTTP logic:
- HTTP message models (`HttpRequest`, `HttpResponse`)
- Parsing logic (`HttpRequestParser`, `HttpResponseParser`)
- Serialization logic (`HttpRequestSerializer`, `HttpResponseSerializer`)
- Framing / encoding abstractions (e.g. `Content-Length` streams)
- Validation and protocol utilities

This project is completely transport-agnostic and only depends on `Stream`.

---

### 2. **MyHttp.Server** (Console App)
A minimal TCP-based HTTP server:
- Uses `TcpListener` from `System.Net.Sockets` to listen on a TCP socket
- Parses incoming HTTP requests from a `NetworkStream`
- Routes requests via a simple handler
- Serializes and sends HTTP responses

This is intentionally minimal and meant as a proof-of-concept server.

---

### 3. **MyHttp.Client** (Console App)
A minimal TCP client:
- Connects to the server via `TcpClient`
- Sends a manually constructed HTTP request
- Parses the HTTP response
- Prints status and body

Demonstrates full client-server HTTP flow without any framework magic.

---

### 4. **MyHttp.Tests** (xUnit)
Test project containing:
- End-to-end serialization → parsing tests for both `HttpRequest` and `HttpResponse` using `MemoryStream` to mimic `NetworkStream`.

Making these tests and ensuring they passed happened before the actual servers/clients.

---

## Design Philosophy

- **Pure stream-based IO**
  No `StreamReader`/`StreamWriter` in the protocol layer, just direct reads and writes on streams.  
  Explicit byte-level control.
- **Clear separation of concrerns**  
  Parsing owns decoding.
  Serialization owns encoding.
- **Framing via streams**
  Message bodies are wrapped in framing streams (`ContentLengthEncodingStream`, `ContentLengthDecodingStream`).

---

## Example

A minimal request/response exchange:

### Running the Server
```bash
dotnet run --project MyHttp.Server 8000
```

### Running the Client
```bash
dotnet run --project MyHttp.Client 8000
```
This will trigger a single Request-Response cycle with the Client writing to the Console the body of the response.

##

## Changelog

Current version: `v0.1.0`

### v0.1.0
- Initial working HTTP/1.1 pipeline
- Manual parsing and serialization
- `Content-Length` framing
- End-to-end serialization → parsing tests for both `HttpRequest` and `HttpResponse`.
- End-to-end client/server demo over TCP
