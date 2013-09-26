using System;
using System.ComponentModel;
using System.Windows.Input;
using FunTools;
using PresentationTools.Events.Weak;

namespace PresentationTools.Reactives
{
	public class ReactiveCommand : ICommand
	{
		public string Name { get; set; }

		public event EventHandler CanExecuteChanged
		{
			add { _canExecuteChanged.Subscribe(value); }
			remove { _canExecuteChanged.Unsubscribe(value); }
		}

		public ReactiveCommand(
			Action<object> execute,
			Func<object, bool> canExecute,
			params INotifyPropertyChanged[] canExecuteNotifiers)
		{
			_execute = execute.ThrowIfNull();
			_canExecute = canExecute.ThrowIfNull();
			_canExecuteNotifiers = canExecuteNotifiers;

			_canExecuteNotifiers.ForEach(x => x.SubscribeWeakly(this, (c, s, e) => c.NotifyCanExecuteChanged()));
		}

		public void Execute(object parameter)
		{
			_execute(parameter);
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute(parameter);
		}

		#region Implementation

		private readonly Action<object> _execute;

		private readonly Func<object, bool> _canExecute;

		private readonly INotifyPropertyChanged[] _canExecuteNotifiers;

		private readonly WeakHandlerEvent<EventHandler, EventArgs>
			_canExecuteChanged = new WeakHandlerEvent<EventHandler, EventArgs>(h => (s, e) => h(s, e));

		private void NotifyCanExecuteChanged()
		{
			_canExecuteChanged.Raise(this, EventArgs.Empty);
		}

		#endregion
	}
}