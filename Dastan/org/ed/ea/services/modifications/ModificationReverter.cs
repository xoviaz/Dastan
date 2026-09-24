using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Dastan.org.ed.ea.commands;
using EA;
using Attribute = EA.Attribute;

namespace Dastan.org.ed.ea.services.modifications
{
    // Writes a logged old value back onto the thing it came from.
    //
    // A log row already holds everything an undo needs -- the element, what changed, and
    // what it was before -- so this is the cheapest possible way to take back a mistake
    // without hunting for it in the model.
    public class ModificationReverter
    {
        public class Result
        {
            // The rows that were actually written back. A reverted row no longer describes
            // the model, and leaving it in the log would make the next delta script
            // re-apply the very change that was just undone -- so the caller drops these.
            public readonly List<ListViewItem> Reverted = new List<ListViewItem>();

            // One line per row that was left alone, saying why.
            public readonly List<string> Skipped = new List<string>();
        }

        // Not a value that can be written back. Undoing one of these means creating or
        // deleting an attribute, an operation or a connector, which is a modelling
        // decision rather than an undo.
        private static readonly HashSet<string> Structural = new HashSet<string>
        {
            ModificationProperties.Connection,
            ModificationProperties.Disconnection,
            ModificationProperties.AttributeAdded,
            ModificationProperties.AttributeRemoved,
            ModificationProperties.OperationAdded,
            ModificationProperties.OperationRemoved
        };

        private const int MemoLength = 255;

        private readonly ModifiedElementDetectionCommand _tracking;

        public ModificationReverter(ModifiedElementDetectionCommand tracking)
        {
            _tracking = tracking;
        }

        public Result Revert(IReadOnlyList<ListViewItem> rows)
        {
            var result = new Result();
            if (rows == null || rows.Count == 0) return result;

            // Newest first. Two rows that changed the same property have to be undone in
            // reverse order so the oldest value is the one left behind: undoing B to C and
            // then A to B ends at A, which is what reverting both should mean.
            for (int i = rows.Count - 1; i >= 0; i--)
                RevertRow(rows[i], result);

            return result;
        }

        private void RevertRow(ListViewItem row, Result result)
        {
            string name = Cell(row, ModificationLogsManager.NameIndex);
            string property = Cell(row, ModificationLogsManager.PropertyIndex);
            string oldValue = Cell(row, ModificationLogsManager.OldValueIndex);
            string newValue = Cell(row, ModificationLogsManager.NewValueIndex);

            if (Structural.Contains(property))
            {
                Skip(result, name, property, "a structural change has to be undone in the model itself");
                return;
            }

            Element element = row.Tag as Element;
            if (element == null)
            {
                Skip(result, name, property, "the element it belonged to no longer exists");
                return;
            }

            // Tracking is stopped around the write, or undoing a change would be logged as
            // a change of its own.
            _tracking.Suspend();
            try
            {
                if (Apply(element, name, property, oldValue, newValue, out string reason))
                    result.Reverted.Add(row);
                else
                    Skip(result, name, property, reason);
            }
            catch (Exception e)
            {
                Skip(result, name, property, e.Message);
            }
            finally
            {
                _tracking.Resume(element);
            }
        }

        // The row names what changed: the element itself, or one of its attributes or
        // operations. Looking the name up settles it, which also stays right when the
        // element has since been renamed and no longer matches its own row.
        private static bool Apply(Element element, string name, string property, string oldValue, string newValue,
            out string reason)
        {
            Attribute attribute = FindAttribute(element, name);
            if (attribute != null) return RevertAttribute(attribute, property, oldValue, newValue, out reason);

            Method method = FindMethod(element, name);
            if (method != null) return RevertMethod(method, property, oldValue, newValue, out reason);

            return RevertElement(element, property, oldValue, newValue, out reason);
        }

        private static bool RevertElement(Element element, string property, string oldValue, string newValue,
            out string reason)
        {
            Func<string> read;
            Action<string> write;

            switch (property)
            {
                case "name":       read = () => element.Name;       write = v => element.Name = v;       break;
                case "alias":      read = () => element.Alias;      write = v => element.Alias = v;      break;
                case "notes":      read = () => element.Notes;      write = v => element.Notes = v;      break;
                case "stereotype": read = () => element.Stereotype; write = v => element.Stereotype = v; break;
                default:
                    return RevertTag(element.TaggedValues, property, oldValue, newValue, out reason);
            }

            if (!StillMatches(read(), newValue, out reason)) return false;

            write(oldValue);
            element.Update();
            return true;
        }

        private static bool RevertAttribute(Attribute attribute, string property, string oldValue, string newValue,
            out string reason)
        {
            Func<string> read;
            Action<string> write;

            switch (property)
            {
                case "name":        read = () => attribute.Name;       write = v => attribute.Name = v;       break;
                case "alias":       read = () => attribute.Alias;      write = v => attribute.Alias = v;      break;
                case "description": read = () => attribute.Notes;      write = v => attribute.Notes = v;      break;
                case "stereotype":  read = () => attribute.Stereotype; write = v => attribute.Stereotype = v; break;
                case "type":        read = () => attribute.Type;       write = v => attribute.Type = v;       break;
                case "default":     read = () => attribute.Default;    write = v => attribute.Default = v;    break;
                default:
                    return RevertTag(attribute.TaggedValues, property, oldValue, newValue, out reason);
            }

            if (!StillMatches(read(), newValue, out reason)) return false;

            write(oldValue);
            attribute.Update();
            return true;
        }

