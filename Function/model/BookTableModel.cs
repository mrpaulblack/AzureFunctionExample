using Azure;
using Azure.Data.Tables;

namespace AzureFunctionExample.Model;
public class BookTableModel : ITableEntity
{
    // wont use; set to empty string -> ""
    public string PartitionKey { get; set; } = string.Empty;

    //book unique ISBN
    public required string RowKey { get; set; }

    // define last insert / update date
    public DateTimeOffset? Timestamp { get; set; }

    // used for cache validation; can be ingored for our use case
    public ETag ETag { get; set; }

    public required string Title { get; set; }

    public required string Author { get; set; }

    public required int PublishYear { get; set; }
}
