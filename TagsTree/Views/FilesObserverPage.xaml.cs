﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using TagsTree.Interfaces;
using TagsTree.Models;
using TagsTree.Services;
using TagsTree.Services.ExtensionMethods;
using TagsTree.ViewModels;

namespace TagsTree.Views;

public sealed partial class FilesObserverPage : Page, ITypeGetter
{
    public FilesObserverPage()
    {
        InitializeComponent();
        Vm.FilesChangedList.CollectionChanged += (_, _) => CollectionChanged();
        CollectionChanged();

        void CollectionChanged()
        {
            BMergeAll.IsEnabled = BDeleteAll.IsEnabled = BApplyAll.IsEnabled = BSaveAll.IsEnabled = Vm.FilesChangedList.Count is not 0;
            BDeleteRange.IsEnabled = Vm.FilesChangedList.Count > 1;
        }
    }
    public static Type TypeGetter => typeof(FilesObserverPage);

    public static FilesObserverViewModel Vm { get; set; } = null!;

    #region 事件处理

    private async void ApplyCmClick(object sender, RoutedEventArgs e)
    {
        var fileChanged = (FileChanged)((MenuFlyoutItem)sender).DataContext;
        if (FileViewModel.IsValidPath(fileChanged.Path))
            if (fileChanged.Type is FileChanged.ChangedType.Create)
                Apply(fileChanged, null);
            else if (App.IdFile.Values.FirstOrDefault(f => f.FullName == fileChanged.OldFullName) is { } fileModel)
                Apply(fileChanged, fileModel);
            else
            {
                await ShowMessageDialog.Information(true, $"文件列表中不存在：{fileChanged.OldFullName}");
                return;
            }
        else
        {
            await ShowMessageDialog.Information(true, $"不在指定文件路径下：{fileChanged.FullName}");
            return;
        }

        _ = Vm.FilesChangedList.Remove(fileChanged);
        App.SaveFiles();
        if (fileChanged.Type is FileChanged.ChangedType.Create or FileChanged.ChangedType.Delete)
            App.SaveRelations();
        Save();
        ShowInfoBar("已应用一项并保存");
    }

    private void DeleteCmClick(object sender, RoutedEventArgs e)
    {
        _ = Vm.FilesChangedList.Remove((FileChanged)((MenuFlyoutItem)sender).DataContext);
        ShowInfoBar("已删除一项");
    }

    private void DeleteAboveCmClick(object sender, RoutedEventArgs e)
    {
        if ((FileChanged)((MenuFlyoutItem)sender).DataContext == Vm.FilesChangedList[^1])
        {
            Vm.FilesChangedList.Clear();
            return;
        }

        var id = ((FileChanged)((MenuFlyoutItem)sender).DataContext).Id;
        while (Vm.FilesChangedList[0].Id <= id)
            Vm.FilesChangedList.RemoveAt(0);
        ShowInfoBar($"已删除序号{id}及之前项");
    }

    private void DeleteFollowCmClick(object sender, RoutedEventArgs e)
    {
        if ((FileChanged)((MenuFlyoutItem)sender).DataContext == Vm.FilesChangedList[0])
        {
            Vm.FilesChangedList.Clear();
            return;
        }

        var id = ((FileChanged)((MenuFlyoutItem)sender).DataContext).Id;
        while (Vm.FilesChangedList[^1].Id >= id)
            _ = Vm.FilesChangedList.Remove(Vm.FilesChangedList[^1]);
        ShowInfoBar($"已删除序号{id}及之后项");
    }

    private async void DeleteRangeBClick(object sender, RoutedEventArgs e)
    {
        var count = 0;
        CdDeleteRange.PrimaryButtonClick += (_, _) =>
        {
            for (var i = 0; i < Vm.FilesChangedList.Count;)
                if (Rs.RangeStart > Vm.FilesChangedList[i].Id)
                    ++i;
                else if (Vm.FilesChangedList[i].Id > Rs.RangeEnd)
                    break;
                else
                {
                    Vm.FilesChangedList.RemoveAt(i);
                    ++count;
                }

            ShowInfoBar($"已删除{count}项");
        };
        _ = await CdDeleteRange.ShowAsync();
    }