        private static bool RevertMethod(Method method, string property, string oldValue, string newValue,
            out string reason)
        {
            Func<string> read;
            Action<string> write;

            switch (property)
            {
                case "name":        read = () => method.Name;       write = v => method.Name = v;       break;
                case "alias":       read = () => method.Alias;      write = v => method.Alias = v;      break;
                case "description": read = () => method.Notes;      write = v => method.Notes = v;      break;
                case "stereotype":  read = () => method.Stereotype; write = v => method.Stereotype = v; break;
                // An operation has no data type of its own; what it returns is what the
                // snapshot recorded as its type.
                case "type":        read = () => method.ReturnType; write = v => method.ReturnType = v; break;
                default:
                    return RevertTag(method.TaggedValues, property, oldValue, newValue, out reason);
            }

            if (!StillMatches(read(), newValue, out reason)) return false;

            write(oldValue);
            method.Update();
            return true;
        }

        // Elements, attributes and operations each carry their own tag type and share no
        // interface between them, so each is asked in turn -- the same shape the snapshot
        // reader uses.
        private static bool RevertTag(Collection tags, string property, string oldValue, string newValue,
            out string reason)
        {
            if (tags != null)
            {
                foreach (object tag in tags)
                {
                    TaggedValue elementTag = tag as TaggedValue;
                    if (elementTag != null && Named(elementTag.Name, property))
                    {
                        if (!StillMatches(Read(elementTag.Value, elementTag.Notes), newValue, out reason)) return false;

                        Write(oldValue, v => elementTag.Value = v, n => elementTag.Notes = n);
                        elementTag.Update();
                        return true;
                    }

                    AttributeTag attributeTag = tag as AttributeTag;
                    if (attributeTag != null && Named(attributeTag.Name, property))
                    {
                        if (!StillMatches(Read(attributeTag.Value, attributeTag.Notes), newValue, out reason))
                            return false;

                        Write(oldValue, v => attributeTag.Value = v, n => attributeTag.Notes = n);
                        attributeTag.Update();
                        return true;
                    }

                    MethodTag methodTag = tag as MethodTag;
                    if (methodTag != null && Named(methodTag.Name, property))
                    {
                        if (!StillMatches(Read(methodTag.Value, methodTag.Notes), newValue, out reason)) return false;

                        Write(oldValue, v => methodTag.Value = v, n => methodTag.Notes = n);
                        methodTag.Update();
                        return true;
                    }
                }
            }

            reason = "there is no longer a tagged value called '" + property + "' on it";
            return false;
        }

        // EA moves a long tagged value into the notes and leaves "<memo>" behind in the
        // value, so both halves have to be read and written the same way round.
        private static string Read(string value, string notes)
        {
            return value == "<memo>" ? notes ?? "" : value ?? "";
        }

        private static void Write(string value, Action<string> writeValue, Action<string> writeNotes)
        {
            string text = value ?? "";

            if (text.Length > MemoLength)
            {
                writeValue("<memo>");
                writeNotes(text);
                return;
            }

            writeValue(text);
        }

        // A row can only be undone while the value is still the one it put there. If
        // something changed it again afterwards, writing the old value back would silently
        // discard that later edit, so the row is left alone and the reason is reported.
        private static bool StillMatches(string current, string expected, out string reason)
        {
            if ((current ?? "") == (expected ?? ""))
            {
                reason = null;
                return true;
            }

            reason = "it was changed again afterwards, and now reads " + Quote(current);
            return false;
        }

        private static Attribute FindAttribute(Element element, string name)
        {
            if (element == null || string.IsNullOrEmpty(name)) return null;

            element.Attributes.Refresh();
            foreach (Attribute attribute in element.Attributes)
            {
                if (string.Equals(attribute.Name, name, StringComparison.Ordinal)) return attribute;
            }

            return null;
        }

        private static Method FindMethod(Element element, string name)
        {
            if (element == null || string.IsNullOrEmpty(name)) return null;

            element.Methods.Refresh();
            foreach (Method method in element.Methods)
            {
                if (string.Equals(method.Name, name, StringComparison.Ordinal)) return method;
            }

            return null;
        }

        private static bool Named(string tagName, string property)
        {
            return string.Equals(tagName, property, StringComparison.OrdinalIgnoreCase);
        }

        private static string Cell(ListViewItem row, int index)
        {
            return index < row.SubItems.Count ? row.SubItems[index].Text ?? "" : "";
        }

        private static void Skip(Result result, string name, string property, string reason)
        {
            result.Skipped.Add(Quote(name) + " " + property + " -- " + reason);
        }

        private static string Quote(string value)
        {
            string text = (value ?? "").Replace("\r\n", " ").Replace("\n", " ");
            if (text.Length > 40) text = text.Substring(0, 40) + "...";

            return "'" + text + "'";
        }
    }
}
