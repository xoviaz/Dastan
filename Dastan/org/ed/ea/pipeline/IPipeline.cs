namespace Dastan.org.ed.ea.pipeline
{
    public interface IPipeline<in T>
    {
        void Do(T input);
    }
}