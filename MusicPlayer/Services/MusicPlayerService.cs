using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Database;
using Android.Graphics;
using Android.Support.V4.Media.Session;
using Android.Support.V4.Media;
using Android.Support.V4.App;
using System.Threading.Tasks;

namespace MusicPlayer.Services
{
    [Service]
    [IntentFilter(new[] { ActionPlay, ActionPause, ActionBack, ActionForward, ActionChangeSong, ActionInitialize})]
    public class MusicPlayerService : Service, AudioManager.IOnAudioFocusChangeListener
    {
        //initalize all the variables
        public const string ActionPlay = "com.xamarin.action.PLAY";
        public const string ActionPause = "com.xamarin.action.PAUSE";
        public const string ActionBack = "com.xamarin.action.BACK";
        public const string ActionForward = "com.xamarin.action.FORWARD";
        public const string ActionChangeSong = "ActionChangeSong";
        public const string ActionInitialize = "ActionInitialize";

        public static List<Song> songsList;
        public static List<Song> playList;
        public MediaPlayer player;
        private AudioManager audioManager;
        private RemoteControlClient remoteControlClient;
        private ComponentName remoteComponentName;
        private bool paused = false;
        private int currentSong = 0;
        
        private const int NotificationId = 1;

        //load the songs from the device, start the audio manager and set up the remote broadcaster
        public override void OnCreate()
        {
            base.OnCreate();

            loadSongs();

            audioManager = (AudioManager)GetSystemService(AudioService);

            remoteComponentName = new ComponentName(PackageName, new RemoteControlBroadcastReceiver().ComponentName);
        }

