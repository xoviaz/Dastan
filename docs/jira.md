# Jira

Dastan can pull Jira issues into the model as elements, and open an element's issue in a
browser. Both live under **Dastan → Jira**.

Configure the connection first — domain, project and token — in [Settings](settings.md).

## Jira Issue Sync

**Select a package** in the Project Browser first. That package is where the issues land.

**Dastan → Jira → Jira Issue Sync**

The sync fetches issues, converts them, and writes them into the model:

- If the selected package is already named `<KEY> - <Project Name>`, issues go straight into
  it.
- Otherwise a child package with that name is created under your selection, and issues go
  there.

Each issue becomes an element of EA type **Task**, named `<KEY> - <Summary>`, with the issue
key in its **Alias**. The alias is the identity — running the sync again updates the
existing element rather than creating a second one.

Issue fields are written as tagged values, and the issue's status is mapped onto a model
state through the **States Mapping** table in Settings.

Progress and problems go to the *Script Generator Log* tab in EA's Output window, which is
cleared at the start of each sync.

The time of the last successful sync is recorded, which is what **Scan Incremental** uses to
fetch only what changed since.

### Narrowing what comes back

| Setting | Effect |
|---|---|
| **Project** | Only that project's issues. |
| **Type** | Only that issue type, or `All`. |
| **Scan Incremental** | Only issues changed since the last sync. |
| **Page Size** | How many issues per request. |
| **Custom JQL** | Combined with the above, for anything the fields do not express. |

!!! note "Synced elements are not schema elements"
    They have no Dastan stereotype, so no generator produces MQL for them and
    [Validate Model](validate.md) leaves them alone. They are there for traceability, not
    for generation.

## Open In Jira

**Select an element**, then **Dastan → Jira → Open In Jira**.

Opens `<Domain>/browse/<Alias>` in your default browser, using the element's **Alias** as
the issue key. It works on anything whose alias is an issue key, not just synced elements —
so putting a key in the alias of a type or a policy gives you a link from the schema back to
the ticket that asked for it.

If the element has no alias, nothing opens and Dastan says so.

## Changesets and issues

There is no formal link between a Jira issue and a
[changeset](modification-log.md#changesets), but naming a changeset after an issue key is a
cheap way to get one: the delta script filename then carries the key too, as
`Modified_Output_<KEY>_<timestamp>.txt`.
