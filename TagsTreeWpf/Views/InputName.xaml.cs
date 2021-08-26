﻿using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TagsTreeWpf.Services;

namespace TagsTreeWpf.Views
{
	/// <summary>
	/// InputName.xaml 的交互逻辑
	/// </summary>
	public partial class InputName : Window
	{
		public InputName(FileX.InvalidMode mode, string text = "")
		{
			Owner = App.Win;
			InitializeComponent();
			switch (mode)
			{
				case FileX.InvalidMode.Name:
					AsBox.PlaceholderText = @"不能包含\/:*?""<>|和除空格外的空白字符";
					_invalidRegex = FileX.GetInvalidNameChars;
					break;
				case FileX.InvalidMode.Path:
					AsBox.PlaceholderText = @"不能包含/:*?""<>|和除空格外的空白字符";
					_invalidRegex = FileX.GetInvalidPathChars;
					break;
				default: throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}
			AsBox.Text = text;
			_ = Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)(() => Keyboard.Focus(AsBox)));
		}

		public string Message = "";
		private readonly string _invalidRegex;

		private void BConfirm_OnClick(object sender, RoutedEventArgs e)
		{
			if (!new Regex($@"^[^{_invalidRegex}]+$").IsMatch(AsBox.Text))
			{
				MessageBoxX.Error(AsBox.PlaceholderText);
				return;
			}
			Message = AsBox.Text;
			DialogResult = true;
			Close();
		}
	}
}
