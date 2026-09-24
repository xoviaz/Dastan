# Checking the model

**Dastan → Validate Model**

Runs a set of checks over your model and lists what would go wrong, before any script
exists. The [script check](preview.md) catches most of the same problems — but only once
you have generated something, when fixing means going back into EA and generating again.
This catches them while they are still one edit away.

## What gets checked

Whatever is selected in the Project Browser:

| Selection | Scope |
|---|---|
| A package | the package and everything inside it, recursively |
| An element | that element, its attributes and its child elements |
| A diagram | every element on it |
| Nothing useful | the whole model |

Model furniture is left out. Notes, boundaries, the elements the Jira sync creates and the
log artifact are all skipped, so the results are about things that would actually be
generated — or that should have been and were not.

## The results window

Each row is one problem. It stays open while you fix things, so you can work through the
list without closing it:

- **Double-click a row** — or press Enter — to select that object in the Project Browser.
  For an attribute this lands on the **type that owns it**; the *Object* column tells you
  which attribute.
- **Errors only** hides the warnings.
- **Copy** puts the visible rows on the clipboard as text.

Everything is also written to the *Script Generator Log* tab in EA's Output window, prefixed
`[Model Check]`, so it outlives the window.

## Errors

These mean the generated MQL will fail, or that nothing will be generated at all.

| Rule | What it means |
|---|---|
| `missing-name` | Something that would be generated has no name. |
| `quote-in-identifier` | A name, type, store or revision contains `"` or `'`. MQL rejects a quote in any identifier and has no way to escape one — see [How MQL handles quotes](mql-notes.md). |
| `space-in-unquoted-value` | A value that is written into the script without quotes contains a space, which ends it early. Attribute allowed values and policy state names are the two cases. |
| `not-a-number` | A tag that has to be a whole number is not one. `Max Length` is the case. |
| `duplicate-name` | Two elements of the same stereotype share a name in the same scope, so the script creates the object twice and the second `add` fails. |
| `conflicting-duplicate` | Two attributes share a name but are defined differently. Only the first one reached is written, so one definition is silently dropped. |

!!! note "States are namespaced"
    A state's name only has to be unique within its own policy, so two policies may each
    have a `Preliminary` state without either being reported.

## Warnings

These mean the script will run, but the result is not what the model says.

| Rule | What it means |
|---|---|
| `missing-stereotype` | A class, state machine or attribute has no stereotype, so every generator skips it and nothing is written for it. |
| `missing-display-name` | An object that writes a line into the `.properties` file has no display name, so the line ends at the `=` and the target system falls back to the internal name. |

`missing-stereotype` is the one worth reading carefully. It is the hardest export problem to
diagnose from the output, because there is no output.

## When it is worth running

Before every generation is cheap and reasonable. It is most valuable:

- after adding anything by hand rather than from a pattern or template
- after pasting a name in from a spreadsheet or a document, which is how quotes get in
- before a release, run over the whole package rather than one element

## If the warnings are noisy

In a model that mixes PLM elements with ordinary UML, every unstereotyped class draws a
`missing-stereotype` warning. Two things help: tick **Errors only**, and select the package
you are working in rather than validating the whole model.
