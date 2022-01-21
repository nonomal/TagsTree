﻿using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading.Tasks;
using TagsTree.Services;
using TagsTree.Views;
using Windows.UI;

namespace TagsTree;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[INotifyPropertyChanged]
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        App.RootNavigationView = NavigationView;
        App.RootFrame = NavigateFrame;
        //TODO 标题栏
        SetTitleBar(TitleBar);
    }

    private double PaneWidth => Math.Max(NavigationView.ActualWidth, NavigationView.CompactModeThresholdWidth) / 4;
    private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
    {
        NavigationView.PaneDisplayMode = NavigationView.ActualWidth < NavigationView.CompactModeThresholdWidth ? NavigationViewPaneDisplayMode.LeftCompact : NavigationViewPaneDisplayMode.Left;
        OnPropertyChanged(nameof(PaneWidth));
    }

    private async void Loaded(object sender, RoutedEventArgs e)
    {
        if (App.ConfigSet)
            await ConfigIsSet();
        else DisplaySettings();
    }

    public async Task ConfigIsSet()
    {
        if (App.LoadConfig() is { } exception)
        {
            DisplaySettings();
            await App.ExceptionHandler(exception);
        }
        else
        {
            _ = NavigateFrame.Navigate(typeof(IndexPage));
            NavigationView.SelectedItem = NavigationView.MenuItems[0];
            NavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left; //不加就不会显示PaneTitle
            OnPropertyChanged(nameof(PaneWidth));
        }
        IconsHelper.LoadFilesIcons();

        foreach (NavigationViewItem menuItem in NavigationView.MenuItems)
            menuItem.IsEnabled = true;
        ((NavigationViewItem)NavigationView.FooterMenuItems[0]).IsEnabled = await App.ChangeFilesObserver();
    }

    private void DisplaySettings()
    {
        _ = NavigateFrame.Navigate(typeof(SettingsPage));
        NavigationView.SelectedItem = NavigationView.FooterMenuItems[1];
        NavigationView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left; //不加就不会显示PaneTitle
        OnPropertyChanged(nameof(PaneWidth));
    }

    private void BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs e)
    {
        NavigateFrame.GoBack();
        sender.SelectedItem = NavigateFrame.Content switch
        {
            IndexPage => sender.MenuItems[0],
            TagSearchFilesPage => sender.MenuItems[0],
            FilePropertiesPage => sender.MenuItems[0],
            FileEditTagsPage => sender.MenuItems[0],
            TagsManagerPage => sender.MenuItems[1],
            FileImporterPage => sender.MenuItems[2],
            SelectTagToEditPage => sender.MenuItems[3],
            TagEditFilesPage => sender.MenuItems[3],
            FilesObserverPage => sender.FooterMenuItems[0],
            SettingsPage => sender.FooterMenuItems[1],
            _ => sender.SelectedItem
        };
        sender.IsBackEnabled = NavigateFrame.CanGoBack;
    }

    private void ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs e)
    {
        if (e.InvokedItem is string item)
            if (Equals(((NavigationViewItem)sender.SelectedItem).Content, NavigateFrame.Content.GetType().Name))
                return;
            else _ = NavigateFrame.Navigate(0 switch
            {
                0 when Equals(item, ((NavigationViewItem)sender.MenuItems[0]).Content) => typeof(IndexPage),
                0 when Equals(item, ((NavigationViewItem)sender.MenuItems[1]).Content) => typeof(TagsManagerPage),
                0 when Equals(item, ((NavigationViewItem)sender.MenuItems[2]).Content) => typeof(FileImporterPage),
                0 when Equals(item, ((NavigationViewItem)sender.MenuItems[3]).Content) => typeof(SelectTagToEditPage),
                0 when Equals(item, ((NavigationViewItem)sender.FooterMenuItems[0]).Content) => typeof(FilesObserverPage),
                0 when Equals(item, ((NavigationViewItem)sender.FooterMenuItems[1]).Content) => typeof(SettingsPage),
                _ => throw new ArgumentOutOfRangeException()
            });
        sender.IsBackEnabled = true;
        GC.Collect();
    }

    private void CloseButtonClick(object sender, RoutedEventArgs e) => Close();


}