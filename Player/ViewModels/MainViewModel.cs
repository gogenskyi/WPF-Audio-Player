using NAudio.Wave;
using Player.Helpers;
using Player.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Player.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Song> Songs { get; set; } = new();
        public ICommand LoadMusicCommand => new RelayCommand(SelectFolderAndLoadMusic);

        private Song _selectedSong;

        public Song SelectedSong
        {
            get => _selectedSong;
            set
            {
                if (_selectedSong != value)
                {
                    _selectedSong = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedArtist));
                    OnPropertyChanged(nameof(SelectedTitle)); // повідомляє, що SelectedTitle теж змінився
                    PlaySelectedSong();
                }
            }
        }


        public string SelectedTitle => _selectedSong?.Title ?? "Unknown Title";
        public string SelectedArtist => _selectedSong?.Artist ?? "Unknown Artist";


        private WaveOutEvent _outputDevice;
        private AudioFileReader _audioFileReader;
        private DispatcherTimer _positionTimer;
        private bool _isDraggingSlider;
        protected bool _isPlaying = false;
        private BitmapImage iconSource;


        private double _trackPositionSeconds;
        public double TrackPositionSeconds
        {
            get => _trackPositionSeconds;
            set
            {
                if (_trackPositionSeconds != value)
                {
                    _trackPositionSeconds = value;
                    OnPropertyChanged();

                    if (_isDraggingSlider && _audioFileReader != null)
                    {
                        _audioFileReader.CurrentTime = TimeSpan.FromSeconds(_trackPositionSeconds);
                    }
                }
            }
        }
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ButtonContent));
                    OnPropertyChanged(nameof(ImagePath));
                }
            }
        }
        public string ButtonContent => IsPlaying ? "Pause" : "Play";
        public string ImagePath => IsPlaying ? "view/Pause.png" : "view/Play.png";
        public ICommand TogglePlayCommand { get; }




        public double TrackDurationSeconds =>
            _audioFileReader?.TotalTime.TotalSeconds ?? 0;
        public void LoadSongs(string[] filePaths)
        {
            Songs.Clear();
            foreach (var path in filePaths)
            {
                var file = TagLib.File.Create(path);
                var song = new Song
                {
                    FilePath = path,
                    Title = file.Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(path),
                    Artist = file.Tag.FirstPerformer ?? "Unknown",
                    AlbumArt = LoadImage(file)
                };
                Songs.Add(song);
            }
        }


        private BitmapImage LoadImage(TagLib.File file)
        {
            if (file.Tag.Pictures.Length > 0)
            {
                using var ms = new MemoryStream(file.Tag.Pictures[0].Data.Data);
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = ms;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                return image;
            }

            // 👉 Fallback до вбудованого зображення

            return DefaultImage();
        }
        public BitmapImage DefaultImage()
        {
            return new BitmapImage(new Uri("pack://application:,,,/view/icons/albumdefault.png"));
        }



        public void SeekToPositionFromSlider()
        {
            if (_audioFileReader != null)
            {
                _audioFileReader.CurrentTime = TimeSpan.FromSeconds(TrackPositionSeconds);
            }
        }
        private void SelectFolderAndLoadMusic()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Виберіть папку з музикою"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string selectedPath = dialog.FileName;
                SettingsHelper.SaveLastFolder(selectedPath);
                LoadMusicFromFolder(selectedPath);
            }
        }

        public void LoadLastUsedFolder()
        {
            string lastFolder = SettingsHelper.LoadLastFolder();

            if (!string.IsNullOrEmpty(lastFolder) && Directory.Exists(lastFolder))
            {
                LoadMusicFromFolder(lastFolder);
            }
        }


        private void LoadMusicFromFolder(string folderPath)
        {
            var supportedExtensions = new[] { ".mp3", ".wav", ".flac" };

            var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                 .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower()));

            Songs.Clear();

            foreach (var path in files)
            {
                try
                {
                    var tagFile = TagLib.File.Create(path);

                    BitmapImage albumArtImage = null;

                    if (tagFile.Tag.Pictures.Length > 0)
                    {
                        using var ms = new MemoryStream(tagFile.Tag.Pictures[0].Data.Data);
                        albumArtImage = new BitmapImage();
                        albumArtImage.BeginInit();
                        albumArtImage.StreamSource = ms;
                        albumArtImage.CacheOption = BitmapCacheOption.OnLoad;
                        albumArtImage.EndInit();
                        albumArtImage.Freeze(); // важливо для WPF
                    }
                    else
                    {
                        // fallback image
                        albumArtImage = new BitmapImage(new Uri("pack://application:,,,/view/icons/albumdefault.png"));
                    }

                    var song = new Song
                    {
                        FilePath = path,
                        Title = tagFile.Tag.Title ?? Path.GetFileNameWithoutExtension(path),
                        Artist = tagFile.Tag.FirstPerformer ?? "Unknown",
                        AlbumArt = albumArtImage
                    };

                    Songs.Add(song);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Помилка при зчитуванні {path}: {ex.Message}");
                }
            }
        }


        private void PlaySelectedSong()
        {

            _outputDevice?.Stop();
            _audioFileReader?.Dispose();

            if (SelectedSong == null) return;

            _audioFileReader = new AudioFileReader(SelectedSong.FilePath);
            _outputDevice = new WaveOutEvent();
            _outputDevice.Init(_audioFileReader);
            _isPlaying = true;
            _outputDevice.Play();
            _positionTimer = new DispatcherTimer

            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _positionTimer.Tick += (s, e) =>
            {
                if (!_isDraggingSlider)
                {
                    try
                    {
                    TrackPositionSeconds = _audioFileReader.CurrentTime.TotalSeconds;
                    OnPropertyChanged(nameof(TrackDurationSeconds));
                    }
                    catch (System.NullReferenceException)
                    {
                        _positionTimer.Stop();
                        _outputDevice.Stop();
                    }
                }
            };
            _positionTimer.Start();
            _positionTimer.Tick += (s, e) =>
            {
                if (!_isDraggingSlider && _audioFileReader != null)
                {
                    TrackPositionSeconds = _audioFileReader.CurrentTime.TotalSeconds;
                    OnPropertyChanged(nameof(CurrentTimePosition));
                }

            };
            OnPropertyChanged(nameof(DurationTimePosition));
        }



        public ICommand CloseCommand { get; }
        public MainViewModel(Action closeAction)
        {
            CloseCommand = new RelayCommand(_ => closeAction?.Invoke());
        }

        public MainViewModel()
        {
            LoadLastUsedFolder();
            TogglePlayCommand = new RelayCommand(_ => TogglePlay());
        }
        private void TogglePlay()
        {
            if (_outputDevice == null)
                return;

            if (IsPlaying)
            {
                _outputDevice.Pause();
            }
            else
            {
                _outputDevice.Play();
            }

            IsPlaying = !IsPlaying;
        }
        public string CurrentTimePosition =>
            TimeSpan.FromSeconds(TrackPositionSeconds).ToString(@"mm\:ss");
        public string DurationTimePosition =>
            TimeSpan.FromSeconds(TrackDurationSeconds).ToString(@"mm\:ss");




        public event PropertyChangedEventHandler PropertyChanged;


        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
