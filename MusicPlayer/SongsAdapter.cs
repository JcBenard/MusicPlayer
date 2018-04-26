using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using MusicPlayer;
using MusicPlayer.Services;

public class SongsAdapter : RecyclerView.Adapter
{
    // Event handler for item clicks:
    public event EventHandler<int> ItemClick;

    // Underlying data set (a photo album):
    public List<Song> mSongsList;

    public class SongViewHolder : RecyclerView.ViewHolder
    {
        public TextView songName { get; private set; }
        public TextView artistName { get; private set; }

        // Get references to the views defined in the CardView layout.
        public SongViewHolder(View itemView, Action<int> listener)
            : base(itemView)
        {
            // Locate and cache view references:
            songName = itemView.FindViewById<TextView>(Resource.Id.song);
            artistName = itemView.FindViewById<TextView>(Resource.Id.artist);

            // Detect user clicks on the item view and report which item
            // was clicked (by layout position) to the listener:
            itemView.Click += (sender, e) => listener(base.LayoutPosition);
        }
    }

    // Load the adapter with the data set at construction time using different ones based on the view
    public SongsAdapter(bool backMenu)
    {
        if (backMenu)
            mSongsList = MusicPlayerService.playList;
        else
            mSongsList = MusicPlayerService.songsList;
    }

    // Create a new photo CardView (invoked by the layout manager): 
    public override RecyclerView.ViewHolder
        OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        // Inflate the CardView for the photo:
        View itemView = LayoutInflater.From(parent.Context).
                    Inflate(Resource.Layout.song_list_row, parent, false);

        // Create a ViewHolder to find and hold these view references, and 
        // register OnClick with the view holder:
        SongViewHolder vh = new SongViewHolder(itemView, OnClick);
        return vh;
    }

    // Fill in the contents of the photo card (invoked by the layout manager):
    public override void
        OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        SongViewHolder vh = holder as SongViewHolder;

        // Set the ImageView and TextView in this ViewHolder's CardView 
        // from this position in the photo album:
        vh.songName.Text = mSongsList[position].title;
        vh.artistName.Text = mSongsList[position].artist;
    }

    // Return the number of photos available in the photo album:
    public override int ItemCount
    {
        get { return mSongsList.Count(); }
    }

    // Raise an event when the item-click takes place:
    void OnClick(int position)
    {
        if (ItemClick != null)
            ItemClick(this, position);
    }
}