# Delta scripts

A delta script contains only what changed, rather than re-creating everything. It is built
from rows in the [modification log](modification-log.md).

## Generating one

1. Open the Modification Logs panel.
2. Choose the rows — select them by hand, or name a changeset to select the whole set.
3. Press **Generate Script**.
4. Read the [preview](preview.md) and accept or cancel.

**Writes:** `Modified_Output_<timestamp>.txt`, or `Modified_Output_<changeset>_<timestamp>.txt`
when every selected row belongs to one changeset.

## What each row becomes

| Row | Statement |
|---|---|
| A changed property or tag | `modify <stereotype> <name> <clause>;` |
| A tag MQL has a word for | that word — `Multi Line = True` becomes `multiline`, not `"Multi Line" "True"` |
| `Connection` | `connect bus ... - relationship ... to ... -;` |
| `Disconnection` | `disconnect bus ... - relationship ... to ... -;` |
| `Attribute added` | the attribute's full definition, then `modify <type> ... add attribute ...;` |
| `Attribute removed` | `modify <type> ... remove attribute ...;` |

Widget-profile elements produce `modify bus ...` instead; tooltips and labels produce their
own text-helper statements.

## New attributes

Attaching an attribute to a type means the attribute is being created, and MQL cannot attach
one that does not exist yet. So the script contains **both** the definition and the
attachment:

```mql
add attribute "CW_Href" type "string" description "Link target" default "" notmultiline notmultivalue maxlength 128 notresetonclone notresetonrevision;
modify "Type" "CW_Widget" add attribute "CW_Href";
```

Three things follow from that, and they are worth knowing:

**Definitions are written first, all of them, before any other statement.** Nothing can
reference an attribute before it is defined, whatever order you selected the rows in.

**The definition is read from the attribute as it stands now**, not from the logged row. If
you added an attribute and then changed its type and its tags, the definition carries the
final values — and the changes that got it there are left out, since restating them would
repeat the definition at best and fail at worst. MQL will not change an attribute's type
once it exists.

**One definition per attribute.** Attaching the same attribute to three types in one
changeset gives three attachment statements and one definition.

!!! warning "Attaching an attribute that already exists"
    If you attach an attribute that is **already in the target system** rather than creating
    a new one, the `add attribute` will fail and roll back the transaction. The preview shows
    the statement before anything is written.

## What is tracked but not generated

Some changes are recorded for the history and produce no statement, because MQL has no single
clause for them:

- **Operations** — nothing in the schema MQL corresponds to them
- **Attribute allowed values** — a range list is changed by adding and removing entries
- **Policy `Type` and `Format`** — lists, the same way
- **Policy `Display Name`** — goes to the `.properties` file, never to a statement
- **Relationship `From`/`To` clone, revision and propagate tags** — these belong to one end
  of the relationship and need a qualifier

The rows stay in the log so the history is complete; they simply generate nothing.

## A known gap

If you add an attribute, let the change be logged, and **then rename it**, the logged row
still holds the old name. The definition lookup misses and no definition is written for it.

Adding and configuring an attribute in one properties-dialog session is fine — that logs a
single row with the final name. The gap only opens if the rename happens later.

## Delta or full?

A delta script is the right tool when the target system already has the model and you are
shipping a change to it. Use a [full generator](generating.md) when you are installing
something for the first time, or when the log does not cover the whole of what you changed
— for instance because tracking was off, or because the changes predate the log.
