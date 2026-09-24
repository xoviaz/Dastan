using Dastan.org.ed.ea.entity;
using Dastan.org.ed.ea.services.log;

namespace Dastan.org.ed.ea.pipeline
{
    public abstract class Pipeline<TInput, TOutput> : IPipeline<TInput>
    {
        protected readonly LogTabService LogService;
        protected readonly IPipeline<TOutput> NextPipeline;
        protected readonly Context Context;

        protected Pipeline(LogTabService logService, Context context, IPipeline<TOutput> nextPipeline = null)
        {
            LogService = logService;
            Context = context;
            NextPipeline = nextPipeline;
        }

        public abstract void Do(TInput input);
    }

    public abstract class TerminalPipeline<TInput> : IPipeline<TInput>
    {
        protected readonly LogTabService LogService;
        protected readonly Context Context;

        protected TerminalPipeline(LogTabService logService, Context context)
        {
            LogService = logService;
            Context = context;
        }

        public abstract void Do(TInput input);
    }
}