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

public class ArtistAdapter : RecyclerView.Adapter
{
    // Event handler for item clicks:
    public event EventHandler<string> ItemClick;

    // Underlying data set (a photo album):
    public List<String> mArtistList;

    public class ArtistViewHolder : RecyclerView.ViewHolder
    {
        public TextView artistName { get; private set; }

        // Get references to the views defined in the CardView layout.
        public ArtistViewHolder(View itemView, Action<int> listener)
            : base(itemView)
        {
            // Locate and cache view references:
            artistName = itemView.FindViewById<TextView>(Resource.Id.album);

            // Detect user clicks on the item view and report which item
            // was clicked (by layout position) to the listener:
            itemView.Click += (sender, e) => listener(base.LayoutPosition);
        }
    }

    // Load the adapter with the data set (photo album) at construction time:
    public ArtistAdapter(List<String> artistList)
    {
        mArtistList = artistList;
    }

    // Create a new photo CardView (invoked by the layout manager): 
    public override RecyclerView.ViewHolder
        OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        // Inflate the CardView for the photo:
        View itemView = LayoutInflater.From(parent.Context).
                    Inflate(Resource.Layout.Album_list_row, parent, false);

        // Create a ViewHolder to find and hold these view references, and 
        // register OnClick with the view holder:
        ArtistViewHolder vh = new ArtistViewHolder(itemView, OnClick);
        return vh;
    }

    // Fill in the contents of the photo card (invoked by the layout manager):
    public override void
        OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        ArtistViewHolder vh = holder as ArtistViewHolder;

        // Set the ImageView and TextView in this ViewHolder's CardView 
        // from this position in the photo album:
        vh.artistName.Text = mArtistList[position];
    }

    // Return the number of photos available in the photo album:
    public override int ItemCount
    {
        get { return mArtistList.Count(); }
    }

    // Raise an event when the item-click takes place:
    void OnClick(int position)
    {
        if (ItemClick != null)
            ItemClick(this, mArtistList[position]);
    }
}