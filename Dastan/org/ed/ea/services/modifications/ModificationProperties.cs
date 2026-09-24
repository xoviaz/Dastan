namespace Dastan.org.ed.ea.services.modifications
{
    // The property names that mean something structural rather than a changed value.
    // They are written into the log and read back out of it by the script strategies, so
    // the two sides have to agree on the exact text -- which is easier to guarantee here
    // than in a string literal at each end.
    public static class ModificationProperties
    {
        public const string Connection = "Connection";
        public const string Disconnection = "Disconnection";

        // An attribute joining or leaving a type is a change to the TYPE, because that is
        // how MQL says it: modify type X add attribute Y.
        public const string AttributeAdded = "Attribute added";
        public const string AttributeRemoved = "Attribute removed";

        // Operations are tracked for the record only. Nothing in the schema MQL has a
        // counterpart for them, so no script is generated from one.
        public const string OperationAdded = "Operation added";
        public const string OperationRemoved = "Operation removed";
    }
}