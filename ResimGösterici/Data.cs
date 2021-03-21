using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;

namespace ResimGösterici
{
    public class Data : InpcBase
    {
        private ObservableCollection<Resim> resimler;

        public Data()
        {
            Yükle = new RelayCommand<object>(parameter =>
            {
                OpenFileDialog openFileDialog = new() { Multiselect = true, Filter = "Resim Dosyaları (*.jpg;*.jpeg;*.tif;*.tiff;*.png)|*.jpg;*.jpeg;*.tif;*.tiff;*.png" };
                if (openFileDialog.ShowDialog() == true)
                {
                    Resimler = new ObservableCollection<Resim>();
                    foreach (string item in openFileDialog.FileNames)
                    {
                        Resimler.Add(new Resim() { Yol = new Uri(item) });
                    }
                }
            }, parameter => true);
        }

        public ObservableCollection<Resim> Resimler
        {
            get { return resimler; }

            set
            {
                if (resimler != value)
                {
                    resimler = value;
                    OnPropertyChanged(nameof(Resimler));
                }
            }
        }
        public ICommand Yükle { get; }
    }

    public class Resim : Data
    {
        private Uri yol;

        public Uri Yol
        {
            get { return yol; }

            set
            {
                if (yol != value)
                {
                    yol = value;
                    OnPropertyChanged(nameof(Yol));
                }
            }
        }

        public Resim() => DosyaGör = new RelayCommand<object>(parameter => Process.Start(Yol.OriginalString), parameter => File.Exists(Yol.OriginalString));

        public ICommand DosyaGör { get; }

    }
}
