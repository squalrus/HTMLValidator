namespace HTMLValidator.Extensions
{
    public static class StringExtensions
    {
        public static string ToSlug(this string s)
        {
            return s.Replace("https://", "").TrimEnd('/').Replace("/", "_");
        }
    }
}
