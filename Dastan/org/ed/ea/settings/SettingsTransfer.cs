using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dastan.org.ed.ea.settings
{
    // Moves Dastan's configuration between machines as a file.
    //
    // Settings live in the registry under the Windows account that set them, so a new
    // machine or a new teammate starts from nothing. This writes the portable part out
    // and reads it back.
    public static class SettingsTransfer
    {
        private const string Format = "dastan-settings";
        private const int Version = 1;

        private class Field
        {
            public string Name { get; }
            public Func<string> Read { get; }
            public Action<string> Write { get; }

            public Field(string name, Func<string> read, Action<string> write)
            {
                Name = name;
                Read = read;
                Write = write;
            }
        }

        // What travels.
        //
        // The Jira token is deliberately absent. It is a personal credential rather than
        // shared configuration; it is encrypted with DPAPI against one Windows account,
        // so a copy would be unreadable anywhere else anyway; and writing it out in the
        // clear would put a secret into a file whose whole purpose is to be passed
        // around. Whoever imports enters their own.
        //
        // Three more are absent because they describe this machine rather than how the
        // add-in is set up: the last Jira sync time (importing someone else's would make
        // an incremental scan skip issues), the preview window's geometry, and the
        // changeset currently being worked on.
        private static readonly List<Field> Portable = new List<Field>
        {
            Text("OutputFolder", () => AppSettings.DefaultOutputFolder, v => AppSettings.DefaultOutputFolder = v),
            Text("StringResourcePrefix", () => AppSettings.StringResourcePrefix, v => AppSettings.StringResourcePrefix = v),
            Flag("AutoOpenExport", () => AppSettings.AutoOpenExport, v => AppSettings.AutoOpenExport = v),
            Flag("TrackModifications", () => AppSettings.TrackModifications, v => AppSettings.TrackModifications = v),
            Flag("SkipPreviewWhenClean", () => AppSettings.SkipPreviewWhenClean, v => AppSettings.SkipPreviewWhenClean = v),

            Text("JiraDomain", () => AppSettings.JiraDomain, v => AppSettings.JiraDomain = v),
            Text("JiraProject", () => AppSettings.JiraProject, v => AppSettings.JiraProject = v),
            Text("JiraPageSize", () => AppSettings.JiraPageSize, v => AppSettings.JiraPageSize = v),
            Flag("JiraScanIncremental", () => AppSettings.JiraScanIncremental, v => AppSettings.JiraScanIncremental = v),
            Text("JiraTaskType", () => AppSettings.JiraTaskType, v => AppSettings.JiraTaskType = v),
            Text("JiraCustomJql", () => AppSettings.JiraCustomJql, v => AppSettings.JiraCustomJql = v),

            Text("JiraDraftEquivalent", () => AppSettings.JiraDraftEquivalent, v => AppSettings.JiraDraftEquivalent = v),
            Text("JiraToDoEquivalent", () => AppSettings.JiraToDoEquivalent, v => AppSettings.JiraToDoEquivalent = v),
            Text("JiraInProgressEquivalent", () => AppSettings.JiraInProgressEquivalent, v => AppSettings.JiraInProgressEquivalent = v),
            Text("JiraInReviewEquivalent", () => AppSettings.JiraInReviewEquivalent, v => AppSettings.JiraInReviewEquivalent = v),
            Text("JiraDoneEquivalent", () => AppSettings.JiraDoneEquivalent, v => AppSettings.JiraDoneEquivalent = v)
        };

        public class ImportResult
        {
            public int Applied { get; set; }

            // Names in the file that this version knows nothing about -- a newer Dastan
            // wrote it, or somebody edited it by hand.
            public List<string> Unknown { get; } = new List<string>();

            // Portable settings the file simply did not mention. Those are left as they
            // are rather than reset, so a partial file tops up rather than wipes.
            public List<string> Missing { get; } = new List<string>();
        }

        public static void Export(string path)
        {
            var values = new JObject();
            foreach (Field field in Portable)
                values[field.Name] = field.Read() ?? "";

            var document = new JObject
            {
                ["format"] = Format,
                ["version"] = Version,
                ["exported"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["settings"] = values
            };

            // No BOM: the file is meant to be readable and diffable by anything.
            File.WriteAllText(path, document.ToString(Formatting.Indented), new UTF8Encoding(false));
        }

        public static ImportResult Import(string path)
        {
            JObject document;
            try
            {
                document = JObject.Parse(File.ReadAllText(path));
            }
            catch (JsonException e)
            {
                throw new InvalidDataException("This file is not readable as JSON: " + e.Message);
            }

            if (!string.Equals((string) document["format"], Format, StringComparison.Ordinal))
                throw new InvalidDataException("This is not a Dastan settings file.");

            JObject values = document["settings"] as JObject;
            if (values == null)
                throw new InvalidDataException("This settings file has no settings in it.");

            var result = new ImportResult();
            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Field field in Portable)
                known.Add(field.Name);

            foreach (KeyValuePair<string, JToken> pair in values)
            {
                if (!known.Contains(pair.Key)) result.Unknown.Add(pair.Key);
            }

            foreach (Field field in Portable)
            {
                JToken token = values[field.Name];
                if (token == null)
                {
                    result.Missing.Add(field.Name);
                    continue;
                }

                field.Write(token.Type == JTokenType.Null ? "" : token.ToString());
                result.Applied++;
            }

            return result;
        }

        private static Field Text(string name, Func<string> read, Action<string> write)
        {
            return new Field(name, read, write);
        }

        private static Field Flag(string name, Func<bool> read, Action<bool> write)
        {
            return new Field(name,
                () => read() ? "true" : "false",
                value => write(string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
