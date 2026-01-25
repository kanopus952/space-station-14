using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Tutorial.Conditions;

public static class EventListenedConditionKeys
{
    public const string ObserveSuffix = ".Observe";
}

public interface IEventListenedCondition
{
    string CounterKey { get; }
    string ObserveKey { get; }
    bool ObserveAnyWithoutTarget { get; }
    EntProtoId? Target { get; }
}

public abstract partial class EventListenedConditionBase<T> : TutorialConditionBase<T>, IEventListenedCondition
    where T : EventListenedConditionBase<T>
{
    public virtual string CounterKey => typeof(T).Name;
    public string ObserveKey => string.Concat(CounterKey, EventListenedConditionKeys.ObserveSuffix);
    public virtual bool ObserveAnyWithoutTarget => false;

    [DataField]
    public EntProtoId? Target { get; set; }

    [DataField]
    public int Count = 1;
}
