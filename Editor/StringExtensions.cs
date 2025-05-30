using System.Text.RegularExpressions;

namespace AAGen
{
    public static class StringExtensions
    {
        public static string ToReadableFormat(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove common prefixes (m_ or k_)
            input = Regex.Replace(input, "^(m_|k_)", "");

            // Insert a space before each uppercase letter
            input = Regex.Replace(input, "(\\B[A-Z])", " $1");

            // Capitalize the first letter
            return char.ToUpper(input[0]) + input.Substring(1);
        }
        
        public static string RemoveExtension(this string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return fileName;

            int index = fileName.LastIndexOf('.');
            return index > 0 ? fileName.Substring(0, index) : fileName;
        }
    }
}