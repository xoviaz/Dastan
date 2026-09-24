using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Dastan.org.ed.ea.repository;
using EA;

namespace Dastan.org.ed.ea.services.modifications
{
    public class ModificationLogsManager
    {
        // Subitem positions. A ListView maps columns to subitems by position, so the
        // ElementID -- which has no column -- has to sit past the last one to stay hidden,
        // and nothing may be inserted before Time.
        public const int NameIndex = 0;
        public const int TypeIndex = 1;
        public const int StereotypeIndex = 2;
        public const int PropertyIndex = 3;
        public const int OldValueIndex = 4;
        public const int NewValueIndex = 5;
        public const int TimeIndex = 6;
        public const int ChangesetIndex = 7;
        public const int ElementIdIndex = 8;

        private readonly ElementRepository _elementRepository = new ElementRepository();

        // The full log. The ListView shows whatever passes the current filter, so it
        // cannot be the store -- saving from it would drop every filtered-out row the
        // next time a modification is tracked.
        private readonly List<ListViewItem> _all = new List<ListViewItem>();

        private readonly ListView _items;
        private string _filter = "";
        private bool _loaded;

        public ModificationLogsManager()
        {
            _items = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            _items.Columns.Add("Element Name", 150);
            _items.Columns.Add("Type", 100);
            _items.Columns.Add("Stereotype", 100);
            _items.Columns.Add("Property", 120);
            _items.Columns.Add("Old", 150);
            _items.Columns.Add("New", 150);
            _items.Columns.Add("Time", 140);
            _items.Columns.Add("Changeset", 120);
        }

        public void AddItem(ListViewItem item)
        {
            if (item == null) return;

            _all.Add(item);
            if (PassesFilter(item)) _items.Items.Add(item);
        }

        // Narrows the visible rows to those containing the text, matched against every
        // field the user can see. An empty filter shows the whole log again.
        public void Filter(string text)
        {
            _filter = (text ?? "").Trim();
            ApplyFilter();
        }

        public void ClearAll()
        {
            _all.Clear();
            _items.Items.Clear();
        }

        // Out of the log, not just out of sight. The rows in _all are the same objects the
        // ListView holds, so each one is dropped from both -- removing it from the view
        // alone would leave it in the store to reappear on the next filter change.
        //
        // The selection is copied first because removing from a ListView mutates the
        // collection being walked.
        public void RemoveSelected()
        {
            Remove(SelectedItems());
        }

        public void Remove(IReadOnlyList<ListViewItem> rows)
        {
            if (rows == null || rows.Count == 0) return;

            _items.BeginUpdate();
            try
            {
                foreach (ListViewItem item in rows)
                {
                    _all.Remove(item);
                    _items.Items.Remove(item);
                }
            }
            finally
            {
                _items.EndUpdate();
            }
        }

        // A copy, because removing from a ListView mutates the live collection and a
        // caller that walks it while acting on it skips rows.
        public List<ListViewItem> SelectedItems()
        {
            var selected = new List<ListViewItem>();
            foreach (ListViewItem item in _items.SelectedItems)
                selected.Add(item);

            return selected;
        }

        private void ApplyFilter()
        {
            _items.BeginUpdate();
            try
            {
                _items.Items.Clear();
                foreach (ListViewItem item in _all)
                {
                    if (PassesFilter(item)) _items.Items.Add(item);
                }
            }
            finally
            {
                _items.EndUpdate();
            }
        }

