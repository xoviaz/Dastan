namespace Dastan.org.ed.ea.services.elements
{
    public interface IScriptGenerator<in T>
    {
        void Generate(T obj);
        
        string StringResources();

        string Scripts();

        string Registers();
    }
}