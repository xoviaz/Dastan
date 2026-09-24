using System;
using System.Collections.Generic;
using System.Text;

namespace Dastan.org.ed.ea.tool
{
    // Renders an MQL script as RTF for the preview window: keywords and quoted values
    // coloured, and flagged statements given a background.
    //
    // The RTF is built in one pass and handed to RichTextBox.Rtf as a single assignment.
    // Colouring the control the obvious way instead -- select a span, set its colour,
    // repeat -- repaints on every step and takes seconds on a real export.
    public static class MqlRtfBuilder
    {
        private const int ColorText = 1;
        private const int ColorKeyword = 2;
        private const int ColorValue = 3;
        private const int HighlightFlagged = 4;

        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "add", "modify", "connect", "disconnect", "delete", "start", "commit", "transaction",
            "bus", "attribute", "property", "connection", "relationship", "type", "policy", "vault",
            "state", "allstate", "program", "input", "filter", "range", "on", "to", "from", "value",
            "description", "default", "derived", "abstract", "cardinality", "one", "many",
            "clone", "revision", "format", "defaultformat", "store", "minorsequence",
            "minorrevision", "majorrevision", "version", "promote", "maxlength",
            "propagateconnection", "notpropagateconnection", "propagatemodify", "notpropagatemodify",
            "preventduplicate", "notpreventduplicate", "multiline", "notmultiline",
            "multivalue", "notmultivalue", "resetonclone", "notresetonclone",
            "resetonrevision", "notresetonrevision", "true", "false", "owner", "public"
        };

        public static string Build(string text, bool[] flagged)
        {
            var rtf = new StringBuilder(text.Length * 2);
            rtf.Append(@"{\rtf1\ansi\ansicpg1252\deff0{\fonttbl{\f0\fmodern\fcharset0 Consolas;}}");
            rtf.Append(@"{\colortbl ;\red32\green32\blue32;\red0\green0\blue192;\red163\green21\blue21;\red255\green212\blue212;}");
            rtf.Append(@"\viewkind4\uc1\f0\fs19 ");

            int currentColor = -1;
            bool highlighted = false;
            int i = 0;

            while (i < text.Length)
            {
                bool flagHere = i < flagged.Length && flagged[i];
                if (flagHere != highlighted)
                {
                    rtf.Append(flagHere ? @"\highlight" + HighlightFlagged + " " : @"\highlight0 ");
                    highlighted = flagHere;
                }

                char c = text[i];
                int start = i;
                int color;

                if (c == '\'' || c == '"')
                {
                    i = EndOfQuoted(text, i) + 1;
                    color = ColorValue;
                }
                else if (IsWordChar(c))
                {
                    while (i < text.Length && IsWordChar(text[i])) i++;
                    color = Keywords.Contains(text.Substring(start, i - start)) ? ColorKeyword : ColorText;
                }
                else
                {
                    i++;
                    color = ColorText;
                }

                if (color != currentColor)
                {
                    rtf.Append(@"\cf").Append(color).Append(' ');
                    currentColor = color;
                }

                for (int at = start; at < i; at++)
                    AppendChar(rtf, text[at]);
            }

            if (highlighted) rtf.Append(@"\highlight0 ");
            rtf.Append('}');
            return rtf.ToString();
        }

        private static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '.';
        }

        // Stops at the end of the line for an unterminated value, so one bad quote does
        // not colour the rest of the script as a string.
        private static int EndOfQuoted(string text, int start)
        {
            char quote = text[start];
            for (int i = start + 1; i < text.Length; i++)
            {
                if (text[i] == quote) return i;
                if (text[i] == '\n') return i - 1;
            }

            return text.Length - 1;
        }

        private static void AppendChar(StringBuilder rtf, char c)
        {
            switch (c)
            {
                case '\\': rtf.Append(@"\\"); return;
                case '{': rtf.Append(@"\{"); return;
                case '}': rtf.Append(@"\}"); return;
                case '\r': return;
                case '\n': rtf.Append(@"\par").Append('\n'); return;
            }

            if (c < 128)
            {
                rtf.Append(c);
                return;
            }

            // Persian label text lands here, so the escape has to be the Unicode form.
            // RTF wants a signed 16-bit code point.
            int code = c > 32767 ? c - 65536 : c;
            rtf.Append(@"\u").Append(code).Append('?');
        }
    }
}