    private async void ApplyAllBClick(object sender, RoutedEventArgs e)
    {
        MergeAll();
        var nameFile = new Dictionary<string, FileModel>();
        foreach (var fileModel in App.IdFile.Values)
            nameFile[fileModel.FullName] = fileModel;
        var deleteList = new List<FileChanged>();
        var invalidExceptions = new List<FileChanged>();
        var notExistExceptions = new List<FileChanged>();
        foreach (var fileChanged in Vm.FilesChangedList)
            if (FileViewModel.IsValidPath(fileChanged.Path))
                if (fileChanged.Type is FileChanged.ChangedType.Create)
                {
                    Apply(fileChanged, null);
                    deleteList.Add(fileChanged);
                }
                else if (nameFile.ContainsKey(fileChanged.OldFullName))
                {
                    Apply(fileChanged, nameFile[fileChanged.OldFullName]);
                    deleteList.Add(fileChanged);
                }
                else
                    notExistExceptions.Add(fileChanged);
            else
                invalidExceptions.Add(fileChanged);
        foreach (var deleteItem in deleteList)
            _ = Vm.FilesChangedList.Remove(deleteItem);
        var exception = "";
        if (invalidExceptions.Count is not 0)
        {
            exception += "*以下文件（夹）不在指定文件路径下：\n";
            exception = invalidExceptions.Aggregate(exception, (current, invalidException) => current + invalidException.FullName + "\n");
        }

        if (notExistExceptions.Count is not 0)
        {
            exception += "*以下文件（夹）不存在于文件列表中：\n";
            exception = notExistExceptions.Aggregate(exception, (current, notExistException) => current + notExistException.OldFullName + "\n");
        }

        if (exception is not "")
            await ShowMessageDialog.Information(true, exception);
        App.SaveFiles();
        App.SaveRelations();
        Save();
        ShowInfoBar("已全部应用并保存");
    }

    private void MergeAllBClick(object sender, RoutedEventArgs e)
    {
        MergeAll();
        Save();
        ShowInfoBar("已全部合并并保存");
    }

    private void DeleteAllBClick(object sender, RoutedEventArgs e)
    {
        ClearAll();
        Save();
        ShowInfoBar("已全部清除并保存");
    }

    private void BSaveAllBClick(object sender, RoutedEventArgs e)
    {
        Save();
        ShowInfoBar("已保存");
    }

    #endregion

    #region 操作

    private static void Apply(FileChanged fileChanged, FileModel? fileModel)
    {
        switch (fileChanged.Type)
        {
            case FileChanged.ChangedType.Create: FileModel.FromFullName(fileChanged.OldFullName).AddNew(); break;
            case FileChanged.ChangedType.Move: fileModel!.MoveOrRename(fileChanged.FullName); break;
            case FileChanged.ChangedType.Rename: fileModel!.MoveOrRename(fileChanged.FullName); break;
            case FileChanged.ChangedType.Delete: fileModel!.Remove(); break;
            default: throw new ArgumentOutOfRangeException(nameof(fileChanged));
        }
    }

    private static void MergeAll()
    {
        var fileChangedMergers = new List<FileChangedMerger>();
        foreach (var fileChanged in Vm.FilesChangedList)
            if (!fileChangedMergers.Any(fileChangedMerger => fileChangedMerger.CanMerge(fileChanged)))
                fileChangedMergers.Add(new(fileChanged));

        ClearAll();
        foreach (var fileChangedMerger in fileChangedMergers)
            switch (fileChangedMerger.GetMergeResult())
            {
                case FileChangedMerger.MergeResult.Move:
                    Vm.FilesChangedList.Add(new(fileChangedMerger.CurrentFullName, FileChanged.ChangedType.Move, fileChangedMerger.OriginalPath));
                    break;
                case FileChangedMerger.MergeResult.Rename:
                    Vm.FilesChangedList.Add(new(fileChangedMerger.CurrentFullName, FileChanged.ChangedType.Rename, fileChangedMerger.OriginalName));
                    break;
                case FileChangedMerger.MergeResult.MoveRename:
                    Vm.FilesChangedList.Add(new($"{fileChangedMerger.CurrentPath}\\{fileChangedMerger.OriginalName}", FileChanged.ChangedType.Move, fileChangedMerger.OriginalPath));
                    Vm.FilesChangedList.Add(new(fileChangedMerger.CurrentFullName, FileChanged.ChangedType.Rename, fileChangedMerger.OriginalName));
                    break;
                case FileChangedMerger.MergeResult.Create:
                    Vm.FilesChangedList.Add(new(fileChangedMerger.CurrentFullName, FileChanged.ChangedType.Create));
                    break;
                case FileChangedMerger.MergeResult.Delete:
                    Vm.FilesChangedList.Add(new(fileChangedMerger.OriginalFullName, FileChanged.ChangedType.Delete));
                    break;
                // MergeResult.Nothing或者不合法数据都不进行操作
                case FileChangedMerger.MergeResult.Nothing:
                default: break;
            }
    }

    private static void ClearAll()
    {
        Vm.FilesChangedList.Clear();
        FileChanged.Num = 1;
    }

    private static void Save()
        // 或App.FilesChangedList
        => FileChanged.Serialize(App.FilesChangedPath, Vm.FilesChangedList);

    private void ShowInfoBar(string message)
    {
        InfoBar.Message = message;
        InfoBar.IsOpen = true;
    }

    #endregion
}
