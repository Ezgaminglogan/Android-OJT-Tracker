using Android.App;
using Android.Content;
using Android.Media;
using OJT_InternTrack.Activities;

namespace OJT_InternTrack.Receivers
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    public class AlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null || intent == null) return;

            var scheduleId = intent.GetIntExtra("scheduleId", 0);
            var title = intent.GetStringExtra("title") ?? "Internship Reminder";
            var location = intent.GetStringExtra("location") ?? "";
            var time = intent.GetStringExtra("time") ?? "";

            // Create notification
            ShowNotification(context, scheduleId, title, location, time);

            // Play alarm sound
            var soundUri = intent.GetStringExtra("soundUri") ?? string.Empty;
            PlayAlarmSound(context, soundUri);

            // Vibrate
            VibratePhone(context);
        }

        private void ShowNotification(Context context, int id, string title, string location, string time)
        {
            var notificationManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            if (notificationManager == null) return;

            // Create notification channel for Android 8.0+ (API 26+)
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                CreateNotificationChannel(notificationManager);
            }

            // Create intent to open ScheduleActivity when notification is tapped
            var notificationIntent = new Intent(context, typeof(ScheduleActivity));
            notificationIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            var pendingIntent = PendingIntent.GetActivity(
                context,
                id,
                notificationIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            );

            // Build notification with proper API level support
            Notification? notification;
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                notification = BuildNotificationForOreoAndAbove(context, title, time, location, pendingIntent);
            }
            else
            {
                notification = BuildNotificationForLegacy(context, title, time, location, pendingIntent);
            }

            // Show notification
            if (notification != null)
            {
                notificationManager.Notify(id, notification);
            }
        }

        // For Android 8.0 (API 26) and above
#pragma warning disable CA1416 // Validate platform compatibility
        private void CreateNotificationChannel(NotificationManager notificationManager)
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    "ojt_alarms",
                    "OJT Alarm Notifications",
                    NotificationImportance.High
                )
                {
                    Description = "Notifications for OJT internship alarms"
                };
                channel.EnableVibration(true);
                channel.EnableLights(true);
                notificationManager.CreateNotificationChannel(channel);
            }
        }
#pragma warning restore CA1416


        // Build notification for Android 8.0+ (API 26+)
#pragma warning disable CA1416 // Validate platform compatibility
        private Notification? BuildNotificationForOreoAndAbove(Context context, string title, string time, string location, PendingIntent? pendingIntent)
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                var builder = new Notification.Builder(context, "ojt_alarms")
                    .SetContentTitle($"⏰ {title}")
                    .SetContentText($"Starting at {time}\n📍 {location}")
                    .SetSmallIcon(Android.Resource.Drawable.IcPopupReminder)
                    .SetAutoCancel(true);

                if (pendingIntent != null)
                {
                    builder.SetContentIntent(pendingIntent);
                }

                return builder.Build();
            }
            return null;
        }
#pragma warning restore CA1416

        // Build notification for Android 7.x and below (API 24-25)
#pragma warning disable CA1422 // Validate platform compatibility
        private Notification? BuildNotificationForLegacy(Context context, string title, string time, string location, PendingIntent? pendingIntent)
        {
            var builder = new Notification.Builder(context)
                .SetContentTitle($"⏰ {title}")
                .SetContentText($"Starting at {time}\n📍 {location}")
                .SetSmallIcon(Android.Resource.Drawable.IcPopupReminder)
                .SetAutoCancel(true)
                .SetPriority((int)NotificationPriority.High);

            if (builder == null)
            {
                return null;
            }

            if (pendingIntent != null)
            {
                builder.SetContentIntent(pendingIntent);
            }

            return builder.Build();
        }
#pragma warning restore CA1422

        private void PlayAlarmSound(Context context, string soundUriStr)
        {
            try
            {
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

                var ringtone = RingtoneManager.GetRingtone(context, alarmUri);
                ringtone?.Play();

                // Stop after 5 seconds
                var looper = Android.OS.Looper.MainLooper;
                if (looper != null)
                {
                    var handler = new Android.OS.Handler(looper);
                    handler.PostDelayed(() => ringtone?.Stop(), 5000);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing alarm sound: {ex.Message}");
            }
        }

        private void VibratePhone(Context context)
        {
            try
            {
                // Use VibratorService for API 24-30, VibratorManager for API 31+
#pragma warning disable CA1422 // Validate platform compatibility
                var vibrator = context.GetSystemService(Context.VibratorService) as Android.OS.Vibrator;
#pragma warning restore CA1422

                if (vibrator != null && vibrator.HasVibrator)
                {
                    if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                    {
                        // Use VibrationEffect for Android 8.0+ (API 26+)
#pragma warning disable CA1416 // Validate platform compatibility
                        var effect = Android.OS.VibrationEffect.CreateOneShot(1000, Android.OS.VibrationEffect.DefaultAmplitude);
                        vibrator.Vibrate(effect);
#pragma warning restore CA1416
                    }
                    else
                    {
                        // Use deprecated method for Android 7.x (API 24-25)
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
                        vibrator.Vibrate(1000);
#pragma warning restore CA1422
#pragma warning restore CS0618
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error vibrating: {ex.Message}");
            }
        }
    }
}
