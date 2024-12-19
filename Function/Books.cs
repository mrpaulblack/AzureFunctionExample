using System.Net;
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

    public Books(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Books>();
    }

    // function called Books
    [OpenApiOperation(operationId: "books", tags: new[] {"books"}, Summary = "List Books", Description = "Get list of books.", Visibility = OpenApiVisibilityType.Important)]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Summary = "The response", Description = "A list of books in json format.")]
    [Function("Books")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var response = request.CreateResponse(HttpStatusCode.OK);

        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

        response.WriteString("Welcome to Azure Functions!");

        return response;
    }
}
