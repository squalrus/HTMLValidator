namespace HTMLValidator.Extensions
{
    public static class StringExtensions
    {
        public static string ToSlug(this string s)
        {
            return s.Replace("https://", "").TrimEnd('/').Replace("/", "_");
        }
        public static string ToUrl(this string s)
        {
            return $"https://{s.Replace("_", "/")}/";
        }
    }
}
