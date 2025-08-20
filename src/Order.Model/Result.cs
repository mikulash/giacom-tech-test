using System.Net;

namespace Order.Model;

public class Result<T>
{
    public HttpStatusCode StatusCode { get; private set; }
    public T Data { get; private set; }
    public string Message { get; private set; }
    public bool IsOk => StatusCode is >= HttpStatusCode.OK and < HttpStatusCode.MultipleChoices; // 200
    
    private Result(HttpStatusCode statusCode, T data, string message)
    {
        StatusCode = statusCode;
        Data = data;
        Message = message;
    }

    public static Result<T> Success(T data, string message = null)
        => new(HttpStatusCode.OK, data, message);
    
    public static Result<T> Created(T data, string message = null) => new(HttpStatusCode.Created, data, message);

    public static Result<T> NoContent(string message = null)
        => new(HttpStatusCode.NoContent, default(T), message);

    public static Result<T> NotFound(string message)
        => new(HttpStatusCode.NotFound, default(T), message);

    public static Result<T> BadRequest(string message)
        => new(HttpStatusCode.BadRequest, default(T), message);
}