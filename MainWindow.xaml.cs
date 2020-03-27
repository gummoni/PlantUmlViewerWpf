using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PlantUmlViewerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<string> Filenames = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();

            var umlFiles = GetFilenames();
            FileListBox.ItemsSource = Filenames;

            Task.Run(() =>
            {
                //カレントディレクトリにあるUMLファイルをコンパイルしてリストボックスに登録。
                //アイテム選択するとWebBrowserで閲覧できるアプリ。
                Parallel.ForEach(umlFiles, filename =>
                {
                    var svgFilename = System.IO.Path.ChangeExtension(filename, "svg");

                    if (!File.Exists(svgFilename))
                    {
                        var processStartInfo = new ProcessStartInfo("java.exe")
                        {
                            WorkingDirectory = Directory.GetCurrentDirectory(),
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            Arguments = $"-jar plantuml.jar -tsvg -charset UTF-8 {filename}",
                        };
                        Process.Start(processStartInfo).WaitForExit();
                    }

                    Dispatcher.Invoke((() =>
                    {
                        lock (this)
                        {
                            Filenames.Add(svgFilename);
                        }
                    }));
                });
            });
        }

        string[] GetFilenames()
        {
            var filenames = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.txt")
                .Select(_ => Path.GetFileName(_))
                .Where(_ => Regex.IsMatch(_, "(object|class)Diagram"))
                .ToArray();
            return filenames;
        }

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            var svgFilename = (string)FileListBox.SelectedItem;
            var svgFullpath = Path.Combine(Directory.GetCurrentDirectory(), svgFilename);

            if (File.Exists(svgFullpath))
            {
                try
                {
                    var text = File.ReadAllText(svgFullpath);
                    WebView.NavigateToString(text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
