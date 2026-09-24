namespace Dastan.org.ed.ea.services.mql
{
    public interface IMqlTagTerm
    {
        string TagName { get; }

        string ForAdd(string value);

        string ForModify(string value);
    }
}