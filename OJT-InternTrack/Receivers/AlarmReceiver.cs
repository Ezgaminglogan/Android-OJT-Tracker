using Android.App;
using Android.Content;
using Android.Media;
using OJT_InternTrack.Activities;

namespace OJT_InternTrack.Receivers
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = "com.companyname.OJT_InternTrack.AlarmReceiver")]
    public class AlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null || intent == null) return;

            var scheduleId = intent.GetIntExtra("scheduleId", 0);
            var title = intent.GetStringExtra("title") ?? "Internship Reminder";
            var location = intent.GetStringExtra("location") ?? "";
            var time = intent.GetStringExtra("time") ?? "";
            var soundUri = intent.GetStringExtra("soundUri") ?? string.Empty;

            // Start the foreground service for the alarm
            var serviceIntent = new Intent(context, typeof(OJT_InternTrack.Services.AlarmService));
            serviceIntent.PutExtra("title", title);
            serviceIntent.PutExtra("location", location);
            serviceIntent.PutExtra("time", time);
            serviceIntent.PutExtra("soundUri", soundUri);

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
#pragma warning disable CA1416
                context.StartForegroundService(serviceIntent);
#pragma warning restore CA1416
            }
            else
            {
                context.StartService(serviceIntent);
            }
        }
    }
}

