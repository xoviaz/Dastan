using System;
using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.lint
{
    // One MQL statement as the parser found it -- the text between two ';' separators,
    // where it starts in the script, and what the parser worked out while walking it.
    public class ScriptStatement
    {
        public string Text { get; }

        // Index of the statement's first character in the script that was parsed.
        public int Offset { get; }

        public int Number { get; }
        public bool Terminated { get; }
        public bool HasUnclosedQuote { get; }
        public IReadOnlyList<string> QuotedValues { get; }

        // First word of the statement, lowercased: add, connect, modify, start, commit.
        public string Verb { get; }

        // Word after the verb, lowercased. Tells "add bus" and "add attribute" apart from
        // "add property" and "add connection", which create no named object.
        public string Subject { get; }

        private static readonly HashSet<string> IdentifierVerbs =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "add", "connect", "modify" };

        // Quoted values that name something, as opposed to carrying free text. MQL rejects
        // a quote character in any name, but a description or a label legitimately contains
        // one, so the two cannot be checked alike.
        //
        // A connect names a source, a target and the relationship joining them, so its
        // first five values are all identifiers and only the tag pairs after them are not.
        // An add or modify starts carrying free text -- description, default, a tag value --
        // from the third value on, so only the type and name can be relied on there.
        public IEnumerable<string> IdentifierValues
        {
            get
            {
                if (!IdentifierVerbs.Contains(Verb)) yield break;
                if (Subject == "property" || Subject == "connection") yield break;

                int count = Math.Min(Verb == "connect" ? 5 : 2, QuotedValues.Count);
                for (int i = 0; i < count; i++)
                    yield return QuotedValues[i];
            }
        }

        public ScriptStatement(string text, int offset, int number, bool terminated, bool hasUnclosedQuote,
            IReadOnlyList<string> quotedValues)
        {
            Text = text;
            Offset = offset;
            Number = number;
            Terminated = terminated;
            HasUnclosedQuote = hasUnclosedQuote;
            QuotedValues = quotedValues;

            string[] words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Verb = words.Length > 0 ? words[0].ToLowerInvariant() : "";
            Subject = words.Length > 1 ? words[1].ToLowerInvariant() : "";
        }
    }
}
