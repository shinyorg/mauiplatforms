using System;

namespace Microsoft.Maui.LifecycleEvents;

public static class TvOSLifecycleExtensions
{
    public static ILifecycleBuilder AddTvOS(this ILifecycleBuilder builder, Action<ITvOSLifecycleBuilder> configureDelegate)
    {
        var lifecycle = new LifecycleBuilder(builder);
        configureDelegate?.Invoke(lifecycle);
        return builder;
    }

    class LifecycleBuilder : ITvOSLifecycleBuilder
    {
        readonly ILifecycleBuilder _builder;

        public LifecycleBuilder(ILifecycleBuilder builder) => _builder = builder;

        public void AddEvent<TDelegate>(string eventName, TDelegate action) where TDelegate : Delegate
            => _builder.AddEvent(eventName, action);
    }
}
