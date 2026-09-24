# Importing from ENOVIA

**Dastan → Import From ENOVIA Export**

Creates model elements from a schema export: the reverse of generating a script. Use it to
bring an existing ENOVIA schema into EA, or to pick up objects someone created in the
target system directly.

## Before you start

**Select the package to import into.** Elements are created inside it, in a sub-package
per kind, so everything an import adds is in one place — and can be deleted in one place
if it is not what you wanted. The command does nothing until a package is selected.

!!! danger "EA cannot undo this"
    Nothing an add-in does through EA's API can be undone with Ctrl+Z. The only way back
    is to delete the sub-packages the import created, which is exactly why it puts
    everything in sub-packages. Dastan shows you the counts and asks before it writes
    anything.

## What it creates

| Kind | Becomes |
|---|---|
| `type` | a «Type» class in **Types**, with its description, abstract flag and a generalization to its parent |
| `attributeDef` | an «Attribute» on the type or relationship that carries it, with its type, default, max length, multiline, reset flags and allowed values |
| `relationshipDef` | a «Relationship» class in **Relationships**, made the association class of a new association between the two end types, with cardinalities and end tags |
| `policy` | a «Policy» state machine in **Policies**, with a state diagram, its states in lifecycle order joined by transitions, and its store, sequence, format and type tags |
| `role` | a «Role» class in **Roles** |

Stereotypes are applied through the MDG profile, so what you get looks exactly like
something you drew yourself, with the profile's tagged values — and its colours — already
on it. If the profiles are not installed, see [Installation](installation.md).

### A relationship is an association class

A `relationshipDef` never becomes a plain association between its two types. It becomes a
«Relationship» class that **is** the association class of an association joining the from
type to the to type — the same shape you would draw by hand, and the shape every generator
expects. The end tags and cardinalities live on it, which is only possible because it is a
class rather than a connector.

An end that accepts all types, several types, or a type that is nowhere to be found cannot
be drawn as one association, so the relationship is reported instead of guessed at.

### It draws a diagram

Everything imported goes onto a class diagram called **Imported Schema** in the package you
chose. Types are laid out in a grid; each relationship class sits beside the midpoint of
its two ends, so the association class reads as one.

Both ends of every relationship go on the diagram, **including a type that was already in
the model** and not part of this import — those are stacked below the grid. An association
class with one end missing does not look like an association class.

It is a starting point, not a finished diagram. Use EA's own layout tools to arrange it.

## It only ever creates

What to create is decided by comparing against the **whole model**, not against the
package you are importing into — so importing into a fresh package does not duplicate
types the model already has elsewhere.

An object the model already has is left exactly as it is, **even when the export disagrees
with it**. Reconciling a difference is a decision about your model, not something an
import should make for you. Use
[Comparing with ENOVIA](compare.md) to see those, and change them yourself.

Running the same import twice therefore creates nothing the second time.

## What it will not create, and tells you about

The Output tab lists every one of these, with the reason, prefixed
`[Import From ENOVIA Export]`:

- **An attribute whose owner the model already has.** Adding it would mean changing an
  existing type, which an import that only creates does not do.
- **A relationship whose end accepts all types, or several types.** One end of a modelled
  relationship is one class, and one association joins exactly two elements. Which type to
  draw it to is a decision.
- **A relationship whose end type is neither in the model nor in the export.** There is
  nothing to connect it to.
- **A parent that is neither in the model nor in the export.** The object is still
  created; only the generalization is left off.
- **A range with any operator other than `=`.** `Allowed Values` can only express
  equality, so a `notequal` or `lessthan` range is reported rather than written as if it
  were an equality.

## Afterwards

Run **Compare With ENOVIA Export** against the same file. Everything just imported should
come back as agreeing — that is the check that the import was faithful, and the quickest
way to see anything it could not carry across.

Then check the imported objects against your own conventions before generating from them:

- **Display names.** The export has no string-resource line, so aliases and `Display Name`
  tags are left empty. Types, roles and relationships take their display name from the
  element's alias; policies and states from a `Display Name` tag.
- **Diagram layout.** Imported states are laid out in one column. EA will not arrange a
  diagram built through the API, and a readable column beats a pile.
- **Where things live.** The sub-packages are a starting point, not a filing system. Move
  things where they belong once you are happy with them.
