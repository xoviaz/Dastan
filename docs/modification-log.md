# Modification log

**Dastan → Modification Logs Panel** toggles the panel.

While **Track Element Modifications** is on in [Settings](settings.md), Dastan records what
you change as you work. Those records are what [delta scripts](delta-scripts.md) are built
from.

## Where the log lives

Inside the model itself — an artifact named `DastanModificationLogs` in the root package.
It travels with the project and is shared by anyone who opens it. It is not a file, and
deleting that artifact deletes the history.

## What gets tracked

Only elements in `PLM_Schema_Profile` or `Widget_Profile`. Everything else is ignored.

**On an element:** name, alias, notes, stereotype, and every tagged value.

**On its attributes and operations:** name, alias, description, stereotype, type, default,
and every tagged value — plus one being added to or removed from its element.

**Connectors:** creating one is logged as `Connection`, deleting one as `Disconnection`,
against whichever end belongs to a tracked profile.

## The columns

| Column | What it holds |
|---|---|
| Element Name | The object the row is about. For an attribute row this is the **attribute's** name, not the type's. |
| Type | The element's EA type, or the attribute's data type. |
| Stereotype | Used as the MQL object kind when a script is generated. |
| Property | What changed — `name`, `description`, a tag name, or one of `Connection`, `Disconnection`, `Attribute added`, `Attribute removed`. |
| Old / New | The values. |
| Time | When it was recorded. |
| Changeset | The set it was filed under, if any. |

## Changesets

The **Changeset** box names the set that changes are filed under **from now on**. Type a
name before starting a piece of work and everything you do lands under it.

Naming a set that already has rows also **selects** them, so *Generate Script* acts on the
whole set instead of whatever happened to be highlighted. That works whether you pick the
name from the list or type it and press Enter.

Clearing the box selects everything.

Naming a brand-new set changes no selection, so starting a new changeset never disturbs
rows you picked by hand.

If every selected row belongs to the same changeset, the generated filename says so:
`Modified_Output_<changeset>_<timestamp>.txt`.

## Search

The **Search** box narrows the visible rows to those containing the text, matched against
every visible column — element name, type, stereotype, property, old and new values, time
and changeset. It is case-insensitive.

Filtering hides rows; it never removes them. The full log is kept separately from what is
displayed, so nothing you cannot see can be lost by saving.

Search and changeset compose: filter to `Tooltip`, then pick a changeset, and you get that
set's tooltip changes. Because *Generate Script* works on the selection, a filter also
narrows what you can export — you cannot export what you cannot see.

## Right-click a row

| Item | What it does |
|---|---|
| **Select in Project Browser** | Jumps to the element. For an attribute row this lands on the **type that owns it**, since that is what the log stores; column 1 says which attribute. Double-clicking a row does the same. |
| **Copy** | Copies the selected rows as tab-separated text, ready to paste into Excel. |
| **Revert Change** | Writes the old value back onto the model. See below. |
| **Remove from Log** | Deletes the selected rows from the log without touching the model. Asks first, because the log is the only record and nothing regenerates it. |

The list takes keys directly too: **Enter** selects in the project browser, **Ctrl+C** copies, **Del** removes. Revert has no key on purpose — it writes to the model, which is not a one-keystroke action.

## Reverting a change

**Revert Change** takes a row's **Old** value and writes it back onto the thing it came
from — an element's name, alias, notes or stereotype, one of its tagged values, or the same
on an attribute or operation.

Two things happen alongside the write:

- **The reverted rows are removed from the log.** They no longer describe the model, and
  leaving them would make the next [delta script](delta-scripts.md) re-apply the change you
  just took back.
- **Tracking is stopped while the write happens**, so undoing a change is not itself
  recorded as a change.

Selecting several rows is fine. They are undone newest first, so two changes to the same
property end at the oldest value — undoing `B → C` and then `A → B` leaves `A`.

### Rows that are left alone

A revert never guesses. Anything it cannot undo cleanly is skipped and listed in the
summary, with the reason:

| Reason | Why |
|---|---|
| The value was changed again afterwards | Writing the old value back would silently discard the later edit. Revert the newer row first, then this one. |
| A structural change | `Connection`, `Disconnection`, `Attribute added` / `removed` and `Operation added` / `removed` mean creating or deleting something, which is a modelling decision rather than an undo. Do it in the model. |
| The element no longer exists | Nothing to write to. |
| The tagged value is no longer there | The tag was deleted after the change was logged. |

### Worth knowing

- Reverting a tag that the change **created** sets it back to empty rather than deleting
  it. An empty tag reads the same as an absent one everywhere Dastan uses it.
- The Project Browser is refreshed afterwards, because a name or stereotype written back
  does not reach it on its own.
- There is no undo for the revert itself, which is what the confirmation is for.

## The toolbar buttons

| Button | What it does |
|---|---|
| **Clear** | Empties the whole log — every row, not just the visible ones. |
| **Generate Script** | Builds a [delta script](delta-scripts.md) from the selected rows. |

## Rows whose element was deleted

A row keeps its history after its element is gone. It still shows what changed and when, but
it can no longer be selected in the Project Browser, and it is skipped when a script is
generated.
