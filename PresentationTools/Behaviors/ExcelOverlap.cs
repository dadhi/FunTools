using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace PresentationTools.Behaviors
{
	/// <summary>
	/// Fixes clicking on ComboBox items when they are over excel window.
	/// For details please refer to http://blogs.msdn.com/b/vsod/archive/2009/12/16/excel-2007-wpf-events-are-not-fired-for-items-that-overlap-excel-ui-for-wpf-context-menus.aspx
	/// </summary>
	public static class ExcelOverlap
	{
		public static readonly DependencyProperty EnableClickOverExcelProperty =
			DependencyProperty.RegisterAttached
			(
				"EnableClickOverExcel",
				typeof(bool),
				typeof(ExcelOverlap),
				new UIPropertyMetadata(false, OnEnableClickOverExcelPropertyChanged)
			);

		public static bool GetEnableClickOverExcel(DependencyObject obj)
		{
			return (bool)obj.GetValue(EnableClickOverExcelProperty);
		}
		
		public static void SetEnableClickOverExcel(DependencyObject obj, bool value)
		{
			obj.SetValue(EnableClickOverExcelProperty, value);
		}

		private static void OnEnableClickOverExcelPropertyChanged(DependencyObject dpo, DependencyPropertyChangedEventArgs args)
		{
			var comboBox = dpo as ComboBox;
			if (comboBox == null)
				return;

			//if the control template is already applied
			if(comboBox.Template != null)
			{
				if (GetPopupAndSubscribe(comboBox, (bool)args.NewValue))
				{
					comboBox.Loaded -= OnControlLoaded; //just to ensure we don't need this subscription
					return;
				}
			}
			//we'll handle subscriptions when template will be applied
			comboBox.Loaded += OnControlLoaded;

		}

		private static void OnControlLoaded(object sender, RoutedEventArgs e)
		{
			var comboBox = sender as ComboBox;
			Debug.Assert(comboBox != null);
			GetPopupAndSubscribe(comboBox, GetEnableClickOverExcel(comboBox));
		}

		private static bool GetPopupAndSubscribe(ComboBox comboBox, bool enableClick)
		{
			var popup = (Popup) comboBox.Template.FindName("PART_Popup", comboBox);
			if (popup != null)
			{
				HandleFocusEvents(popup, enableClick);
				return true;
			}
			return false;
		}

		private static void HandleFocusEvents(Popup popup, bool enableClick)
		{
			popup.GotFocus -= OnPopupGotFocus;
			popup.LostFocus -= OnPopupLostFocus;

			if (!enableClick)
				return;

			popup.GotFocus += OnPopupGotFocus;
			popup.LostFocus += OnPopupLostFocus;
		}

		private static void OnPopupGotFocus(object sender, RoutedEventArgs e)
		{
			var popup = sender as Popup;
			Debug.Assert(popup != null);

//			if (LogicalTreeHelper.GetParent((DependencyObject)e.OriginalSource) == popup)
//			{
				popup.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (DispatcherOperationCallback)
																  delegate
																  	{
																	  var frame = new DispatcherFrame();
																	  Dispatcher.PushFrame(frame);

																	  if (frames.ContainsKey(popup))
																	  {
																		  frames[popup].Continue = false;
																		  frames[popup] = frame;
																	  }
																	  else
																		  frames.Add(popup, frame);

																  	return null;
																  }, null);
//			}
		}

		private static void OnPopupLostFocus(object sender, RoutedEventArgs e)
		{
			var popup = sender as Popup;
			Debug.Assert(popup != null);


//			if (LogicalTreeHelper.GetParent((DependencyObject)e.OriginalSource) == popup)
//			{
				if (frames.ContainsKey(popup))
				{
					frames[popup].Continue = false;
					frames.Remove(popup);
				}
//			}
		}

		private static readonly Dictionary<Popup, DispatcherFrame> frames = new Dictionary<Popup, DispatcherFrame>();
	}
}
