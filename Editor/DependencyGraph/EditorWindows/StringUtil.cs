using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AAGen.Editor
{
    public static class StringUtil
    {
        /// <summary>
        /// Splits a comma-separated string into a list of strings.
        /// </summary>
        /// <param name="input">The comma-separated string.</param>
        /// <returns>A list of strings.</returns>
        public static List<string> SplitCommaSeperatedString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;
            
            var splitInputArray = input.Split(",", StringSplitOptions.RemoveEmptyEntries);
            return splitInputArray.Select(splitInput => splitInput.Trim()).ToList();
        }
    }
}
