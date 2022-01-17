﻿using JetBrains.Annotations;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TagsTreeWpf.Services.ExtensionMethods;


namespace TagsTreeWpf.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<FileViewModel> FileViewModels { get; set; } = Enumerable.Empty<FileViewModel>().ToObservableCollection();

        private ObservableCollection<FileViewModel> _resultCallBack = Enumerable.Empty<FileViewModel>().ToObservableCollection();
        public ObservableCollection<FileViewModel> ResultCallBack
        {
            get => _resultCallBack;
            set
            {
                if (Equals(_resultCallBack, value)) return;
                _resultCallBack = value;
                FileViewModels = value;
                OnPropertyChanged(nameof(FileViewModels));
            }
        }

        public void CollectionChanged() => OnPropertyChanged(nameof(FileViewModels));
    }
}