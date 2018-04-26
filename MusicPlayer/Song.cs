using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace MusicPlayer
{
    public class Song
    {
        public String id { get; set; }
        public String artist { get; set; }
        public String title { get; set; }
        public String data { get; set; }
        public String displayname { get; set; }
        public String duration { get; set; }
        public String album { get; set; }
        public long albumArt { get; set; }
    }
}