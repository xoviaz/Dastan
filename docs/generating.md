# The generators

All five live under **Dastan → Script**. Each one reads what is selected in the Project
Browser, builds a script, shows it to you in the [preview window](preview.md), and writes it
to the folder set in [Settings](settings.md).

Menu items grey out when the selection is wrong for them, so the menu itself tells you what
applies to what.

Every script is wrapped in one transaction:

```mql
start transaction;
...
commit transaction;
```

which is why one bad statement takes the whole batch with it, and why the preview is worth
reading.

---

## Schema Script Generator

**Select:** an element stereotyped `Type` or `Role`.

Generates the type or role, its attributes, the relationships hanging off its associations,
and every child reached through generalization. Then the registration properties each
object needs.

**Writes two files:**

- `Schema_Output_<timestamp>.txt` — the MQL
- `String_Resource_Output_<timestamp>.properties` — display names

The `.properties` file is **not** shown in the preview and is written unconditionally once
you accept the script.

Starting from a Type also pulls in its attributes, its relationships and its derived types,
so selecting the root of a hierarchy usually gets you everything in one pass.

---

## Policy Script Generator

**Select:** an element whose EA type is StateMachine, stereotyped `Policy`.

Generates the policy, its states in transition order starting from the `Start State`, and
the access rules attached to each state.

**Writes two files:**

- `Policy_Output_<timestamp>.txt`
- `Policy_Output_String_Resources_<timestamp>.properties`

!!! note "No byte order mark"
    The policy script is written without a BOM, unlike the others.

The policy needs a diagram, and that diagram needs a state stereotyped `Start State`.
Without either, nothing is generated for the states.

---

## UI Script Generator

**Select:** an element in `Widget_Profile`.

Generates the widget as a bus object, together with its connections, labels and tooltips.

**Writes:** `UI_Output_<timestamp>.txt`

Labels and tooltips are generated as text-helper bus objects carrying an English and a
Persian value.

---

## Trigger Script Generator

**Select:** a diagram, an element, or a package.

Generates the trigger program parameter objects found in the selection — elements
stereotyped `eService Trigger Program Parameters`.

**Writes:** `Trigger_Output_<timestamp>.txt`

Selecting a package generates every qualifying element directly inside it; selecting a
diagram generates the ones on it.

---

## Name Generator Script Generator

**Select:** a diagram, an element, or a package.

Generates the object number generators in the selection — elements stereotyped
`Object Number Generator` — and their connections.

**Writes:** `Naming_Output_<timestamp>.txt`

---

## What "select" means

Generators read EA's **context item**, which is whatever is highlighted in the Project
Browser. If a command reports that the selection is wrong when you believe it is right,
click the element in the Project Browser once more and try again — having a diagram open is
not the same as having its element selected.

## Order of statements

Within one script, objects are emitted in the order their generator walks the model:
attributes before the types that carry them, parents before derived children. That ordering
is a consequence of how the generators call each other rather than a sorting step, so if you
generate two scripts and run them separately, **you are responsible for running them in the
right order**.

The one place Dastan orders explicitly is the [delta script](delta-scripts.md), where new
attribute definitions are collected and written ahead of everything else.
