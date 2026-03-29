### v0.2.0

- Major performance improvements through buffered I/O and batch processing (replacing per-byte reads).
- Introduced span-based, zero-allocation parsing.
- Added `HttpServer` and `HttpClient` abstractions, with the server capable of handling multiple client connections asynchronously.
- Added fluent builders: `HttpRequestBuilder` and `HttpResponseBuilder`.
- Improved testing by simulating TCP connections using an in-process duplex stream pair.
- Added a fifth project implementing a lightweight static file hosting server using `HttpServer`.

### v0.1.0

- Initial working HTTP/1.1 request/response pipeline.
- Manual parsing and serialization.
- `Content-Length` framing.
- End-to-end serialization → parsing tests for both `HttpRequest` and `HttpResponse`.
- End-to-end client/server communication over TCP.
