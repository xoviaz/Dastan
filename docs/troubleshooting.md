# Troubleshooting

## The Dastan menu is missing

EA loads add-ins once, at startup, so nothing takes effect until EA is fully closed and
reopened. If it is still missing after a restart, work through
[Installation](installation.md) — most often `RegAsm` was run without administrator rights,
in which case it reports success and does nothing.

## A menu item is greyed out

The generators only enable for the selection they can work with:

| Item | Needs |
|---|---|
| Schema Script Generator | an element stereotyped `Type` or `Role` |
| Policy Script Generator | an element stereotyped `Policy` |
| UI Script Generator | an element in `Widget_Profile` |

If the element looks right but the item is still grey, check its stereotype — an element
created before the MDG technology was imported will not have one.

## Nothing was generated, and there was no error

Almost always a missing stereotype. Every generator tests it first and returns quietly when
it does not match.

Run **Dastan → Validate Model** over the package. `missing-stereotype` is exactly this case.

## Generation stopped with an error about cardinality

A relationship's association connector has no cardinality on one end. Set it on both ends —
`1` becomes `cardinality one`, anything else becomes `cardinality many`. Nothing is written
until it is fixed.

## The script failed in ENOVIA and the error is unhelpful

Every script is one transaction, so a single bad statement rolls back the batch and the
error often names the commit rather than the cause.

1. Open the script file — it is still in your output folder.
2. Check the *Script Generator Log* tab for the findings recorded at generation time.
3. If the failure is a duplicate `add`, the object already exists in the target system. The
   [script check](preview.md) cannot know that; it only sees the script.

## A change I made was not logged

- **Is tracking on?** *Dastan → Settings → Track Element Modifications*.
- **Is the element in a tracked profile?** Only `PLM_Schema_Profile` and `Widget_Profile`
  are watched.
- **Was the panel closed?** A change that cannot be delivered is held and reported later, so
  it should still appear — but open the panel and make another small change to flush it.

## Rows disappeared from the modification log

Four things remove rows:

- **Clear** empties the whole log, not just the visible rows.
- **Remove from Log** in the right-click menu deletes the selected rows.
- **Revert Change** removes each row it successfully undoes, since the row no longer
  describes the model.
- Deleting the `DastanModificationLogs` artifact from the model root deletes everything.

Searching does **not** remove rows. If rows seem to be missing, clear the Search box first.

## Generate Script produced fewer statements than I expected

Rows that generate nothing on purpose:

- rows whose element has been deleted
- operations
- attribute allowed values, policy `Type`/`Format`/`Display Name`, and the relationship
  `From`/`To` tags — see [Delta scripts](delta-scripts.md#what-is-tracked-but-not-generated)
- changes to an attribute that the same script creates, because the definition already
  carries the final values

## Revert Change said it left rows alone

The summary names each one and why. The most common is *"it was changed again
afterwards"* — revert the newer change to that property first, then this one. Structural
changes are never reverted; see
[Reverting a change](modification-log.md#rows-that-are-left-alone).

## An attribute was attached but never defined

The logged row holds the attribute's name from when it was added. If it was **renamed
afterwards**, the lookup misses and no definition is written. See
[the known gap](delta-scripts.md#a-known-gap).

## EA feels slow when clicking around the model

Change tracking compares an element against its snapshot when you move off it, which means
re-reading its attributes and tagged values. On a large element in a shared repository this
is noticeable.

Turning off **Track Element Modifications** while doing bulk modelling removes it entirely —
but nothing is logged while it is off, so turn it back on before the work you want tracked.

## The preview window shows everything on one line

Fixed — if you see it, you are running an older build. Rebuild, re-register, restart EA.

## Where to look for messages

The **Script Generator Log** tab in EA's Output window carries everything: `[Script Check]`
findings, `[Model Check]` findings, `[Modification Tracking Error]` diagnostics, and the
Jira sync log.

It is cleared at the start of each Jira sync, so copy anything you need before running one.
