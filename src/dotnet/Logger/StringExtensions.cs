namespace GitHub.VsTest.Logger;

internal static class StringExtensions
{
    /// <summary> Returns bool value represents the string ("1"/"True"/"tRuE" => true etc). </summary>
    public static bool asBool(this string self)
    {
        bool result = false;
        if (!string.IsNullOrWhiteSpace(self) && !bool.TryParse(self, out result))
            result = string.Equals(self, "1", StringComparison.Ordinal);
        return result;
    }
}
