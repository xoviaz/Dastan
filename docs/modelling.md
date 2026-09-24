# Profiles and tagged values

Dastan decides what to generate from an element's **stereotype**. Every generator starts by
testing it and returns quietly when it does not match, so an element without a stereotype
produces nothing at all — and says nothing about why. [Validate Model](validate.md) exists
largely to catch that.

Stereotypes come from the two MDG technologies shipped with the project
(`SchemaMDGTechnology.xml`, `WidgetMDGTechnology.xml`). Import them before you model; see
[Installation](installation.md).

The two profiles also decide what gets **tracked**. Only elements belonging to
`PLM_Schema_Profile` or `Widget_Profile` are watched for changes.

---

## Type

An element stereotyped `Type`. Becomes an MQL type.

| In EA | In MQL |
|---|---|
| Name | the type name |
| Notes | `description` |
| Alias | the display name in the `.properties` file |
| Abstract | `abstract true` / `abstract false` |
| Generalization to another Type | `derived <parent>` |
| Attributes on the element | `attribute "<name>"`, and each attribute is defined too |
| Association connector | generates the relationship on the other end |
| Usage connector | a trigger clause, using the connector's stereotype and its `Event` and `Type` tags |

Generalization is followed **downwards**: generating from a parent type also generates its
children.

!!! note "Two names are skipped"
    Elements named `Document` or `*` are ignored by the type generator. They are assumed to
    be already present in the target system.

---

## Attribute

An **EA attribute** on a Type or Relationship element, stereotyped `Attribute`. An attribute
without that stereotype is skipped, and the type is written without it.

| In EA | In MQL |
|---|---|
| Name | the attribute name |
| Type | `type` |
| Notes | `description` |
| Default | `default` |
| Alias | the display name in the `.properties` file |

### Tagged values

| Tag | Values | Becomes |
|---|---|---|
| `Multi Line` | `True` / anything else | `multiline` / `notmultiline` |
| `Multi Value` | `True` / anything else | `multivalue` / `notmultivalue` |
| `Max Length` | a whole number, or empty | `maxlength <n>`, or nothing |
| `Reset On Clone` | `True` / anything else | `resetonclone` / `notresetonclone` |
| `Reset On Revision` | `True` / anything else | `resetonrevision` / `notresetonrevision` |
| `Allowed Values` | comma-separated list | one `range <value>` per entry |

!!! warning "Allowed values go in unquoted"
    Each entry is written as `range <value>` with nothing around it, so a **space inside an
    entry ends the value early** and the rest becomes stray MQL. `Validate Model` reports
    this as `space-in-unquoted-value`.

Attributes are global objects in MQL. If the same attribute name appears on several types,
only one definition is written — the first one reached. If two same-named attributes differ
in their settings, one of them is silently dropped, which `Validate Model` reports as
`conflicting-duplicate`.

---

## Role

An element stereotyped `Role`. Name, Notes and Alias only, plus generalization, which is
followed downwards the same way as for types.

---

## Relationship

An **association class** — an element stereotyped `Relationship` attached to an association
connector. The connector supplies the two ends; the element supplies everything else.

| In EA | In MQL |
|---|---|
| Name | the relationship name |
| Notes | `description` |
| Alias | display name |
| Abstract | `abstract true` / `abstract false` |
| Generalization on the association class | `derived <parent relationship>` |
| Connector's client / supplier | `from type` / `to type` |
| Connector end cardinality | `cardinality one` when the end reads `1`, otherwise `many` |
| Attributes on the association class | `attribute "<name>"` |

### Tagged values

| Tag | Becomes |
|---|---|
| `From Clone`, `To Clone` | `clone <value>` on that end |
| `From Revision`, `To Revision` | `revision <value>` on that end |
| `From Propagate connection`, `To Propagate connection` | `propagateconnection` / `notpropagateconnection` |
| `From Propagate modify`, `To Propagate modify` | `propagatemodify` / `notpropagatemodify` |
| `Prevent Duplicate` | `preventduplicate` / `notpreventduplicate` |

!!! danger "Cardinality is required"
    If either connector end has no cardinality set, generation **stops with an error** and
    no file is written. The message names the relationship.

---

## Policy

An element whose EA type is **StateMachine** and whose stereotype is `Policy`.

| Tag | Becomes |
|---|---|
| `Type` | `type "A", "B"` — comma-separated list |
| `Store` | `store` |
| `Format` | one `format` clause per comma-separated entry |
| `Default Format` | `defaultformat` |
| `Sequence` | `minorsequence` |
| `Display Name` | the display name in the `.properties` file |

### States

States are child elements on the policy's diagram. The generator starts from the state
stereotyped **`Start State`** and walks the transitions from there. A state's own stereotype
is optional — `Start` and `Final` are stripped out of it before use.

Per state: the `Display Name` tag supplies the string resource, and connected elements
supply the access rules. A state named `owner` or `public` on the far end of a connector is
treated as that MQL keyword rather than as a role.

!!! warning "State names go in unquoted"
    A state's name is written into the policy script bare, so a space in it ends the name
    early. `Validate Model` reports this as `space-in-unquoted-value`.

---

## Widgets

Elements in `Widget_Profile` become bus objects rather than schema objects. Their
connections, labels and tooltips are generated by the
[UI Script Generator](generating.md#ui-script-generator).

Labels and tooltips are written as text-helper bus objects carrying both an English and a
Persian value, so non-ASCII text in those fields is expected and handled.

---

## Triggers

Elements stereotyped `eService Trigger Program Parameters` are generated by the
[Trigger Script Generator](generating.md#trigger-script-generator). They are also treated as
bus objects when their changes are tracked.

A type picks up a trigger through a **Usage** connector to the trigger element, with `Event`
and `Type` tagged values on the connector.

---

## Object Number Generator

Elements stereotyped `Object Number Generator` are generated by the
[Name Generator Script Generator](generating.md#name-generator-script-generator).
