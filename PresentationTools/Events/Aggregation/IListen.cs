namespace PresentationTools.Events.Aggregation
{
	public interface IListen<TEvent>
	{
		void Listen(TEvent e);
	}
}