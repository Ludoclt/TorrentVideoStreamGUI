using Microsoft.Win32;
using MonoTorrent;
using MonoTorrent.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TorrentVideoStreamGUI.Engines;

namespace TorrentVideoStreamGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void UserSelectTorrentFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            openFileDialog.Filter = "Torrent files (*.torrent)|*.torrent";
            openFileDialog.ShowDialog();
            TorrentPath.Text = openFileDialog.FileName;
        }

        private async void StartTorrentStreaming(object sender, RoutedEventArgs e)
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();

            var torrentTask = TorrentTask(cancellation.Token);

            Console.CancelKeyPress += delegate { cancellation.Cancel(); torrentTask.Wait(); };
            AppDomain.CurrentDomain.ProcessExit += delegate { cancellation.Cancel(); torrentTask.Wait(); };
            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e) { Console.WriteLine(e.ExceptionObject); cancellation.Cancel(); torrentTask.Wait(); };
            Thread.GetDomain().UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e) { Console.WriteLine(e.ExceptionObject); cancellation.Cancel(); torrentTask.Wait(); };

            torrentTask.Wait();
        }

        private async Task TorrentTask(CancellationToken token)
        {
            EngineSettingsBuilder settingBuilder = new EngineSettingsBuilder
            {
                AllowPortForwarding = true,

                AutoSaveLoadDhtCache = true,

                AutoSaveLoadFastResume = true,

                AutoSaveLoadMagnetLinkMetadata = true,

                ListenPort = 55123,

                DhtPort = 55123,
            };

            ClientEngine engine = new ClientEngine(settingBuilder.ToSettings());
            TorrentStreamEngine torrentStream = new TorrentStreamEngine(engine);

            var stream = await torrentStream.StreamAsync(MagnetLink.Parse("magnet:?xt=urn:btih:EA2976C389CD5DF50252EAAEC0098CB65CC792BA&dn=The%20Book%20of%20Boba%20Fett%20S01E01%201080p%20HEVC%20x265-MeGusta&tr=udp%3A%2F%2Ftracker.coppersurfer.tk%3A6969%2Fannounce&tr=udp%3A%2F%2Ftracker.openbittorrent.com%3A6969%2Fannounce&tr=udp%3A%2F%2F9.rarbg.to%3A2710%2Fannounce&tr=udp%3A%2F%2F9.rarbg.me%3A2780%2Fannounce&tr=udp%3A%2F%2F9.rarbg.to%3A2730%2Fannounce&tr=udp%3A%2F%2Ftracker.opentrackr.org%3A1337&tr=http%3A%2F%2Fp4p.arenabg.com%3A1337%2Fannounce&tr=udp%3A%2F%2Ftracker.torrent.eu.org%3A451%2Fannounce&tr=udp%3A%2F%2Ftracker.tiny-vps.com%3A6969%2Fannounce&tr=udp%3A%2F%2Fopen.stealth.si%3A80%2Fannounce"), token);

            await torrentStream.StartPlayback(stream);

            Console.ReadKey();

            foreach (var manager in engine.Torrents)
            {
                var stoppingTask = manager.StopAsync();
                while (manager.State != TorrentState.Stopped)
                {
                    Console.WriteLine("{0} is {1}", manager.Torrent.Name, manager.State);
                    await Task.WhenAll(stoppingTask, Task.Delay(250));
                }
                await stoppingTask;
                if (engine.Settings.AutoSaveLoadFastResume)
                    Console.WriteLine($"FastResume data for {manager.Torrent?.Name ?? manager.InfoHash.ToHex()} has been written to disk.");
            }

            if (engine.Settings.AutoSaveLoadDhtCache)
                Console.WriteLine($"DHT cache has been written to disk.");

            if (engine.Settings.AllowPortForwarding)
                Console.WriteLine("uPnP and NAT-PMP port mappings have been removed");
        }
    }
}
