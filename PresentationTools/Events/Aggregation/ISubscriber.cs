namespace PresentationTools.Events.Aggregation
{
    public interface ISubscriber<TEvent>
    {
        void HandleEvent(TEvent e);
    }
}