namespace AzureFunctionExample.Model;
/// <summary>
/// Defines Model of a book. This is returned by the list and get book endpoint.
/// </summary>
/// <param name="Isbn">Unique ISBN of the book.</param>
/// <param name="Author">Author of the book.</param>
/// <param name="Title">Title of the book. Should not contain spaces for the get endpoint to work.</param>
/// <param name="PublishYear">Year of publication of the book.</param>
public record BookModel
(
    string Isbn,
    string Author,
    string Title,
    int PublishYear
)
{
    // helper method to transform a given BookTableModel to a BookModel
    public static BookModel FromBookTableModel(BookTableModel row)
    {
        return new
        (
            Isbn: row.RowKey,
            Author: row.Author,
            Title: row.Title,
            PublishYear: row.PublishYear
        );
    }
};
