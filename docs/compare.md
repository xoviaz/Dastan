# Comparing with ENOVIA

**Dastan → Compare With ENOVIA Export**

Lines your model up against a schema export taken from the target system and lists
everything the two disagree about — objects the model is missing, objects the target
system has never been given, and objects that exist on both sides but say different
things.

This is the only feature that looks at what is actually installed. It does it by reading a
file you export yourself, not by connecting: Dastan still never talks to ENOVIA.

## Producing the export

In MQL, as a System Administrator, in native language mode:

```mql
export admin CW_* xml into file C:/temp/schema.xml;
```

`admin` covers every kind of administrative definition at once. Narrow the pattern to your
own prefix — exporting the whole default schema gives you several thousand objects, none
of which are in your model, and the result will be a very long list of things that are not
your problem.

To compare one kind only:

```mql
export type CW_* xml into file C:/temp/types.xml;
```

!!! warning "`ematrixml.dtd` must sit beside the file"
    An export refers to entities declared in `ematrixml.dtd` and **cannot be read without
    it**. Copy it out of the `XML` folder of the ENOVIA installation that produced the
    export and put it in the same folder as the `.xml`. Dastan says so explicitly if it is
    missing.

The export format has not changed since V6R2008 — an R2024x file still declares that
version — so one reader handles every release. Anything in the file Dastan does not
recognise is counted and reported rather than treated as an error.

## What gets compared

Whatever is selected in the Project Browser:

| Selection | Scope |
|---|---|
| A package | the package and everything inside it, recursively |
| An element | the package that holds it |
| Nothing useful | the whole model |

Five kinds are compared: **types**, **attributes**, **relationships**, **policies** and
**roles**. Objects are matched by name, ignoring case.

!!! important "Only the kinds the export actually contains"
    An export holding nothing but `<type>` elements says nothing whatever about
    relationships, roles or attributes — so those kinds are left out of the comparison
    entirely rather than reported as missing from ENOVIA. The window and the Output log
    both name the kinds that were left out and how many modelled objects they cover.

    This is why `export admin CW_* xml` is usually what you want: an `export type` file
    can only ever tell you about types.

| Kind | Fields |
|---|---|
| Type | description, abstract, parent, attribute list |
| Attribute | description, primitive type, default, max length, multiline, reset on clone, reset on revision, ranges |
| Relationship | description, abstract, prevent duplicates, parent, attribute list, and for each end: cardinality, types, revision, clone, propagate connection, propagate modify |
| Policy | description, store, sequence, default format, types, formats, states |
| Role | description |

## The results window

Each row is one disagreement. An object that differs in three fields is three rows, so you
can see each one on its own.

- **Double-click a row** — or press Enter — to select that object in the Project Browser.
  For an attribute this lands on the **type that owns it**. A row for something that only
  exists in ENOVIA has nothing to select, which is the point of the row.
- The **dropdown** narrows the list to one category.
- **Copy** puts the visible rows on the clipboard as text.

Everything is also written to the *Script Generator Log* tab in EA's Output window,
prefixed `[Schema Comparison]`, so it outlives the window.

| Colour | Meaning |
|---|---|
| Red — *Only in ENOVIA* | In the target system, not in your model. Something was changed in ENOVIA directly, or your model was never given it. |
| Blue — *Only in the model* | Modelled but not installed. Often simply means you have not run the script yet. |
| Amber — *Different* | On both sides, saying different things. |

## What is deliberately not a difference

The two systems word the same fact differently, and reporting that as drift would bury the
real findings. These are all treated as agreement:

- **Whitespace and line endings** in a description.
- **The order of a type's attributes.** The same attributes listed differently is the same
  type.
- **Max length 0, blank, and absent.** All three mean unlimited.
- **`1` and `one`, `n` and `many`.** MQL writes `one` and `many`, an export writes `1` and
  `n`, and EA writes `1` and `*`. Three vocabularies, the same two facts.
- **Tags you left blank.** A policy with no `Store` tag still installs with `STORE`, so
  that is what it is compared as. The same goes for `generic` formats, a `-` sequence and
  `none` clone and revision actions.

## What *is* a difference, and may surprise you

- **The order of a policy's states.** States are a lifecycle; the same states in a
  different order are a different policy.
- **A name differing only in case.** MQL names are case-sensitive, so `CW_Widget` and
  `cw_widget` are two different objects. Dastan pairs them up anyway and reports it as a
  `name` difference, which is more useful than showing each as missing from the other side.
- **A relationship end that accepts several types.** One end of a modelled relationship is
  one class, so a relationship in ENOVIA that accepts several types on an end will always
  report as different. That is true — the model does not say so.
- **A `0..1` multiplicity in EA.** Anything that is not exactly `1` is generated as
  `many`, so that is what it is compared as — what the model would install, not what it
  might have meant.
- **A range with any operator other than `=`.** `Allowed Values` can only express equality.
  A `notequal` or `lessthan` range in the target system reports as drift because the model
  has no way to express it.
- **A primitive type spelled differently.** An attribute's type is compared exactly as
  written. ENOVIA says `string`, `boolean`, `integer`, `datetime` — write the same word in
  EA. This one is deliberately *not* normalised: the type is a value you choose, not a
  fact the two systems are forced to word differently, so a mismatch is a modelling error
  and worth seeing.

## What is not compared at all

- **Business objects.** Only schema definitions are read. Instance data in the export is
  skipped.
- **Triggers, programs, forms, tables, interfaces** and every other admin type. They are
  counted in the Output log so you know they were in the file and took no part.
- **Multi Value.** The export format has no element for it — only an undocumented
  `attrValueType` number — so there is nothing to compare against. It is left out rather
  than guessed at.

!!! note "A clean result is only as complete as the export"
    The comparison is against a file, not a live system; it can only speak about the five
    kinds it reads, and only about the ones your export actually contains. "The model and
    the export agree" always comes with a count of what was compared and a list of what
    was left out — read both before treating it as a clean bill of health.
