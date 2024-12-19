using System.Linq;
using System.Net;
using AzureFunctionExample.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace AzureFunctionExample;

public class Books
{
    private readonly ILogger _logger;
    private readonly IList<BookModel> _books;

    public Books(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Books>();

        // define list of books
        _books =
        [
            new(
                Author: "Marie Musterfrau",
                Title: "Computer Science 101",
                PublishYear: 2019,
                Genre: Enum.Genre.Scientific
            ),
            new("Max Musterman", "Cyperpunk", 1994, Enum.Genre.Fiction),
        ];
    }

    // get list of all books
    [OpenApiOperation(
        operationId: "listBooks",
        tags: ["books"],
        Summary = "List Books",
        Description = "Get list of books."
    )]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(string),
        Summary = "The response",
        Description = "A list of books in json format."
    )]
    [Function("HTTPListBooks")]
    public async Task<HttpResponseData> ListBooks(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "book")] HttpRequestData request
    )
    {
        _logger.LogInformation(
            $"[{request.FunctionContext.InvocationId}] Processing request for list books endpoint."
        );

        var response = request.CreateResponse(HttpStatusCode.OK);

        await response.WriteAsJsonAsync(_books);

        return response;
    }

    // get specific book by its title
    [OpenApiOperation(
        operationId: "getBook",
        tags: ["books"],
        Summary = "Get Book",
        Description = "Get a book by its title."
    )]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query
    )]
    [OpenApiParameter(
        name: "bookTitle",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "Title of book to return",
        Description = "Return the book for the given title."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(string),
        Summary = "The response",
        Description = "A book in json formet."
    )]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.NotFound,
        Summary = "Book not found",
        Description = "Returned when there is no book found for the given title."
    )]
    [Function("HTTPGetBook")]
    public async Task<HttpResponseData> GetBook(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "book/{bookTitle}")]
            HttpRequestData request,
        string bookTitle
    )
    {
        _logger.LogInformation(
            $"[{request.FunctionContext.InvocationId}] Processing request to get specific book by its title with the title {bookTitle}."
        );

        try
        {
            var selectedBook = _books.Single(x => x.Title == bookTitle);

            var response = request.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(selectedBook);
            return response;
        }
        catch (InvalidOperationException)
        {
            _logger.LogWarning(
                $"[{request.FunctionContext.InvocationId}] Error: {bookTitle} was not found."
            );
            return request.CreateResponse(HttpStatusCode.NotFound);
        }
    }
}
