using Player.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Player.view
{
    public partial class MainestWindow : Window
    {
        public MainestWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(Close);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Upload last folder feature (dont work)
                //string settingsPath = System.IO.Path.Combine(
                //Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                //"PlayerApp",
                //"settings.json");
                //string jsonString = File.ReadAllText(settingsPath);
                //Config config = JsonSerializer.Deserialize<Config>(jsonString);
                //string folderPath = config.LastFolder;
            var files = Directory.GetFiles(@"E:\music", "*.mp3");
            vm.LoadSongs(files);
            }
        }
        public class Config
        {
            public string LastFolder { get; set; }
        }
        private void Slider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.GetType().GetField("_isDraggingSlider", BindingFlags.NonPublic | BindingFlags.Instance)
                  ?.SetValue(vm, true);
        }

        private void Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.GetType().GetField("_isDraggingSlider", BindingFlags.NonPublic | BindingFlags.Instance)
                  ?.SetValue(vm, false);
        }
        private void Slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SeekToPositionFromSlider();
            }
        }

        private void Window_Loaded1(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.LoadLastUsedFolder();
            }
        }
    }
}
