using System.Linq;
using System.Net;
using Azure.Data.Tables;
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
    private readonly TableClient _table;

    public Books(ILoggerFactory loggerFactory, TableServiceClient tableService)
    {
        // name of the azure storage account table where to create, store, lookup and delete books
        string tableName = "books";

        _logger = loggerFactory.CreateLogger<Books>();
        // create TableClient for table with name tableName and create table if not exists already
        tableService.CreateTableIfNotExists(tableName);
        _table = tableService.GetTableClient(tableName);
    }

    // get list of all books
    // define open api attributes / decorators
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
        bodyType: typeof(IList<BookModel>)
    )]
    [Function("HTTPListBooks")]
    public async Task<HttpResponseData> ListBooks(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "book")] HttpRequestData request
    )
    {
        _logger.LogInformation(
            $"[{request.FunctionContext.InvocationId}] Processing request for list books endpoint."
        );

        // get all rows from table storage as list of BookTableModel (this is already deserialized by the TableClient)
        var queryResult = _table.Query<BookTableModel>();

        // transform list of BookTableModel objects to list of BookModel
        var resultList = queryResult.Select(row => BookModel.FromBookTableModel(row)).ToList();

        // return successfull response
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(resultList);
        return response;
    }

    // get specific book by its isbn
    [OpenApiOperation(
        operationId: "getBook",
        tags: ["books"],
        Summary = "Get Book",
        Description = "Get a book by its ISBN."
    )]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query
    )]
    [OpenApiParameter(
        name: "isbn",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "ISBN of the reqeuested book"
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(BookModel)
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.NotFound,
        contentType: "application/json",
        bodyType: typeof(ErrorModel)
    )]
    [Function("HTTPGetBook")]
    public async Task<HttpResponseData> GetBook(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "book/{isbn}")]
            HttpRequestData request,
        string isbn
    )
    {
        _logger.LogInformation(
            $"[{request.FunctionContext.InvocationId}] Processing request to get specific book by its isbn with the isbn {isbn}."
        );

        var queryResult = _table.GetEntityIfExists<BookTableModel>(
            partitionKey: string.Empty,
            rowKey: isbn
        );

        // return HTTP 404 if no book found for the given isbn
        if (!queryResult.HasValue || queryResult.Value == null)
        {
            var errorResponse = request.CreateResponse(HttpStatusCode.NotFound);
            await errorResponse.WriteAsJsonAsync<ErrorModel>(
                new(
                    Error: "BookNotFound",
                    ErrorMessage: "There was no book found for the provided isbn."
                )
            );
            return errorResponse;
        }

        // return sucessfull response as BookModel
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(BookModel.FromBookTableModel(queryResult.Value));
        return response;
    }

    // create new book
    [OpenApiOperation(
        operationId: "createBook",
        tags: ["books"],
        Summary = "Create Book",
        Description = "Create a new book in the backend."
    )]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query
    )]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(BookModel))]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(BookModel)
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.BadRequest,
        contentType: "application/json",
        bodyType: typeof(ErrorModel)
    )]
    [Function("HTTPCreateBook")]
    public async Task<HttpResponseData> CreateBook(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "book")] HttpRequestData request
    )
    {
        _logger.LogInformation(
            $"[{request.FunctionContext.InvocationId}] Processing request for create book endpoint."
        );

        // deserialize request body into BookModel object
        var createBookReq = await request.ReadFromJsonAsync<BookModel>();

        // if request body cannot be deserialized or is null, return an HTTP 400
        if (createBookReq == null)
            return request.CreateResponse(HttpStatusCode.BadRequest);

        // if isbn from book create request already exists -> return an HTTP 400
        if (
            _table
                .GetEntityIfExists<BookTableModel>(
                    partitionKey: string.Empty,
                    rowKey: createBookReq.Isbn
                )
                .HasValue
        )
            return request.CreateResponse(HttpStatusCode.BadRequest);

        // transform BookModel into BookTableModel and write row to table; partition + row key need to be unique!
        var createTableRow = await _table.AddEntityAsync<BookTableModel>(
            new()
            {
                RowKey = createBookReq.Isbn,
                Title = createBookReq.Title,
                Author = createBookReq.Author,
                PublishYear = createBookReq.PublishYear,
            }
        );

        // return error if transaction in table storage unsuccessfull
        if (createTableRow.IsError)
        {
            var errorResponse = request.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync<ErrorModel>(
                new(
                    Error: "TableTransactionError",
                    ErrorMessage: "There was a problem executing the table transaction."
                )
            );
            return errorResponse;
        }

        // serialize requested BookModel to json and return to client, when request successfull
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(createBookReq);
        return response;
    }

    // delete specific book by isbn
    [OpenApiOperation(
        operationId: "deleteBook",
        tags: ["books"],
        Summary = "Delete Book",
        Description = "Delete a book by its ISBN."
    )]
    [OpenApiSecurity(
        "function_key",
        SecuritySchemeType.ApiKey,
        Name = "code",
        In = OpenApiSecurityLocationType.Query
    )]
    [OpenApiParameter(
        name: "isbn",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(string),
        Summary = "ISBN of the to be deleted book"
    )]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.NoContent,
        Summary = "Empty response if sucessfull."
    )]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.InternalServerError,
        contentType: "application/json",
        bodyType: typeof(ErrorModel)
    )]
    [Function("HTTPDeleteBook")]
    public async Task<HttpResponseData> DeleteBook(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "book/{isbn}")]
            HttpRequestData request,
        string isbn
    )
    {
        _logger.LogInformation(
            $"[{request.FunctionContext.InvocationId}] Processing request to delete specific book by its isbn with the isbn {isbn}."
        );

        // try to delete book with given isbn
        var deleteResult = _table.DeleteEntity(partitionKey: string.Empty, rowKey: isbn);

        // return HTTP 500 if deletion unsucessfull
        if (deleteResult.IsError)
        {
            var errorResponse = request.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync<ErrorModel>(
                new(
                    ErrorMessage: $"There was an error deleting the book with isbn {isbn}: {deleteResult.ReasonPhrase}.",
                    Error: "BookDeletionError"
                )
            );
            return errorResponse;
        }

        // return sucessfull response as HTTP 204
        return request.CreateResponse(HttpStatusCode.NoContent);
    }
}
