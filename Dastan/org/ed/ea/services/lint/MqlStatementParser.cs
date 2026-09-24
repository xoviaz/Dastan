using System.Collections.Generic;
using System.Text;

namespace Dastan.org.ed.ea.services.lint
{
    // Splits a generated script into statements on ';', ignoring separators that sit
    // inside a quoted value, and collects each statement's quoted values on the way.
    //
    // A backslash is an ordinary character here, exactly as MQL treats it: there is no
    // escape sequence, and a value carrying a quote is delimited with the other quote
    // character instead. Treating '\' as an escape would both mangle a value containing
    // one and hide the unterminated-value case that MQL actually fails on.
    public class MqlStatementParser
    {
        public static List<ScriptStatement> Parse(string script)
        {
            var statements = new List<ScriptStatement>();
            if (string.IsNullOrEmpty(script)) return statements;

            var value = new StringBuilder();
            var quotedValues = new List<string>();
            bool inQuote = false;
            char quoteChar = '\0';
            int start = 0;

            for (int i = 0; i < script.Length; i++)
            {
                char c = script[i];

                if (!inQuote && (c == '\'' || c == '"'))
                {
                    inQuote = true;
                    quoteChar = c;
                    value.Length = 0;
                    continue;
                }

                if (inQuote && c == quoteChar)
                {
                    inQuote = false;
                    quotedValues.Add(value.ToString());
                    continue;
                }

                if (!inQuote && c == ';')
                {
                    Add(statements, script, start, i, quotedValues, true, false);
                    start = i + 1;
                    quotedValues = new List<string>();
                    continue;
                }

                if (inQuote) value.Append(c);
            }

            // Whatever follows the last ';' was never terminated. A value still open at the
            // end of the script never closed its quote.
            if (inQuote) quotedValues.Add(value.ToString());
            Add(statements, script, start, script.Length, quotedValues, false, inQuote);

            return statements;
        }

        // start and end bound the statement within the original script. The offset recorded
        // is its first non-whitespace character, so a caller can locate the statement
        // exactly -- searching for its text instead finds the first copy of a duplicate,
        // which is the wrong one precisely when a duplicate is what was reported.
        private static void Add(List<ScriptStatement> statements, string script, int start, int end,
            List<string> quotedValues, bool terminated, bool hasUnclosedQuote)
        {
            while (start < end && char.IsWhiteSpace(script[start])) start++;
            while (end > start && char.IsWhiteSpace(script[end - 1])) end--;
            if (end <= start) return;

            statements.Add(new ScriptStatement(script.Substring(start, end - start), start,
                statements.Count + 1, terminated, hasUnclosedQuote, quotedValues));
        }
    }
}
