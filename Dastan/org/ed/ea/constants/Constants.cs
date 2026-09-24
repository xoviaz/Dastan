namespace Dastan.org.ed.ea.constants
{
    public class Constants
    {
        public static readonly string UI_FORM_ID = "ScriptGenerator.ModificationLogs";
    }

    // MDG Technology profile names (defined in SchemaMDGTechnology.xml / WidgetMDGTechnology.xml).
    public static class Profiles
    {
        public const string PlmSchema = "PLM_Schema_Profile";
        public const string Widget = "Widget_Profile";
    }


    // Element stereotypes used for behavior decisions.
    public static class Stereotypes
    {
        public const string Type = "Type";
        public const string Role = "Role";
        public const string Attribute = "Attribute";
        public const string Policy = "Policy";
        public const string Relationship = "Relationship";
        public const string ObjectNumberGenerator = "Object Number Generator";
    }

    // EA "Type" property values for elements (as opposed to Stereotype).
    public static class ObjectTypes
    {
        public const string StateMachine = "StateMachine";
    }

    // EA connector "Type" property values.
    public static class ConnectorTypes
    {
        public const string Generalization = "Generalization";
        public const string Aggregation = "Aggregation";
        public const string Composition = "Composition";
        public const string Association = "Association";
        public const string Usage = "Usage";
        public const string Transition = "Transition";
        public const string StateFlow = "StateFlow";
    }
}