        private bool PassesFilter(ListViewItem item)
        {
            if (_filter.Length == 0) return true;

            // Everything except the ElementID, which has no column and means nothing to
            // someone typing a search.
            int last = Math.Min(item.SubItems.Count, ElementIdIndex);
            for (int i = 0; i < last; i++)
            {
                string text = item.SubItems[i].Text;
                if (text != null && text.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }

            return false;
        }

        // The name is passed in rather than read off the element, because a row is not
        // always about the element itself -- an attribute or an operation that changed is
        // named in the script by its own name, and that is what has to reach column 0.
        //
        // The columns are filled whether or not the element still exists, so a row whose
        // element has since been deleted keeps its history instead of being reduced to a
        // single empty cell the next time the log is saved.
        public ListViewItem GenerateItem(Element element, string name, params string[] parameters)
        {
            string display = string.IsNullOrEmpty(name) ? element?.Name : name;

            ListViewItem item = new ListViewItem(string.IsNullOrEmpty(display) ? "(deleted)" : display);
            foreach (string parameter in parameters)
            {
                item.SubItems.Add(parameter ?? "");
            }

            item.Tag = element;
            return item;
        }

        public ListView GetItems()
        {
            return _items;
        }

        // Distinct changeset names present in the log, for the panel's picker.
        public List<string> ChangesetNames()
        {
            var names = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // From the full log, so filtering the view does not empty the picker.
            foreach (ListViewItem item in _all)
            {
                string name = SubItem(item, ChangesetIndex);
                if (name.Length == 0 || !seen.Add(name)) continue;

                names.Add(name);
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        // Selects every row filed under the given changeset, so Generate Script acts on a
        // whole set instead of whatever happened to be highlighted. Matches against the
        // visible rows, so it combines with the search filter. Returns false and leaves
        // the selection untouched when no visible row carries the name -- naming a brand
        // new changeset should not wipe a selection the user just made by hand.
        public bool SelectChangeset(string changeset)
        {
            if (string.IsNullOrEmpty(changeset)) return false;

            bool any = false;
            foreach (ListViewItem item in _items.Items)
            {
                if (!Matches(item, changeset)) continue;

                any = true;
                break;
            }

            if (!any) return false;

            _items.BeginUpdate();
            try
            {
                ListViewItem first = null;
                foreach (ListViewItem item in _items.Items)
                {
                    bool match = Matches(item, changeset);
                    item.Selected = match;
                    if (match && first == null) first = item;
                }

                if (first != null) first.EnsureVisible();
            }
            finally
            {
                _items.EndUpdate();
            }

            return true;
        }

        // No changeset named means no scope. Visible rows only -- a filter deliberately
        // narrows what Generate Script can act on, since you cannot export what you
        // cannot see.
        public void SelectAll()
        {
            _items.BeginUpdate();
            try
            {
                foreach (ListViewItem item in _items.Items)
                    item.Selected = true;
            }
            finally
            {
                _items.EndUpdate();
            }
        }

        private static bool Matches(ListViewItem item, string changeset)
        {
            return string.Equals(SubItem(item, ChangesetIndex), changeset, StringComparison.OrdinalIgnoreCase);
        }

        public void EnsureLoaded(Repository repository)
        {
            if (_loaded) return;
            LoadItems(repository);
        }

        public void LoadItems(Repository repository)
        {
            Element targetElement = _elementRepository.FindModificationLogsElement(repository, out bool repositoryReady);
            if (repositoryReady) _loaded = true;

            if (targetElement == null) return;

            ClearAll();

            foreach (string line in (targetElement.Notes ?? "")
                     .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] p = line.Split('|');

                // Expect: Name|Type|Stereotype|Property|Old|New|Time|Changeset|ElementID
                if (p.Length < 7) continue;

                for (int i = 0; i < p.Length; i++)
                    p[i] = Unescape(p[i]);

                // Rows written before changesets existed end at the ElementID, so a
                // nine-field row carries one and an eight-field row does not.
                string changeset = p.Length >= 9 ? p[7] : "";
                string elementIdText = p.Length >= 9 ? p[8] : (p.Length >= 8 ? p[7] : "");

                Element element = null;
                if (int.TryParse(elementIdText, out int elementId))
                {
                    element = _elementRepository.SafeGetElementById(repository, elementId);
                }

                // The stored name, not the element's current one: the log says what was
                // changed at the time, and for an attribute row there is no element whose
                // name would be right anyway.
                ListViewItem item = GenerateItem(
                    element,
                    p[0], // Name
                    p[1], // Type
                    p[2], // Stereotype
                    p[3], // Property
                    p[4], // Old
                    p[5], // New
                    p[6], // Time
                    changeset,
                    elementIdText
                );

                // Keep id even if element is gone
                if (item.Tag == null)
                    item.Tag = elementIdText;

                AddItem(item);
            }
        }

        public void SaveItems(Repository repository)
        {
            Element targetElement = _elementRepository.GetOrCreateModificationLogsElement(repository);
            if (targetElement == null) return;

            // From the full log, never the visible rows -- a filter must not be able to
            // delete what it is hiding.
            var builder = new StringBuilder();
            foreach (ListViewItem item in _all)
            {
                var parts = new List<string>();
                foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
                    parts.Add(Escape(subItem.Text ?? ""));

                builder.AppendLine(string.Join("|", parts));
            }

            targetElement.Notes = builder.ToString();
            targetElement.Update();
        }

        // A row built from a deleted element carries only its first subitem, so every
        // read past that has to tolerate a short row.
        private static string SubItem(ListViewItem item, int index)
        {
            return index < item.SubItems.Count ? item.SubItems[index].Text ?? "" : "";
        }

        // '|' delimits fields and newlines delimit rows, so both — plus the escape
        // character itself — must be escaped before a field is written, and reversed on load.
        private static string Escape(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("|", "\\p")
                .Replace("\r\n", "\\n")
                .Replace("\r", "\\n")
                .Replace("\n", "\\n");
        }

        private static string Unescape(string value)
        {
            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\\' && i + 1 < value.Length)
                {
                    char next = value[i + 1];
                    if (next == '\\') { sb.Append('\\'); i++; continue; }
                    if (next == 'p') { sb.Append('|'); i++; continue; }
                    if (next == 'n') { sb.Append('\n'); i++; continue; }
                }
                sb.Append(value[i]);
            }
            return sb.ToString();
        }
    }
}
