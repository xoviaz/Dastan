namespace Dastan.org.ed.ea.services.wrapper
{
    public interface IWrapperScriptGenerator<in T>
    {
        void Execute(T obj);

    }
}