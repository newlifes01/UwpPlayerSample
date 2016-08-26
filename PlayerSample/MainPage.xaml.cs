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
            //Magine samples            
            { "HLS test Magine", "http://media-lb.magine.com/reference-source-asset-no-drm/hls5-any.m3u8" },
            { "HLS live test Magine", "http://media-lb.magine.com/135/hls4-any.m3u8" },
            { "DASH test Magine", "http://media-lb.magine.com/reference-source-asset-no-drm/Manifest.mpd" },
            { "SMOOTH test Mageine", "http://media-lb.magine.com/reference-source-asset-no-drm.ism/Manifest" },
            // DASH samples from http://dashif.org/test-vectors/
            { "DASH SINGLE RESOLUTION MULTI-RATE", "http://dash.edgesuite.net/dash264/TestCases/1a/netflix/exMPD_BIP_TC1.mpd" },
            { "DASH MULTI-RESOLUTION MULTI-RATE", "http://dash.edgesuite.net/dash264/TestCases/2a/qualcomm/1/MultiResMPEG2.mpd" },
            { "DASH MULTIPLE AUDIO REPRESENTATIONS", "http://dash.edgesuite.net/dash264/TestCases/3a/fraunhofer/aac-lc_stereo_without_video/ElephantsDream/elephants_dream_audio_only_aaclc_stereo_sidx.mpd" },
            { "DASH ADDITION OF SUBTITLES", "http://dash.edgesuite.net/dash264/TestCases/4b/qualcomm/1/ED_OnDemand_5SecSeg_Subtitles.mpd" },
            { "DASH MULTIPLE PERIODS", "http://dash.edgesuite.net/dash264/TestCases/5a/nomor/1.mpd" },
            { "DASH ENCRYPTION AND KEY ROTATIONS", "http://dash.edgesuite.net/dash264/TestCases/6b/microsoft/CENC_SD_Time/CENC_SD_time_MPD.mpd" },
            { "DASH DYNAMIC SEGMENT OFFERING", "http://54.72.87.160/stattodyn/statodyn.php?type=mpd&pt=1376172180&tsbd=120&origmpd=http%3A%2F%2Fdash.edgesuite.net%2Fdash264%2FTestCases%2F1b%2Fqualcomm%2F2%2FMultiRate.mpd&php=http%3A%2F%2Fdasher.eu5.org%2Fstatodyn.php&mpd=&debug=0&hack=.mpd" },
            { "DASH DYNAMIC SEGMENT OFFERING WITH MPD UPDATE", "http://tvnlive.dashdemo.edgesuite.net/live/manifest.mpd" },
            { "DASH ADDITION OF TRICK MODE", "http://dash.edgesuite.net/dash264/TestCases/9a/qualcomm/1/MultiRate.mpd" },
            { "DASH MULTIPLE TRACK CONTENT", "http://dash.edgesuite.net/dash264/TestCases/10a/1/iis_forest_short_poem_multi_lang_480p_single_adapt_aaclc_sidx.mpd" },
            { "DASH HIGH DEFINITION SINGLE RESOLUTION MULTI-RATE", "http://dash.edgesuite.net/dash264/TestCasesHD/1a/qualcomm/1/MultiRate.mpd" },
            { "DASH HIGH DEFINITION MULTI-RESOLUTION MULTI-RATE", "http://dash.edgesuite.net/dash264/TestCasesHD/2a/qualcomm/1/MultiResMPEG2.mpd" },
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
                TransportControls = new MediaTransportControls { IsCompact = false }
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
