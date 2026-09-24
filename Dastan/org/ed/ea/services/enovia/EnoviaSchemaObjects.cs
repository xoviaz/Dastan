using System.Collections.Generic;

namespace Dastan.org.ed.ea.services.enovia
{
    // The five admin definitions Dastan models. They are small and only meaningful
    // together, so they share a file rather than scattering one property bag each across
    // five.
    //
    // Every flag here answers an EMPTY element in the DTD -- <abstract/>, <multiline/> --
    // where presence is the value. That is why a release which omits one reads as false
    // rather than as an error.

    public class EnoviaType : EnoviaObject
    {
        public bool Abstract { get; set; }

        // derivedFrom/typeRefList. One parent in practice, though the DTD allows a list.
        public string DerivedFrom { get; set; } = "";

        // attributeDefRefList: names only. The definitions themselves arrive as
        // top-level attributeDef elements.
        public List<string> AttributeNames { get; } = new List<string>();

        public List<string> Methods { get; } = new List<string>();
    }

    public class EnoviaAttribute : EnoviaObject
    {
        // primitiveType: string, integer, real, boolean, date, ...
        public string PrimitiveType { get; set; } = "";
        public string DefaultValue { get; set; } = "";

        public bool Multiline { get; set; }
        public bool ResetOnClone { get; set; }
        public bool ResetOnRevision { get; set; }

        // Absent means unlimited, which MQL writes as 0.
        public string MaxLength { get; set; } = "";

        // rangeList. Each entry keeps its operator, because "= Quarts" and "!= Quarts"
        // are different rules and the operator is not optional in MQL.
        public List<EnoviaRange> Ranges { get; } = new List<EnoviaRange>();
    }

    // A range as the export words it, not as MQL words it. rangeType holds ENOVIA's own
    // terms -- equal, notequal, lessthan, greaterthanequal, match, between -- where MQL
    // wants =, !=, <, >=. Translating is the job of whatever writes MQL, the mirror of
    // what RangeMqlTagTerm already does going out; the reader keeps what the file said.
    public class EnoviaRange
    {
        public string RangeType { get; set; } = "";
        public string Value { get; set; } = "";

        public override string ToString()
        {
            return (RangeType + " " + Value).Trim();
        }
    }

    public class EnoviaRelationship : EnoviaObject
    {
        public bool Abstract { get; set; }
        public bool PreventDuplicates { get; set; }
        public string DerivedFrom { get; set; } = "";

        public EnoviaRelationshipEnd From { get; } = new EnoviaRelationshipEnd();
        public EnoviaRelationshipEnd To { get; } = new EnoviaRelationshipEnd();

        public List<string> AttributeNames { get; } = new List<string>();
    }

    public class EnoviaRelationshipEnd
    {
        public string Meaning { get; set; } = "";
        public string Cardinality { get; set; } = "";
        public string RevisionAction { get; set; } = "";
        public string CloneAction { get; set; } = "";
        public bool PropagateModify { get; set; }
        public bool PropagateConnection { get; set; }

        // allowAllTypes is its own EMPTY element; when it is absent the list is what
        // applies, and the two mean very different things to a reader.
        public bool AllowAllTypes { get; set; }
        public List<string> Types { get; } = new List<string>();
    }

    public class EnoviaPolicy : EnoviaObject
    {
        public string Store { get; set; } = "";
        public string Sequence { get; set; } = "";
        public string MajorSequence { get; set; } = "";
        public string DefaultFormat { get; set; } = "";

        public bool AllowAllTypes { get; set; }
        public List<string> Types { get; } = new List<string>();
        public List<string> Formats { get; } = new List<string>();

        // In policy order, which is the lifecycle order -- the file preserves it and so
        // must anything reading it.
        public List<EnoviaState> States { get; } = new List<EnoviaState>();
    }

    public class EnoviaState
    {
        public string Name { get; set; } = "";

        // Two separate flags in the DTD, and two separate tagged values in the model:
        // revisionable is the minor revision, majorrevisionable the major one.
        public bool Revisionable { get; set; }
        public bool MajorRevisionable { get; set; }
        public bool Versionable { get; set; }
        public bool Published { get; set; }
        public bool AutoPromotion { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class EnoviaRole : EnoviaObject
    {
    }
}
