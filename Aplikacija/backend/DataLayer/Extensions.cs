namespace DataLayer;

public static class Extensions
{
    internal static ErrorMessage ToError(this string message, int statusCode = 400)
    {
        return new ErrorMessage(message, statusCode);
    }
}