namespace Dastan.org.ed.ea.services.modifications.scripts
{
    public interface IModificationScriptStrategy
    {
        bool CanHandle(ModificationRow row);
        string Generate(ModificationRow row);
    }
}