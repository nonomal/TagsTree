﻿using ModernWpf.Controls;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using TagsTree.Delegates;
using TagsTree.Services.ExtensionMethods;
using TagsTree.ViewModels;
using TagsTree.ViewModels.Controls;
using TagsTree.Views;
using TagsTree.Views.Controls;
using static TagsTree.Properties.Settings;

namespace TagsTree.Services
{
	public static class MainService
	{
		private static MainViewModel Vm;
		public static Main Win;

		public static void Load(Main window)
		{
			Win = window;
			Vm = (MainViewModel)window.DataContext;
		}

		public static void LoadFileProperties() => _fileProperties = Win.FileProperties;

		private static FileProperties _fileProperties;
		private static bool _isShowed;

		#region 事件处理

		public static void MainMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (!_isShowed || e.GetPosition((Main)sender).X is >= 215 and <= 1065) return;
			_fileProperties.Hide();
			_isShowed = false;
		}

		public static void DgItemMouseDoubleClick(object sender, MouseButtonEventArgs e) => PropertiesCmClick(sender);

		public static void ResultChanged(TagSearchBox sender, ResultChangedEventArgs e) => ((MainViewModel)Win.DataContext).FileViewModels = e.NewResult.ToObservableCollection();

		public static void FileRemoved(FileProperties sender, FileRemovedEventArgs e)
		{
			_ = Vm.FileViewModels.Remove(e.RemovedItem);
			Vm.CollectionChanged();
		}

		#endregion

		#region 命令

		public static void OpenCmClick(object? parameter) => ((FileViewModel)((DataGridRow)parameter!).DataContext).FullName.Open();
		public static void OpenExplorerCmClick(object? parameter) => ((FileViewModel)((DataGridRow)parameter!).DataContext).Path.Open();
		public static void RemoveCmClick(object? parameter)
		{
			if (!App.MessageBoxX.Warning("是否从软件移除该文件？")) return;
			if (!App.TryRemoveFileModel((FileViewModel)((DataGridRow)parameter!).DataContext)) return;
			_ = Vm.FileViewModels.Remove((FileViewModel)((DataGridRow)parameter).DataContext);
			Vm.CollectionChanged();
		}
		public static async void PropertiesCmClick(object? parameter)
		{
			((FilePropertiesViewModel)Win.FileProperties.Grid.DataContext).Load((FileViewModel)((DataGridRow)parameter!).DataContext);
			_ = _fileProperties.ShowAsync(ContentDialogPlacement.Popup);
			await Task.Delay(100);
			_isShowed = true;
		}

		#endregion

		#region 操作

		public static bool CheckConfig()
		{
			while (true)
			{
				if (Default.IsSet) //如果之前有储存过用户配置，则判断是否符合
					switch (App.LoadConfig())
					{
						case null: return false;
						case true: return true;
					}
				else if (new NewConfig().ShowDialog() == false)
					return false;
			}
		}

		#endregion
	}
}