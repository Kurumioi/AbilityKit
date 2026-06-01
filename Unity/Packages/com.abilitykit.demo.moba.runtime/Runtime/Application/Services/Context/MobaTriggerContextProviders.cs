namespace AbilityKit.Demo.Moba.Services
{
    public interface IMobaTriggerLineageContextProvider
    {
        bool TryGetLineageContext(out MobaTriggerLineageContext lineageContext);
    }

    public interface IMobaTriggerTraceContextProvider
    {
        bool TryGetTraceContext(out MobaTriggerTraceContext traceContext);
    }
}
