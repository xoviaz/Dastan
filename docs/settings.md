# Settings

**Dastan → Settings**

Everything is stored per Windows user in the registry, under
`HKEY_CURRENT_USER\Software\DastanSettings`. There is no per-project configuration — the
settings follow you, not the model.

## Script settings

| Setting | Default | What it does |
|---|---|---|
| **Default Output Folder** | `C:\temp` | Where every generated file is written. Generation stops with an error if this is empty. |
| **String Resource Prefix** | `emxFramework` | The leading segment of each line in the generated `.properties` files, e.g. `emxFramework.Type.CW_Widget = Widget`. |
| **Open Exported File After Generate** | off | Opens each written file in its default application once generation finishes. |
| **Track Element Modifications** | on | Master switch for change tracking. With it off, nothing reaches the [modification log](modification-log.md) and delta scripts have nothing to work from. |

## Jira settings

Used by [Jira issue sync](jira.md).

| Setting | What it does |
|---|---|
| **Domain** | Your Jira base URL, e.g. `https://example.atlassian.net`. Also used by *Open In Jira* to build the issue link. |
| **Project** | The Jira project key to pull issues from. |
| **Token** | API token. Stored encrypted with Windows DPAPI, scoped to your Windows account, and masked in the dialog. |
| **Page Size** | How many issues to request per call. Leave empty for the server default. |
| **Scan Incremental** | Fetch only issues changed since the last sync instead of the whole project. |
| **Type** | Restrict the sync to one issue type, or `All`. |
| **Custom JQL** | Extra JQL combined with the settings above, for anything the fields do not cover. |

!!! note "The token is encrypted, not hidden"
    DPAPI ties the stored value to your Windows user on that machine. Copying the registry
    key to another machine or another account gives you an unreadable value, not a leaked
    one. That is by design; re-enter the token instead.

## States mapping

Maps a Jira status onto the state name the model uses, so a synced issue lands in the right
state.

| Jira status | Default model state |
|---|---|
| Draft | `Proposed` |
| To Do | `Proposed` |
| In Progress | `Validated` |
| In Review | `Approved` |
| Done | `Implemented` |

## Moving settings to another machine

Settings live in the registry under the Windows account that set them, so a new machine
or a new teammate starts from nothing. **Export...** and **Import...** on the Settings
dialog move the portable part as a JSON file.

### What travels

The output folder, the string resource prefix, the four checkboxes, and every Jira field
except the token — including the whole States Mapping table.

### What does not

| Setting | Why |
|---|---|
| **Jira token** | A personal credential, not shared configuration. It is encrypted against one Windows account, so a copy would be unreadable elsewhere anyway — and writing it out in the clear would put a secret in a file whose purpose is to be passed around. Whoever imports enters their own. |
| **Last Jira sync time** | Importing someone else's would make an incremental scan skip issues you have not seen. |
| **Preview window layout** | Describes your screen. |
| **Active changeset** | What you happen to be working on right now. |

### How the two buttons behave

Both work on the **saved** settings rather than on what is currently typed into the
dialog:

- **Export** writes what is stored. If you have just edited a field, save first — otherwise
  you export the old value.
- **Import** applies immediately and then refreshes the dialog to match. **Cancel will not
  undo it.** Your token is left alone.

A file that leaves a setting out tops up rather than wipes — anything not mentioned keeps
its current value, and the summary lists which. Names the file contains that this version
does not recognise are ignored and also listed, so a file from a newer Dastan still works.

## Settings that are not in the dialog

These are remembered from the places you set them, and live alongside the rest in the
registry:

| Setting | Set from |
|---|---|
| Skip the preview when there are no findings | The checkbox in the [preview window](preview.md) |
| Preview window size and layout | Resizing the preview window |
| Active changeset | The changeset box on the [modification log panel](modification-log.md) |
| Last Jira sync time | Written automatically after each sync |

## Output file names

Every file is written to the output folder with a timestamp of the form
`yyyyMMdd_HHmmss`, so repeated generation never overwrites an earlier run.

| Command | Files |
|---|---|
| Schema Script Generator | `Schema_Output_<timestamp>.txt`, `String_Resource_Output_<timestamp>.properties` |
| Policy Script Generator | `Policy_Output_<timestamp>.txt`, `Policy_Output_String_Resources_<timestamp>.properties` |
| UI Script Generator | `UI_Output_<timestamp>.txt` |
| Trigger Script Generator | `Trigger_Output_<timestamp>.txt` |
| Name Generator Script Generator | `Naming_Output_<timestamp>.txt` |
| Generate Script (delta) | `Modified_Output_<timestamp>.txt`, or `Modified_Output_<changeset>_<timestamp>.txt` |
