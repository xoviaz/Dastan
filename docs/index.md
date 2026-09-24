# Dastan

Dastan is a Sparx Enterprise Architect add-in that turns an EA model into MQL for an
ENOVIA / Matrix-One target system.

You model types, attributes, relationships, roles, policies and widgets in EA using the
Dastan MDG profiles, and Dastan writes the MQL that creates them. It also watches what you
change as you work, so a release can be a script of just the differences rather than a
full re-install.

## The thing to understand first

**MQL generation is one-way and blind.** The script goes out; nothing comes back. Dastan
never reads the target system, so it cannot tell you whether a type already exists, whether
an attribute is in use, or whether the last script actually ran.

Everything in this manual is arranged around that gap. Three features exist purely to
close it, and they are worth using every time:

- [Checking the model](validate.md) catches problems while you are still in EA, where a
  fix is one edit.
- [Preview and script check](preview.md) shows you the exact text before it is written, with
  problems listed beside it.
- [Comparing with ENOVIA](compare.md) tells you where the model and the target system have
  drifted apart — by reading an export file you produce, since nothing can be read back
  directly.
- [Importing from ENOVIA](importing.md) goes the other way, creating model elements from
  that same export file.

Both matter more than they sound, because every script Dastan writes is wrapped in a single
transaction:

```mql
start transaction;
...
commit transaction;
```

One bad statement rolls back the whole batch, and MQL's error rarely names the real cause.

## A normal working session

1. **Model** the change in EA — add a type, an attribute, a state. See
   [Profiles and tagged values](modelling.md).
2. **Check the model** with *Dastan → Validate Model*. See [Checking the model](validate.md).
3. **Generate** — either a full script for what you selected, or a delta script from the
   changes Dastan tracked. See [The generators](generating.md) and
   [Delta scripts](delta-scripts.md).
4. **Review** what appears in the preview window, and cancel if it is not what you meant.
5. **Run** the file against your target system yourself. Dastan does not execute anything.

## Where everything lives

| | |
|---|---|
| Menu | **Dastan** in the EA main menu, and in the *Develop* ribbon category |
| Keyboard | Every menu item has an access letter: **S**cript, **J**ira, **M**odification Logs Panel, **V**alidate Model, **C**ompare With ENOVIA Export, **I**mport From ENOVIA Export, Install **P**rofiles, S**e**ttings, then **S**chema, **U**I, **P**olicy, **T**rigger, **N**ame Generator inside Script |
| Output files | The folder set in [Settings](settings.md), named `<Kind>_Output_<timestamp>.txt` |
| Modification log | Stored inside the model itself, in an artifact named `DastanModificationLogs` in the root package |
| Settings | Windows registry, `HKEY_CURRENT_USER\Software\DastanSettings` |
| Messages | The *Script Generator Log* tab in EA's Output window |

## What Dastan does not do

- It does not connect to ENOVIA or run MQL. Nothing is ever read back from the target
  system directly.
- It only knows what is installed there if you hand it an export file to
  [compare against](compare.md), and then only as of the moment that file was written.
- It does not version or branch scripts — the timestamped filenames are all the history
  there is.

If a generated script fails, the cause is in the model or in the target system's existing
state, and the script text in your output folder is the evidence to work from.
