# Preview and script check

Every generated script passes through one gate before it reaches disk. Nothing bypasses it.

The window shows the **exact text** that will be written, with any problems listed beside
it. Closing it with **Cancel** writes nothing — not the script, not the `.properties` file,
and no completion message.

## The window

- **The script**, syntax-highlighted, with flagged statements on a pink background.
- **The findings list**, when there are any. Click one to jump to the statement it is about
  — it locates that exact statement, even when two statements read identically.
- **Ctrl+F** opens a find bar; **F3** finds the next match.
- **Copy** puts the whole script on the clipboard.
- **Wrap long lines** toggles wrapping.
- **Skip preview when the script is clean** suppresses the window entirely for scripts with
  no findings. Anything with a finding still stops and waits for you.

Window size, splitter position and the wrap setting are remembered between runs.

Findings are also written to the *Script Generator Log* tab in EA's Output window, prefixed
`[Script Check]`, so they outlive the window even when you go ahead with the export.

## The checks

The script is split into statements on `;`, respecting quotes, and each rule runs over the
result.

| Rule | What it means |
|---|---|
| `unterminated-statement` | A statement is not closed with `;`. |
| `unclosed-quote` | A quoted value is never closed. Usually a quote character inside a name — see [How MQL handles quotes](mql-notes.md). |
| `empty-name` | An object type or name is empty. |
| `quote-in-name` | An identifier contains `"` or `'`, which MQL rejects. |
| `duplicate-add` | The same object is created more than once in one script, so the second `add` fails. |

### Names versus free text

The checks distinguish the two, because the rules are different. MQL rejects a quote in any
name, type, policy, vault or revision — but a description, a label or a tag value may
legitimately contain one. So only the identifier positions of a statement are checked:

- a `connect` names a source, a target and the relationship joining them, so its first five
  values are identifiers
- an `add` or `modify` starts carrying free text from its third value onwards, so only the
  type and name are treated as identifiers
- `add property` and `add connection` create no named object, so neither is checked

## What it cannot tell you

The check reads the script, not your target system. It does not know:

- whether an object already exists there, so an `add` that will fail on a duplicate looks
  clean here
- whether a referenced object exists, unless this same script creates it
- whether the last script you ran succeeded

Those remain your judgement, which is the real reason to read the preview rather than to
trust an empty findings list.

## Turning the preview off

Ticking **Skip preview when the script is clean** is reasonable once a model is stable. It
only skips windows with nothing to report — a script with any finding still stops.

Everything still goes to the log tab either way, so you can look afterwards.
