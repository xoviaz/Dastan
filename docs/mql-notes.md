# How MQL handles quotes

This page exists because the answer is counter-intuitive and shapes several of Dastan's
checks. It was established by running the statements below against a live MQL console, not
inferred from documentation.

## MQL has no escape mechanism

There is no way to write a quote character inside a value delimited by that same character.
A backslash is an ordinary character, not an escape:

```mql
add bus 'CW_Widget' 'EDP101-WGT-00000001\"' - policy ... ;
```

fails. So does doubling the quote:

```mql
... "fdsaf""" ... ;
```

Neither `\"` nor `""` produces a quote. There is nothing that does.

## The only way to carry a quote is to switch delimiter

A value containing `"` can be delimited with `'`, and a value containing `'` can be
delimited with `"`:

```mql
... 'fdsaf"' ... ;       # a value containing a double quote
... "fdsaf'" ... ;       # a value containing a single quote
```

Both work. This is what Dastan does automatically: a value is wrapped in single quotes when
it contains a double quote and no single quote, and in double quotes otherwise.

!!! danger "A value containing both is unrepresentable"
    If a value contains `"` **and** `'`, there is no delimiter left that works. MQL simply
    cannot carry it. Dastan will still write the statement, and it will fail.

## Identifiers cannot contain a quote at all

The switch-delimiter trick works for free text. It does **not** work for names. MQL rejects a
quote character in any:

- name
- type
- policy
- vault
- revision

So a type named `CW"Widget` cannot exist, however the statement is written.

## Where this shows up in Dastan

Because free text and identifiers follow different rules, the checks treat them differently.

| Position | Quote allowed? | Checked by |
|---|---|---|
| Name, type, store, revision | No | `quote-in-identifier`, `quote-in-name` |
| Description, alias, label, tooltip, tag value | Yes | not checked |
| Allowed values, policy state names | No — they go in **unquoted**, so a space breaks them too | `space-in-unquoted-value` |

[Validate Model](validate.md) catches these in the model; the
[script check](preview.md) catches them in the generated script.

## Practical advice

Quotes get into names by being pasted in — from a specification document, a spreadsheet
column, or an email. Typographic quotes (`"` and `"`) are a particular trap, because they
look like punctuation rather than syntax.

The habit that avoids all of it: run **Validate Model** over the package after any bulk
edit or paste, before generating.

## Unquoted positions

Two values are written into scripts with nothing around them:

- an attribute's **allowed values**, as `range <value>`
- a **policy state's name**

For these, a space does not widen the value — it ends it, and the rest of the text becomes
stray MQL that fails somewhere unhelpful. Keep them single words.
