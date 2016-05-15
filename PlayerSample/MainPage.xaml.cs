using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.PlayerFramework;
using Microsoft.PlayerFramework.Adaptive;

namespace PlayerSample
{
    public sealed partial class MainPage : Page
    {
        public Dictionary<string, string> Protocols { get; } = new Dictionary<string, string>
        {
            { "HLS", "http://media-lb.tvoli.com/reference-source-asset-no-drm/hls5-any.m3u8" },
            { "DASH", "http://media-lb.tvoli.com/reference-source-asset-no-drm/Manifest.mpd" },
            { "SMOOTH", "http://media-lb.tvoli.com/reference-source-asset-no-drm.ism/Manifest" },
        };

        public MainPage()
        {
            InitializeComponent();

            DataContext = this;

            SetupPlayer();
        }

#if PLAYERFRAMEWORK
        private Microsoft.PlayerFramework.MediaPlayer _playerControl;
#else
        private MediaElement _playerControl;
#endif


        private void SetupPlayer()
        {
            var appView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();

#if PLAYERFRAMEWORK
            appView.Title = "PlayerFramework";
            _playerControl = new Microsoft.PlayerFramework.MediaPlayer
            {
                IsAudioSelectionVisible = true,
                IsCaptionSelectionVisible = true,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            _playerControl.Plugins.Add(new AdaptivePlugin { InstreamCaptionsEnabled = true, });
            _playerControl.Plugins.Add(new Microsoft.PlayerFramework.TimedText.CaptionsPlugin());
            _playerControl.Plugins.Add(new Microsoft.PlayerFramework.TTML.CaptionSettings.TTMLCaptionSettingsPlugin());
#else
            appView.Title = "MediaElement";
            _playerControl = new MediaElement
            {
                AreTransportControlsEnabled = true,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TransportControls = new MediaTransportControls { IsCompact = true }
            };
#endif
            Grid.SetRow(_playerControl, 1);
            rootContainer.Children.Add(_playerControl);

            _playerControl.MediaFailed += OnPlayerMediaFailed;
            _playerControl.MediaOpened += OnPlayerMediaOpened;
            _playerControl.MediaEnded += OnPlayerMediaEnded;
            
            _playerControl.AutoPlay = true;
        }

        private void OnPlayerMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Debug.WriteLine("PLAYER_MEDIA_FAILED");
        }

        private MediaPlaybackItem _currentPlaybackItem;

        private void OnPlayerMediaOpened(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("PLAYER_MEDIA_OPENED");

#if PLAYERFRAMEWORK
            _playerControl.SelectedCaption = _playerControl.AvailableCaptions.FirstOrDefault();
#else
            var subtitleTracks = _currentPlaybackItem.TimedMetadataTracks;
            if ((subtitleTracks != null) && subtitleTracks.Any())
            {
                subtitleTracks.SetPresentationMode(0, TimedMetadataTrackPresentationMode.PlatformPresented);
            }
#endif
        }

        private void OnPlayerMediaEnded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("PLAYER_MEDIA_ENDED");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("LOADED");

            protocolNamesComboBox.SelectedIndex = 0;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("UNLOADED");
        }

        private async Task StartPlayingAsync(string url)
        {
            Debug.WriteLine("START_PLAYING " + url);

#if PLAYERFRAMEWORK
            _playerControl.Source = new Uri(url);

            await Task.FromResult(true);
#else
            var result = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(url));
            if (result.Status == AdaptiveMediaSourceCreationStatus.Success)
            {
                var mediaSource = MediaSource.CreateFromAdaptiveMediaSource(result.MediaSource);
                _currentPlaybackItem = new MediaPlaybackItem(mediaSource);
                _currentPlaybackItem.TimedMetadataTracksChanged +=
                    OnCurrentPlaybackItemTimedMetadataTracksChanged;
                _playerControl.SetPlaybackSource(_currentPlaybackItem);
            }

            statusTextBlock.Text = result.Status.ToString();
#endif
        }

        private void OnCurrentPlaybackItemTimedMetadataTracksChanged(MediaPlaybackItem sender, IVectorChangedEventArgs e)
        {
            Debug.WriteLine("CURRENT_PLAYBACK_ITEM_TIMED_METADATA_TRACKS_CHANGED");
        }

        private async void OnProtocolsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _playerControl.Stop();

            var selectedProtocol = (string) ((ComboBox) sender).SelectedItem;
            string selectedProtocolUrl;
            if (Protocols.TryGetValue(selectedProtocol, out selectedProtocolUrl))
            {
                selectedProtocolUrlTextBlock.Text = selectedProtocolUrl;
                await StartPlayingAsync(selectedProtocolUrl);
            }
            else
            {
                selectedProtocolUrlTextBlock.Text = string.Empty;
            }
        }
    }
}
