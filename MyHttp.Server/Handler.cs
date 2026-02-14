using MyHttp.Core.Messages;

namespace MyHttp.Server;
public static class Handler {
    public static HttpResponse Handle(HttpRequest request) {
        return (request.Method == HttpMethod.GET && request.Target.RawPath == "/")
            ? HttpResponses.Ok("Hello from server!")
            : HttpResponses.BadRequest("Disappointment from server!");
    }
}