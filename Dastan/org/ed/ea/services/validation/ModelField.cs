namespace Dastan.org.ed.ea.services.validation
{
    // How a generator writes a value into the script. What counts as broken depends
    // entirely on this: a quote is fatal in a name and fine in a description, and a
    // space is fatal only where the value goes in without quotes around it.
    public enum FieldKind
    {
        // Written through Quote(), but MQL rejects a quote character inside it --
        // a name, a type, a store, a revision.
        Identifier,

        // Written into the statement bare, so a space or a quote ends the value early
        // and the rest of it becomes stray MQL.
        Unquoted,

        // Written bare and read as a number.
        Number
    }

    // One value a generator will take off this object and put into the script.
    public class ModelField
    {
        public string Name { get; }
        public string Value { get; }
        public FieldKind Kind { get; }

        public ModelField(string name, string value, FieldKind kind)
        {
            Name = name;
            Value = value ?? "";
            Kind = kind;
        }
    }
}
