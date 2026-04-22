namespace Lab1.Extensions;

public static class SessionKeys
{
    public const string BookCreateDraft = "Book.Create.Draft";
    public const string ReaderCreateDraft = "Reader.Create.Draft";
    public const string LoanCreateDraft = "Loan.Create.Draft";

    public static string BookEditDraft(int id) => $"Book.Edit.Draft.{id}";
    public static string ReaderEditDraft(int id) => $"Reader.Edit.Draft.{id}";
    public static string LoanEditDraft(int id) => $"Loan.Edit.Draft.{id}";
}
