using Android.App;
using Android.Content;
using Android.Media;
using OJT_InternTrack.Activities;
using OJT_InternTrack.Database;
using Android.Widget;

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
            var actionType = intent.GetStringExtra("actionType") ?? "notification"; // notification, clock_in, break_start, break_end, clock_out
            var userId = intent.GetIntExtra("userId", -1);

            // Handle auto-clock actions
            if (actionType != "notification" && userId != -1)
            {
                HandleAutoClockAction(context, actionType, userId, location, scheduleId);
            }

            // Start the foreground service for the alarm notification
            var serviceIntent = new Intent(context, typeof(OJT_InternTrack.Services.AlarmService));
            serviceIntent.PutExtra("title", title);
            serviceIntent.PutExtra("location", location);
            serviceIntent.PutExtra("time", time);
            serviceIntent.PutExtra("soundUri", soundUri);
            serviceIntent.PutExtra("actionType", actionType);

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

        private void HandleAutoClockAction(Context context, string actionType, int userId, string location, int scheduleId)
        {
            var dbHelper = new DatabaseHelper(context);

            try
            {
                switch (actionType)
                {
                    case "clock_in":
                        // Auto clock-in
                        var activeEntry = dbHelper.GetActiveTimeEntry(userId);
                        if (activeEntry == null) // Only clock in if not already clocked in
                        {
                            int entryId = dbHelper.ClockIn(userId, location);
                            if (entryId > 0)
                            {
                                // Save the entry ID and schedule ID for reference
                                var prefs = context.GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
                                var editor = prefs?.Edit();
                                editor?.PutInt("active_entry_id", entryId);
                                editor?.PutInt("active_schedule_id", scheduleId);
                                editor?.Apply();

                                ShowToast(context, "Auto clocked in!");
                            }
                        }
                        break;

                    case "break_start":
                        // Mark break start time
                        var currentEntry = dbHelper.GetActiveTimeEntry(userId);
                        if (currentEntry != null)
                        {
                            dbHelper.UpdateTimeEntryBreakStart(currentEntry.EntryId, DateTime.Now);
                            ShowToast(context, "Break started");
                        }
                        break;

                    case "break_end":
                        // Mark break end time
                        var entryOnBreak = dbHelper.GetActiveTimeEntry(userId);
                        if (entryOnBreak != null)
                        {
                            dbHelper.UpdateTimeEntryBreakEnd(entryOnBreak.EntryId, DateTime.Now);
                            ShowToast(context, "Break ended - back to work!");
                        }
                        break;

                    case "clock_out":
                        // Auto clock-out
                        var prefs2 = context.GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
                        int entryIdToClose = prefs2?.GetInt("active_entry_id", -1) ?? -1;
                        
                        if (entryIdToClose > 0)
                        {
                            bool success = dbHelper.ClockOut(entryIdToClose, "Auto clock-out from scheduled shift");
                            if (success)
                            {
                                // Clear the active entry reference
                                var editor2 = prefs2?.Edit();
                                editor2?.Remove("active_entry_id");
                                editor2?.Remove("active_schedule_id");
                                editor2?.Apply();

                                // Mark schedule as completed
                                dbHelper.MarkScheduleCompleted(scheduleId);

                                ShowToast(context, "Auto clocked out!");
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in auto-clock action: {ex.Message}");
            }
            finally
            {
                dbHelper?.Close();
            }
        }

        private void ShowToast(Context context, string message)
        {
            // Toast might not show from background receiver in newer Android versions
            // But we'll try anyway
            try
            {
                Toast.MakeText(context, message, ToastLength.Short)?.Show();
            }
            catch { }
        }
    }
}

