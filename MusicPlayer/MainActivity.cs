using Android.App;
using Android.Widget;
using Android.OS;
using MusicPlayer.Services;
using Android.Content;
using System;
using Android.Graphics;
using Android.Support.V7.Widget;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MusicPlayer
{
    [Activity(Label = "Music Player", MainLauncher = true)]
    public class MainActivity : Activity, SeekBar.IOnSeekBarChangeListener
    {

        //initalize all the variables 
        static TextView artistTxt;
        static TextView titleTxt;
        static ImageView albumArt;
        SeekBar seekBar;

        public static string currentArtist = "";
        public static string currentTitle = "";
        public static long currentArt;
        public static bool isPlaying;

        public event PlayingEventHandler Playing;

        ImageButton play;
        ImageButton prev;
        ImageButton forward;
        TextView artists;
        TextView songs;
        TextView playing;
        TextView back;

        System.Timers.Timer timer;

        String selectedArtist;
        bool isBound = false;
        bool backView;

        private MusicPlayerServiceBinder binder;
        MusicServiceConnection musicServiceConnection;
        private Intent musicServiceIntent;
        RecyclerView recyclerView;
        RecyclerView.LayoutManager layoutManager;
        SongsAdapter songsAdapter;
        AlbumAdapter albumAdapter;
        ArtistAdapter artistAdapter;

        //set the main view, start the service if it hasn't been started and create the binder
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
         
            timer = new System.Timers.Timer();

            MainView();

            if (!isPlaying)
            {
                Intent intent = new Intent(this, typeof(MusicPlayerService));
                intent.PutExtra("button", "ActionInitialize");
                StartService(intent);

            }
            else
            {
                var play = FindViewById<ImageButton>(Resource.Id.playButton);
                play.SetImageResource(Resource.Drawable.pause);
                isPlaying = true;
                ChangeText();
            }

            musicServiceIntent = new Intent(ApplicationContext, typeof(MusicPlayerService));
            musicServiceConnection = new MusicServiceConnection(this);
            BindService(musicServiceIntent, musicServiceConnection, Bind.AutoCreate);
        }

        //creates a binder to the service
        class MusicServiceConnection : Java.Lang.Object, IServiceConnection
        {
            MainActivity instance;

            public MusicServiceConnection(MainActivity player)
            {
                this.instance = player;
            }

            public void OnServiceConnected(ComponentName name, IBinder service)
            {
                var musicServiceBinder = service as MusicPlayerServiceBinder;
                if (musicServiceBinder != null)
                {
                    var binder = (MusicPlayerServiceBinder)service;
                    instance.binder = binder;
                    instance.isBound = true;
                }
            }

            public void OnServiceDisconnected(ComponentName name)
            {
                instance.isBound = false;
            }
        }

        //sends commands to the service to change the musicplayer
        private void SendAudioCommand(string action)
        {
            Intent intent = new Intent(this, typeof(MusicPlayerService));

            //if it's not playing send the play action and change the button, if not do the opposit
            if (!isPlaying && action == "com.xamarin.action.PLAY")
            {
                var play = FindViewById<ImageButton>(Resource.Id.playButton);
                play.SetImageResource(Resource.Drawable.pause);
                timer.Enabled = true;
                isPlaying = true;
                intent.PutExtra("button", action);
            }
            else if(isPlaying && action == "com.xamarin.action.PLAY")
            {
                var play = FindViewById<ImageButton>(Resource.Id.playButton);
                play.SetImageResource(Resource.Drawable.play);
                isPlaying = false;
                timer.Enabled = false;
                intent.PutExtra("button", "com.xamarin.action.PAUSE");
            }
            else
            {
                intent.PutExtra("button", action);
                timer.Enabled = true;
                isPlaying = true;
            }           
            StartService(intent);
        }

        //Test  code to change text with the binder, having some issues will look into
        public void ChangeText2()
        {
            artistTxt.SetText("Artist: " + binder.GetMusicPlayerService().Artist, TextView.BufferType.Editable);
            titleTxt.SetText("Song: " + binder.GetMusicPlayerService().Title, TextView.BufferType.Editable);

            var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
            var songAlbumArtUri = ContentUris.WithAppendedId(songCover, binder.GetMusicPlayerService().Art);

            albumArt.SetImageURI(songAlbumArtUri);
            seekBar.Progress = 0;

        }

        //change the artist and title text fields on the main page
        public static void ChangeText()
        {
            
            artistTxt.SetText(currentArtist, TextView.BufferType.Editable);
            titleTxt.SetText(currentTitle, TextView.BufferType.Editable);

            var songCover = Android.Net.Uri.Parse("content://media/external/audio/albumart");
            var songAlbumArtUri = ContentUris.WithAppendedId(songCover, currentArt);

            albumArt.SetImageURI(songAlbumArtUri);

        }

        //loads the view coresponding to the action sent
        public void ChangeView(String action)
        {
            switch (action)
            {
                case "artistView":ArtistView(); break;
                case "songsView":
                    SongsView(false);
                    break;
                case "playingView":
                    MainView();
                    ChangeText();
                    break;
            }
        }

        //loops reseting the playList
        public void Loop()
        {
            MusicPlayerService.playList.Clear();
            var count = 0;
            while (count < MusicPlayerService.songsList.Count)
            {
                    MusicPlayerService.playList.Add(new Song
                    {
                        id = MusicPlayerService.songsList[count].id,
                        artist = MusicPlayerService.songsList[count].artist,
                        title = MusicPlayerService.songsList[count].title,
                        data = MusicPlayerService.songsList[count].data,
                        displayname = MusicPlayerService.songsList[count].displayname,
                        duration = MusicPlayerService.songsList[count].duration,
                        album = MusicPlayerService.songsList[count].album,
                        albumArt = MusicPlayerService.songsList[count].albumArt
                    });
                count++;
            }
        }

        //The main view, loads the layout, and hooks up the buttons to the events, starts the timer and if it's already playing it sets items to that state
        public void MainView()
        {
            SetContentView(Resource.Layout.Main);
            play = FindViewById<ImageButton>(Resource.Id.playButton);
            prev = FindViewById<ImageButton>(Resource.Id.backButton);
            forward = FindViewById<ImageButton>(Resource.Id.forwardButton);
            artists = FindViewById<Button>(Resource.Id.artistButton);
            songs = FindViewById<Button>(Resource.Id.songsButton);
            playing = FindViewById<Button>(Resource.Id.playingButton);

            artistTxt = FindViewById<TextView>(Resource.Id.artistText);
            titleTxt = FindViewById<TextView>(Resource.Id.songText);
            albumArt = FindViewById<ImageView>(Resource.Id.albumImage);

            seekBar = FindViewById<SeekBar>(Resource.Id.seekBar);


            play.Click += (sender, args) => SendAudioCommand(MusicPlayerService.ActionPlay);
            prev.Click += (sender, args) => SendAudioCommand(MusicPlayerService.ActionBack);
            forward.Click += (sender, args) => SendAudioCommand(MusicPlayerService.ActionForward);

            artists.Click += (sender, args) => ChangeView("artistView");
            songs.Click += (sender, args) => ChangeView("songsView");
            playing.Click += (sender, args) => ChangeView("albumView");

             
            
            timer.Interval = 10;
            timer.Elapsed += OnTimedEvent;

            seekBar.SetOnSeekBarChangeListener(this);

            if (isPlaying)
            {               
                var play = FindViewById<ImageButton>(Resource.Id.playButton);
                play.SetImageResource(Resource.Drawable.pause);
                isPlaying = true;
                timer.Enabled = true;
            }
        }

        //update the seekbar with the timer
        private void OnTimedEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            
            seekBar.Progress = binder.GetMusicPlayerService().Position;
            seekBar.Max = binder.GetMusicPlayerService().Duration;
        }

        public void OnProgressChanged(SeekBar seekBar, int progress, bool fromUser)
        {

        }

        //when they start using the seekbar pause the timer so it doesn't move
        public void OnStartTrackingTouch(SeekBar seekBar)
        {
            timer.Enabled = false;
        }

        //when they stop set the song to where the seekbar ended then turn the timer on
        public void OnStopTrackingTouch(SeekBar seekBar)
        {
            binder.GetMusicPlayerService().Seek(seekBar.Progress);
            timer.Enabled = true;
        }

        //Loads the song recycler, changes the view slightly if it's loaded from the album view or songs button
        public void SongsView(bool backV)
        {
            backView = backV;
            if (backView)
            {
                SetContentView(Resource.Layout.RecyclerBack);
                back = FindViewById<Button>(Resource.Id.backRecyButton);
                back.Click += (sender, args) => AlbumView(selectedArtist);
            }
            else
            {
                SetContentView(Resource.Layout.Recycler);
            }
            
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recycler_view);
            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);
            songsAdapter = new SongsAdapter(backView);
            songsAdapter.ItemClick += ClickSong;
            recyclerView.SetAdapter(songsAdapter);

            artists = FindViewById<Button>(Resource.Id.artistButton);
            songs = FindViewById<Button>(Resource.Id.songsButton);
            playing = FindViewById<Button>(Resource.Id.playingButton);

            artists.Click += (sender, args) => ChangeView("artistView");
            songs.Click += (sender, args) => ChangeView("songsView");
            playing.Click += (sender, args) => ChangeView("playingView");
        }

        //loads the artist view, loops through grabing all the unqiue artist names before loading
        public void ArtistView()
        {
            SetContentView(Resource.Layout.Recycler);

            var count = 0;
            List<String> artist = new List<string>();
            while (count < MusicPlayerService.songsList.Count)
            {
                artist.Add(MusicPlayerService.songsList[count].artist);
                count++;
            }

            artist = artist.Distinct().ToList();

            recyclerView = FindViewById<RecyclerView>(Resource.Id.recycler_view);
            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);
            artistAdapter = new ArtistAdapter(artist);
            artistAdapter.ItemClick += ClickArtist;
            recyclerView.SetAdapter(artistAdapter);

            artists = FindViewById<Button>(Resource.Id.artistButton);
            songs = FindViewById<Button>(Resource.Id.songsButton);
            playing = FindViewById<Button>(Resource.Id.playingButton);

            artists.Click += (sender, args) => ChangeView("artistView");
            songs.Click += (sender, args) => ChangeView("songsView");
            playing.Click += (sender, args) => ChangeView("playingView");
        }

        //loads the album view, loops through grabing all the unqiue albums from the selected artist
        public void AlbumView(String artist)
        {
            SetContentView(Resource.Layout.RecyclerBack);

            
            var count = 0;
            List<String> albums = new List<string>();
            while (count < MusicPlayerService.songsList.Count)
            {              
                if (artist == MusicPlayerService.songsList[count].artist)
                {
                    albums.Add(MusicPlayerService.songsList[count].album);                  
                }
                count++;
            }
            albums = albums.Distinct().ToList();

            recyclerView = FindViewById<RecyclerView>(Resource.Id.recycler_view);
            layoutManager = new LinearLayoutManager(this);
            recyclerView.SetLayoutManager(layoutManager);
            albumAdapter = new AlbumAdapter(albums);
            albumAdapter.ItemClick += ClickAlbum;
            recyclerView.SetAdapter(albumAdapter);

            artists = FindViewById<Button>(Resource.Id.artistButton);
            songs = FindViewById<Button>(Resource.Id.songsButton);
            playing = FindViewById<Button>(Resource.Id.playingButton);
            back = FindViewById<Button>(Resource.Id.backRecyButton);

            artists.Click += (sender, args) => ChangeView("artistView");
            songs.Click += (sender, args) => ChangeView("songsView");
            playing.Click += (sender, args) => ChangeView("playingView");
            back.Click += (sender, args) => ChangeView("artistView");
        }

        //when a song is selected it tells the music player service which song to switch to then returns to the main view, also reseting the playList if needed
        void ClickSong(object sender, int position)
        {
            if (!backView)
                Loop();

            isPlaying = true;
            Intent intent = new Intent(this, typeof(MusicPlayerService));
            intent.PutExtra("button", "ActionChangeSong");
            intent.PutExtra("song", position);
            MainView();
            
            Console.WriteLine(binder.GetMusicPlayerService().Duration);
            StartService(intent);

            
        }

        //loads the album view sending the artist name
        void ClickArtist(object sender, String position)
        {
            selectedArtist = position;
            AlbumView(position);
            
        }

        //loops through collecting all the songs from the chosen album then calling the song view to show them
        void ClickAlbum(object sender, String position)
        {
            MusicPlayerService.playList.Clear();
            var count = 0;
            while (count < MusicPlayerService.songsList.Count)
            {
                if (MusicPlayerService.songsList[count].album == position)
                {
                    MusicPlayerService.playList.Add(new Song
                    {
                        id = MusicPlayerService.songsList[count].id,
                        artist = MusicPlayerService.songsList[count].artist,
                        title = MusicPlayerService.songsList[count].title,
                        data = MusicPlayerService.songsList[count].data,
                        displayname = MusicPlayerService.songsList[count].displayname,
                        duration = MusicPlayerService.songsList[count].duration,
                        album = MusicPlayerService.songsList[count].album,
                        albumArt = MusicPlayerService.songsList[count].albumArt
                    });
                }
                count++;
            }
            SongsView(true);

        }
    }
}

