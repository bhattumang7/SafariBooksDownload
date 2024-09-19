using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafariBooksDownload
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Book> Books { get; set; }
        public DownloadViewModel DownloadProgress { get; set; }
        private bool _searchInProgress { get; set; }

        private bool _RetainFolder;

        public bool RetainFolder
        {
            get => _RetainFolder;
            set
            {
                if (_RetainFolder != value)
                {
                    _RetainFolder = value;
                    OnPropertyChanged(nameof(RetainFolder));
                }
            }
        }

        public bool searchInProgress
        {
            get => _searchInProgress;
            set
            {
                if (_searchInProgress != value)
                {
                    _searchInProgress = value;
                    OnPropertyChanged(nameof(searchInProgress));
                }
            }
        }
        public MainViewModel()
        {
            Books = new ObservableCollection<Book>();
            DownloadProgress = new DownloadViewModel();
            
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