        //connect the remote client so the drop menu is populated
        private void RegisterRemoteClient()
        {
            try
            {

                if (remoteControlClient == null)
                {
                    audioManager.RegisterMediaButtonEventReceiver(remoteComponentName);
                    //Create a new pending intent that we want triggered by remote control client
                    var mediaButtonIntent = new Intent(Intent.ActionMediaButton);
                    mediaButtonIntent.SetComponent(remoteComponentName);
                    // Create new pending intent for the intent
                    var mediaPendingIntent = PendingIntent.GetBroadcast(this, 0, mediaButtonIntent, 0);
                    // Create and register the remote control client
                    remoteControlClient = new RemoteControlClient(mediaPendingIntent);
                    audioManager.RegisterRemoteControlClient(remoteControlClient);
                }


                //add transport control flags we can to handle
                remoteControlClient.SetTransportControlFlags(RemoteControlFlags.Play |
                    RemoteControlFlags.Pause |
                    RemoteControlFlags.PlayPause |
                    RemoteControlFlags.Stop |
                    RemoteControlFlags.Previous |
                    RemoteControlFlags.Next);


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
		/// Unregisters the remote client from the audio manger
		/// </summary>
		private void UnregisterRemoteClient()
        {
            try
            {
                audioManager.UnregisterMediaButtonEventReceiver(remoteComponentName);
                audioManager.UnregisterRemoteControlClient(remoteControlClient);
                remoteControlClient.Dispose();
                remoteControlClient = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Updates the metadata on the lock screen
        /// </summary>
        private void UpdateMetadata()
        {
            if (remoteControlClient == null)
                return;
            var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
            var songAlbumArtUri = ContentUris.WithAppendedId(songCover, playList[currentSong].albumArt);
            Bitmap bitmap = BitmapFactory.DecodeStream(
                this.ContentResolver.OpenInputStream(songAlbumArtUri));

            var metadataEditor = remoteControlClient.EditMetadata(true);
            metadataEditor.PutString(MetadataKey.Album, playList[currentSong].album);
            metadataEditor.PutString(MetadataKey.Artist, playList[currentSong].artist);
            metadataEditor.PutString(MetadataKey.Title, playList[currentSong].title);
            metadataEditor.PutBitmap(BitmapKey.Artwork, bitmap);
            metadataEditor.Apply();
        }

        //create the binder that the main method calls
        IBinder binder;

        public override IBinder OnBind(Intent intent)
        {
            binder = new MusicPlayerServiceBinder(this);
            return binder;
        }

        public override bool OnUnbind(Intent intent)
        {
            return base.OnUnbind(intent);
        }

        //searches the users device for songs loading them into a list of Song objects
        public void loadSongs()
        {

            var musicResolver = this.ContentResolver;

            Android.Net.Uri uri = MediaStore.Audio.Media.ExternalContentUri;
            String selection = MediaStore.Audio.AudioColumns.IsMusic + "!= 0";
            String sortOrder = MediaStore.Audio.AudioColumns.Title + " ASC";

            String[] projection = {
                MediaStore.Audio.AudioColumns.Id,
                MediaStore.Audio.AudioColumns.Artist,
                MediaStore.Audio.AudioColumns.Title,
                MediaStore.Audio.AudioColumns.Data,
                MediaStore.Audio.AudioColumns.DisplayName,
                MediaStore.Audio.AudioColumns.Duration,
                MediaStore.Audio.AudioColumns.Album,
                MediaStore.Audio.Albums.InterfaceConsts.AlbumId};

            songsList = new List<Song>();
            playList = new List<Song>();

            ICursor cursor = musicResolver.Query(uri, projection, selection, null, sortOrder);

            int count = 0;

            if (cursor != null)
            {
                count = cursor.ColumnCount;

                if (count > 0)
                {
                    while (cursor.MoveToNext())
                    {
                        songsList.Add(new Song{
                            id = cursor.GetString(0),
                            artist = cursor.GetString(1),
                            title = cursor.GetString(2),
                            data = cursor.GetString(3),
                            displayname = cursor.GetString(4),
                            duration = cursor.GetString(5),
                            album = cursor.GetString(6),
                            albumArt = cursor.GetLong(7)});

                        playList.Add(new Song
                        {
                            id = cursor.GetString(0),
                            artist = cursor.GetString(1),
                            title = cursor.GetString(2),
                            data = cursor.GetString(3),
                            displayname = cursor.GetString(4),
                            duration = cursor.GetString(5),
                            album = cursor.GetString(6),
                            albumArt = cursor.GetLong(7)
                        });
                    }

                }
            }

            cursor.Close();

        }

        //The commands from the mainActivity are redirrected to the proper method from here
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
           
            string action = intent.GetStringExtra("button");

            switch (action)
            {
                case ActionPlay: Play(); break;
                case ActionBack: Back(); break;
                case ActionForward: Forward(); break;
                case ActionPause: Pause(); break;
                case ActionInitialize: IntializePlayer(); break;
                case ActionChangeSong:                  
                    int song = intent.GetIntExtra("song", currentSong);
                    ChangeSong(song); break;

            }
            //Set sticky as we are a long running operation
            return StartCommandResult.Sticky;
        }

        //Starts the player setting some base settings
        private void IntializePlayer()
        {
            player = new MediaPlayer();

            player.SetAudioStreamType(Stream.Music);

            //Wake mode will be partial to keep the CPU still running under lock screen
            player.SetWakeMode(ApplicationContext, WakeLockFlags.Partial);

            player.Prepared += (sender, args) => {
                if (remoteControlClient != null)
                    remoteControlClient.SetPlaybackState(RemoteControlPlayState.Playing);
                UpdateMetadata();
                player.Start();
            };


            //When we have reached the end of the song stop ourselves, however you could signal next track here.
            player.Completion += (sender, args) => Forward();

            player.Error += (sender, args) =>
            {
                //playback error
                Console.WriteLine("Error in playback resetting: " + args.What);
                Stop();//this will clean up and reset properly.
            };
        }

        /// <summary>
        /// When we start on the foreground we will present a notification to the user
        /// When they press the notification it will take them to the main page so they can control the music
        /// </summary>
        public void StartForeground()
        {
            var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
            var songAlbumArtUri = ContentUris.WithAppendedId(songCover, playList[currentSong].albumArt);
            Bitmap bitmap = BitmapFactory.DecodeStream(
                this.ContentResolver.OpenInputStream(songAlbumArtUri));
            //Bitmap bitmap = MediaStore.Images.Media.GetBitmap(Main.getContentResolver(), songAlbumArtUri);
            var pendingIntent = PendingIntent.GetActivity(ApplicationContext, 0,
                           new Intent(ApplicationContext, typeof(MainActivity)),
                           PendingIntentFlags.UpdateCurrent);


            var notification = new Notification
            {
                TickerText = new Java.Lang.String("Song started!"),
                Icon = Resource.Drawable.note,
                LargeIcon = bitmap
            };
            notification.Flags |= NotificationFlags.OngoingEvent;
            notification.SetLatestEventInfo(ApplicationContext, playList[currentSong].title,
                            playList[currentSong].artist, pendingIntent);
            StartForeground(NotificationId, notification);

        }


        //starts the player depending on the current state
        private void Play()
        {

            if (paused && player != null)
            {
                paused = false;
                //We are simply paused so just start again
                player.Start();
                StartForeground();

                RegisterRemoteClient();
                remoteControlClient.SetPlaybackState(RemoteControlPlayState.Playing);
                UpdateMetadata();
                return;
            }

            if (player == null)
            {

                IntializePlayer();
            }

            if (player.IsPlaying)
                return;

            try
            {
                changeSong();
                StartForeground();
                RegisterRemoteClient();
                remoteControlClient.SetPlaybackState(RemoteControlPlayState.Buffering);
                UpdateMetadata();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to start playback: " + ex);
            }

        }

        //pause the player disabling the lock screen and drop down
        private void Pause()
        {
            if (player == null)
                return;

            if (player.IsPlaying)
                player.Pause();

            StopForeground(true);
            paused = true;

        }

        //stops the player leaving it in a base state
        private void Stop()
        {
            if (player == null)
                return;

            if (player.IsPlaying)
                player.Stop();

            player.Reset();
            paused = false;
            StopForeground(true);
        }

        //moves to the previous song in the array unless the song is 3000ms in then it returns to the begining
        private void Back()
        {
            if (Position > 3000)
            {
                Seek(0);
            }
            else
            {
                if (currentSong != 0)
                {
                    currentSong--;
                }
                else
                {
                    currentSong = playList.Count - 1;
                }

                player.Reset();
                changeSong();
                StartForeground();
                RegisterRemoteClient();
                remoteControlClient.SetPlaybackState(RemoteControlPlayState.Playing);
                UpdateMetadata();
            }
        }

        //moves to the next song in the array 
        private void Forward()
        {
            if(currentSong != playList.Count - 1)
            {
                currentSong++;
            }
            else
            {
                currentSong = 0;
            }
            
            player.Reset();
            changeSong();
            StartForeground();
            RegisterRemoteClient();
            remoteControlClient.SetPlaybackState(RemoteControlPlayState.Playing);
            UpdateMetadata();
        }

        //changes the song to a specific part of the array based on the number passes
        private void ChangeSong(int songNumber)
        {
            currentSong = songNumber;
            player.Reset();
            changeSong();
            StartForeground();
            RegisterRemoteClient();
            remoteControlClient.SetPlaybackState(RemoteControlPlayState.Playing);
            UpdateMetadata();
        }

        /// <summary>
        /// Properly cleanup of your player by releasing resources
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();
            if (player != null)
            {
                player.Release();
                player = null;
            }
        }

        //changes the song and the varraibles to update for the display
        private void changeSong()
        {
            player.SetDataSource(playList[currentSong].data);

            MainActivity.currentArtist = playList[currentSong].artist;
            MainActivity.currentTitle = playList[currentSong].title;
            MainActivity.currentArt = playList[currentSong].albumArt;
            MainActivity.ChangeText();

            player.Prepare();
        }

        public string Artist
        {
            get
            {
                return playList[currentSong].artist;
            }

        }

        public string Title
        {
            get
            {
                return playList[currentSong].title;
            }

        }

        public long Art
        {
            get
            {
                return playList[currentSong].albumArt;
            }

        }

        public int Position
        {
            get
            {
                if (player == null || paused || !player.IsPlaying)
                    return 0;
                else
                    return player.CurrentPosition;
            }
        }

        public int Duration
        {
            get
            {
                if (player == null || paused || !player.IsPlaying)
                    return 1;
                else
                    return player.Duration;
            }
        }

        public void Seek(int position)
        {
            player.SeekTo(position);
        }

        /// <summary>
        /// For a good user experience we should account for when audio focus has changed.
        /// There is only 1 audio output there may be several media services trying to use it so
        /// we should act correctly based on this.  "duck" to be quiet and when we gain go full.
        /// All applications are encouraged to follow this, but are not enforced.
        /// </summary>
        /// <param name="focusChange"></param>
        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    if (player == null)
                        IntializePlayer();

                    if (!player.IsPlaying)
                    {
                        player.Start();
                        paused = false;
                    }

                    player.SetVolume(1.0f, 1.0f);//Turn it up!
                    break;
                case AudioFocus.Loss:
                    //We have lost focus stop!
                    Stop();
                    break;
                case AudioFocus.LossTransient:
                    //We have lost focus for a short time, but likely to resume so pause
                    Pause();
                    break;
                case AudioFocus.LossTransientCanDuck:
                    //We have lost focus but should till play at a muted 10% volume
                    if (player.IsPlaying)
                        player.SetVolume(.1f, .1f);//turn it down!
                    break;

            }
        }
    }

    //binds the player when called 
    public class MusicPlayerServiceBinder : Binder
    {
        private MusicPlayerService service;

        public MusicPlayerServiceBinder(MusicPlayerService service)
        {
            this.service = service;
        }

        public MusicPlayerService GetMusicPlayerService()
        {
            return service;
        }
    }
}
