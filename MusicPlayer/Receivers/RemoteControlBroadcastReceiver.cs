using Android.App;
using Android.Content;
using Android.Views;
using MusicPlayer.Services;
using System;

namespace MusicPlayer
{
    [BroadcastReceiver]
    [Android.App.IntentFilter(new[] { Intent.ActionMediaButton })]
    public class RemoteControlBroadcastReceiver : BroadcastReceiver
    {

        /// <summary>
        /// gets the class name for the component
        /// </summary>
        /// <value>The name of the component.</value>
        public string ComponentName { get { return this.Class.Name; } }

        /// <Docs>The Context in which the receiver is running.</Docs>
        /// <summary>
        /// When we receive the action media button intent
        /// parse the key event and tell our service what to do.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="intent">Intent.</param>
        public override void OnReceive(Context context, Intent intent)
        {
            Console.WriteLine("remote Player: " + intent);

            if (intent.Action != Intent.ActionMediaButton)
                return;

            //The event will fire twice, up and down.
            // we only want to handle the down event though.
            var key = (KeyEvent)intent.GetParcelableExtra(Intent.ExtraKeyEvent);
            if (key.Action != KeyEventActions.Down)
                return;

            var action = MusicPlayerService.ActionPlay;

            switch (key.KeyCode)
            {
                case Keycode.MediaPlay:
                    action = MusicPlayerService.ActionPlay;
                    break;
                case Keycode.MediaPause:
                    action = MusicPlayerService.ActionPause;
                    break;
                case Keycode.MediaNext:
                    action = MusicPlayerService.ActionForward;
                    break;
                case Keycode.MediaPrevious:
                    action = MusicPlayerService.ActionBack;
                    break;
                default:
                    return;
            }

            var remoteIntent = new Intent(action);
            Console.WriteLine("remote Player:" + remoteIntent);
            context.StartService(remoteIntent);
        }
    }
}