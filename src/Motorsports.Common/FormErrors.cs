namespace Motorsports.Common;

/// <summary>Field and form level messages of a failed operation, shaped for inline display.</summary>
public sealed class FormErrors
{
    private static readonly IReadOnlyDictionary<string, string[]> None = new Dictionary<string, string[]>();

    private IReadOnlyDictionary<string, string[]> fields = None;

    public string? Summary { get; private set; }

    public bool Any => Summary is not null || fields.Count > 0;

    public void Clear()
    {
        fields = None;
        Summary = null;
    }

    public void Set(Error error)
    {
        fields = error.Fields ?? None;
        Summary =
            error.Kind == ErrorKind.Validation
                ? (fields.TryGetValue("", out var form) && form.Length > 0 ? form[0] : null)
                : error.Message;
    }

    public bool Has(string field) => fields.ContainsKey(field);

    public string? Text(string field) =>
        fields.TryGetValue(field, out var messages) ? string.Join(" ", messages) : null;
}
