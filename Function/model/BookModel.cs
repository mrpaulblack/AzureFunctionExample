using AzureFunctionExample.Enum;

namespace AzureFunctionExample.Model;
/// <summary>
/// Defines Model of a book. This is returned by the list and get book endpoint.
/// </summary>
/// <param name="Author">Author of the book.</param>
/// <param name="Title">Title of the book. Should not contain spaces for the get endpoint to work.</param>
/// <param name="PublishYear">Year of publication of the book.</param>
/// <param name="Genre">Genre of the book.</param>
public record BookModel
(
    string Author,
    string Title,
    int PublishYear,
    Genre Genre
);
