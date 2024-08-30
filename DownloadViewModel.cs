using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafariBooksDownload
{
    public class DownloadViewModel : INotifyPropertyChanged
    {
        private string _downloadLabel;
        private double _progressBarValue;
        private string _progressLabel;

        public string DownloadLabel
        {
            get => _downloadLabel;
            set
            {
                _downloadLabel = value;
                OnPropertyChanged(nameof(DownloadLabel));
            }
        }

        public double ProgressBarValue
        {
            get => _progressBarValue;
            set
            {
                _progressBarValue = value;
                OnPropertyChanged(nameof(ProgressBarValue));
            }
        }

        public string ProgressLabel
        {
            get => _progressLabel;
            set
            {
                _progressLabel = value;
                OnPropertyChanged(nameof(ProgressLabel));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
