# Installation

Dastan is a COM add-in built against .NET Framework 4.8. Installing it means building the
assembly, registering it for COM, and telling EA where to find it.

!!! note "Check this against your own setup"
    The registration steps below are the standard ones for an EA COM add-in. If your team
    already has a deployment script, use that instead — the important part is that both
    the COM registration and the `EAAddins` key are present.

## Build

Open `Dastan.sln` and build the `Dastan` project. It references `Interop.EA.dll` from your
Sparx installation, so the reference path has to match where EA is installed on your
machine. If the reference is broken, fix the hint path in `Dastan/Dastan.csproj` before
building.

## Register for COM

From an **elevated** command prompt, in the folder holding the built `Dastan.dll`:

```bat
"%WINDIR%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe" /codebase Dastan.dll
```

`/codebase` is what lets COM find the assembly where it sits, without installing it to the
GAC. It will warn that the assembly is not strong-named; that is expected.

## Tell EA about the add-in

Create a registry key under:

```
HKEY_CURRENT_USER\Software\Sparx Systems\EAAddins\Dastan
```

with its **(Default)** value set to:

```
Dastan.DastanAddIn
```

That value is the ProgID of the add-in class, which is the namespace and class name of
`DastanAddIn`.

## Restart EA

EA loads add-ins once, at startup. Nothing you register takes effect until EA is fully
closed and reopened — not a new project, the whole application.

!!! warning "This applies to every rebuild"
    After any code change: rebuild, re-register, and restart EA. Skipping the restart is
    the single most common reason a change appears not to have worked.

## Confirm it loaded

Open EA and look for a **Dastan** menu. If it is there, the add-in is loaded.

If it is missing:

- Check *Specialize → Add-Ins → Manage Add-Ins* (the exact path varies by EA version) to
  see whether EA found it and disabled it.
- Re-run `RegAsm` from an elevated prompt — registration silently does nothing without
  administrator rights.
- Make sure you registered the same build you are pointing at. `/codebase` records the
  path, so moving the DLL afterwards breaks it.

## The Modification Logs panel

The panel is a separate COM control with its own ProgID, `ScriptGenerator.ModificationLogs`,
registered by the same `RegAsm` call. If the menu works but *Dastan → Modification Logs
Panel* reports that the panel could not be created, the assembly registered but that class
did not — rebuild and re-register, then restart EA.

## The MDG profiles

**Dastan → Install Profiles**

Everything Dastan generates is read out of stereotypes and tagged values, so a model drawn
without the profiles produces nothing: the elements have no stereotype, every generator
skips them, and [Validate Model](validate.md) reports `missing-stereotype`.

The profiles are carried **inside the add-in assembly**, so they can never be a different
version from the code that reads them. One menu item installs all three:

| Technology | What it holds |
|---|---|
| Schema Technology | Types, attributes, relationships, policies, states and roles, with their toolbox pages |
| Widget Technology | Widgets, commands, menus, tables, forms and their connectors, with their toolbox pages |
| Class Diagram Technology | `controller` and `endpoint` stereotypes. No Dastan generator reads these |

The dialog says which of them EA already holds before it changes anything.

!!! warning "Installing replaces what is there"
    EA will not import a technology it already has, so an existing copy is **deleted
    first**. Anything you have edited by hand in an installed copy is lost. If you have
    local changes to a profile, export it from EA before installing.

Restart EA afterwards. The stereotypes work straight away, but toolbox pages are built
when EA starts.

### If a technology does not take

EA listing a technology is not the same as EA using it. Dastan checks both after
installing, and reports a technology EA holds but has not enabled rather than calling it
installed — a success message followed by an empty toolbox is worse than no message.

When one does not take, Dastan offers to write the profile files to
`Documents\Dastan Profiles` and open the folder. Then, in EA:

*Specialize → Technologies → Import MDG Technology*, point it at each file, and restart EA.

That dialog is the route that always works. The API route is the convenience.

### Stereotype colours come from the profile

Each stereotype carries its own `bgcolor`, which is why a type reads blue and a relationship
yellow on a diagram without anyone colouring anything. If imported or newly drawn elements
come out plain white, the profile is not installed — that is the first thing to check.
