using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;

namespace OJT_InternTrack.Services
{
    [Service(Enabled = true, Exported = false, Name = "com.companyname.OJT_InternTrack.AlarmService")]
    public class AlarmService : Service
    {
        private MediaPlayer? mediaPlayer;
        private Vibrator? vibrator;
        private const int NotificationId = 9999;
        private const string ChannelId = "ojt_alarm_active";

        public override IBinder? OnBind(Intent? intent) => null;

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            var title = intent?.GetStringExtra("title") ?? "OJT Reminder";
            var location = intent?.GetStringExtra("location") ?? "";
            var time = intent?.GetStringExtra("time") ?? "";
            var soundUriStr = intent?.GetStringExtra("soundUri") ?? "";

            StartForegroundNotification(title, time, location);
            PlayAlarmSound(soundUriStr);
            StartVibration();

            return StartCommandResult.Sticky;
        }

        private void StartForegroundNotification(string title, string time, string location)
        {
            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            if (notificationManager == null) return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(ChannelId, "Active Alarm", NotificationImportance.High)
                {
                    Description = "Active OJT Alarm Notification",
                    LockscreenVisibility = NotificationVisibility.Public
                };
                channel.EnableVibration(true);
                channel.SetSound(null, null); // We play sound via MediaPlayer
                notificationManager.CreateNotificationChannel(channel);
            }

            var stopIntent = new Intent(this, typeof(AlarmActionReceiver));
            stopIntent.SetAction("STOP_ALARM");
            var stopPendingIntent = PendingIntent.GetBroadcast(this, 0, stopIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            // CUSTOM PROFESSIONAL NOTIFICATION LAYOUT
            int layoutId = Resources.GetIdentifier("notification_alarm", "layout", PackageName);
            var remoteViews = new RemoteViews(PackageName, layoutId);
            
            int titleId = Resources.GetIdentifier("notif_title", "id", PackageName);
            int timeId = Resources.GetIdentifier("notif_time", "id", PackageName);
            int locId = Resources.GetIdentifier("notif_location", "id", PackageName);
            int dismissId = Resources.GetIdentifier("btn_notif_dismiss", "id", PackageName);

            if (titleId != 0) remoteViews.SetTextViewText(titleId, title);
            if (timeId != 0) remoteViews.SetTextViewText(timeId, time);
            if (locId != 0) remoteViews.SetTextViewText(locId, location);
            if (dismissId != 0) remoteViews.SetOnClickPendingIntent(dismissId, stopPendingIntent);

            var builder = (Build.VERSION.SdkInt >= BuildVersionCodes.O) 
                ? new Notification.Builder(this, ChannelId)
                : new Notification.Builder(this);

            builder.SetSmallIcon(Android.Resource.Drawable.IcPopupReminder)
                   .SetCustomContentView(remoteViews)
                   .SetCustomBigContentView(remoteViews) // Professional grid feel
                   .SetStyle(new Notification.DecoratedCustomViewStyle())
                   .SetOngoing(true)
                   .SetCategory(Notification.CategoryAlarm)
                   .SetPriority((int)NotificationPriority.Max)
                   .SetFullScreenIntent(stopPendingIntent, true); // Pop up for user

            if ((int)Build.VERSION.SdkInt >= 34) // Android 14+
            {
                // 1073741824 is ForegroundService.TypeSpecialUse
                StartForeground(NotificationId, builder.Build(), (Android.Content.PM.ForegroundService)1073741824);
            }
            else
            {
                StartForeground(NotificationId, builder.Build());
            }
        }

        private void PlayAlarmSound(string soundUriStr)
        {
            try
            {
                CleanupPlayback();

                Android.Net.Uri? alarmUri = null;
                if (!string.IsNullOrEmpty(soundUriStr))
                {
                    alarmUri = Android.Net.Uri.Parse(soundUriStr);
                }

                if (alarmUri == null)
                {
                    alarmUri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm);
                }

                if (alarmUri == null)
                {
                    alarmUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
                }

                mediaPlayer = new MediaPlayer();
                if (alarmUri != null)
                {
                    mediaPlayer.SetDataSource(this, alarmUri);
                }
                else
                {
                    var defaultUri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm) 
                                   ?? RingtoneManager.GetDefaultUri(RingtoneType.Notification);
                    if (defaultUri != null)
                    {
                        mediaPlayer.SetDataSource(this, defaultUri);
                    }
                    else
                    {
                        return;
                    }
                }
                mediaPlayer.SetAudioAttributes(new AudioAttributes.Builder()
                    .SetUsage(AudioUsageKind.Alarm)
                    .SetContentType(AudioContentType.Music)
                    .Build());
                mediaPlayer.Looping = true;
                mediaPlayer.Prepare();
                mediaPlayer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing alarm sound: {ex.Message}");
            }
        }

        private void StartVibration()
        {
            if (vibrator == null)
                vibrator = GetSystemService(VibratorService) as Vibrator;

            if (vibrator != null && vibrator.HasVibrator)
            {
                vibrator.Cancel();
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    long[] pattern = { 0, 500, 500 };
                    var effect = VibrationEffect.CreateWaveform(pattern, 0);
                    vibrator.Vibrate(effect);
                }
                else
                {
#pragma warning disable CS0618
                    vibrator.Vibrate(new long[] { 0, 500, 500 }, 0);
#pragma warning restore CS0618
                }
            }
        }

        public override void OnDestroy()
        {
            CleanupPlayback();
            base.OnDestroy();
        }

        private void CleanupPlayback()
        {
            try
            {
                if (mediaPlayer != null)
                {
                    if (mediaPlayer.IsPlaying) mediaPlayer.Stop();
                    mediaPlayer.Release();
                    mediaPlayer = null;
                }
                
                if (vibrator != null)
                {
                    vibrator.Cancel();
                }
            }
            catch { }
        }
    }

    [BroadcastReceiver(Enabled = true, Exported = true, Name = "com.companyname.OJT_InternTrack.AlarmActionReceiver")]
    public class AlarmActionReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null) return;
            if (intent?.Action == "STOP_ALARM")
            {
                var serviceIntent = new Intent(context, typeof(AlarmService));
                context.StopService(serviceIntent);
            }
        }
    }
}
