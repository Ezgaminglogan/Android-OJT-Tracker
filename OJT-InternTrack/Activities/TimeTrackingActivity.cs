using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using OJT_InternTrack.Database;
using OJT_InternTrack.Models;

namespace OJT_InternTrack.Activities
{
    [Activity(Label = "Time Tracking", Theme = "@style/Theme.AppCompat.Light")]
    public class TimeTrackingActivity : Activity
    {
        private TextView? currentTimeText;
        private TextView? currentDateText;
        private TextView? statusText;
        private TextView? activeTimerText;
        private TextView? clockedInSinceText;
        private TextView? todayHoursText;
        private TextView? todaySessionsText;
        private Button? clockInButton;
        private Button? clockOutButton;
        private LinearLayout? viewTimesheetButton;
        private LinearLayout? addNotesButton;
        private LinearLayout? historyContainer;
        private TextView? noHistoryText;
        private ImageButton? backButton;
        private SwipeRefreshLayout? swipeRefreshLayout;

        private Handler? timerHandler;
        private TimeEntry? activeEntry;
        private int userId;
        private DatabaseHelper? dbHelper;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_timetracking);

            // Get user ID
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            userId = prefs?.GetInt("user_id", -1) ?? -1;

            if (userId == -1)
            {
                Toast.MakeText(this, "Error: User not logged in", ToastLength.Short)?.Show();
                Finish();
                return;
            }

            InitializeViews();
            SetupEventHandlers();
            LoadActiveTimeEntry();
            UpdateCurrentTime();
            UpdateTodaySummary();
            LoadHistory();
        }

        private void InitializeViews()
        {
            currentTimeText = FindViewById<TextView>(Resource.Id.currentTimeText);
            currentDateText = FindViewById<TextView>(Resource.Id.currentDateText);
            statusText = FindViewById<TextView>(Resource.Id.statusText);
            activeTimerText = FindViewById<TextView>(Resource.Id.activeTimerText);
            clockedInSinceText = FindViewById<TextView>(Resource.Id.clockedInSinceText);
            todayHoursText = FindViewById<TextView>(Resource.Id.todayHoursText);
            todaySessionsText = FindViewById<TextView>(Resource.Id.todaySessionsText);
            clockInButton = FindViewById<Button>(Resource.Id.clockInButton);
            clockOutButton = FindViewById<Button>(Resource.Id.clockOutButton);
            viewTimesheetButton = FindViewById<LinearLayout>(Resource.Id.viewTimesheetButton);
            addNotesButton = FindViewById<LinearLayout>(Resource.Id.addNotesButton);
            backButton = FindViewById<ImageButton>(Resource.Id.backButton);
            swipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefreshLayout);
            historyContainer = FindViewById<LinearLayout>(Resource.Id.historyContainer);
            noHistoryText = FindViewById<TextView>(Resource.Id.noHistoryText);

            dbHelper = new DatabaseHelper(this);
            timerHandler = new Handler(Looper.MainLooper!);
        }

        private void SetupEventHandlers()
        {
            if (backButton != null)
            {
                backButton.Click += (s, e) => Finish();
            }

            if (clockInButton != null)
            {
                clockInButton.Click += ClockInButton_Click;
            }

            if (clockOutButton != null)
            {
                clockOutButton.Click += ClockOutButton_Click;
            }

            if (viewTimesheetButton != null)
            {
                viewTimesheetButton.Click += (s, e) =>
                {
                    StartActivity(new Intent(this, typeof(TimesheetActivity)));
                };
            }

            if (addNotesButton != null)
            {
                addNotesButton.Click += AddNotesButton_Click;
            }

            if (swipeRefreshLayout != null)
            {
                swipeRefreshLayout.SetColorSchemeColors(
                    Android.Graphics.Color.ParseColor("#10B981"),
                    Android.Graphics.Color.ParseColor("#059669")
                );
                swipeRefreshLayout.Refresh += async (s, e) =>
                {
                    await System.Threading.Tasks.Task.Delay(1000);
                    UpdateTodaySummary();
                    LoadHistory();
                    swipeRefreshLayout.Refreshing = false;
                };
            }
        }

        private void LoadActiveTimeEntry()
        {
            if (dbHelper == null) return;

            activeEntry = dbHelper.GetActiveTimeEntry(userId);

            if (activeEntry != null)
            {
                // User is clocked in
                UpdateUIForClockedIn();
                StartTimer();
            }
            else
            {
                // User is not clocked in
                UpdateUIForClockedOut();
            }
        }

        private void UpdateUIForClockedIn()
        {
            if (statusText != null)
            {
                statusText.Text = "● Clocked In";
                statusText.SetTextColor(Android.Graphics.Color.ParseColor("#10B981"));
            }

            if (activeTimerText != null)
            {
                activeTimerText.Visibility = Android.Views.ViewStates.Visible;
            }

            if (clockedInSinceText != null)
            {
                clockedInSinceText.Visibility = Android.Views.ViewStates.Visible;
                clockedInSinceText.Text = $"Clocked in since: {activeEntry?.GetFormattedClockIn()}";
            }

            if (clockInButton != null)
            {
                clockInButton.Enabled = false;
                clockInButton.Alpha = 0.5f;
            }

            if (clockOutButton != null)
            {
                clockOutButton.Enabled = true;
                clockOutButton.Alpha = 1.0f;
            }
        }

        private void UpdateUIForClockedOut()
        {
            if (statusText != null)
            {
                statusText.Text = "● Not Clocked In";
                statusText.SetTextColor(Android.Graphics.Color.ParseColor("#718096"));
            }

            if (activeTimerText != null)
            {
                activeTimerText.Visibility = Android.Views.ViewStates.Gone;
            }

            if (clockedInSinceText != null)
            {
                clockedInSinceText.Visibility = Android.Views.ViewStates.Gone;
            }

            if (clockInButton != null)
            {
                clockInButton.Enabled = true;
                clockInButton.Alpha = 1.0f;
            }

            if (clockOutButton != null)
            {
                clockOutButton.Enabled = false;
                clockOutButton.Alpha = 0.5f;
            }
        }

        private void StartTimer()
        {
            timerHandler?.PostDelayed(UpdateTimer, 1000);
        }

        private void UpdateTimer()
        {
            if (activeEntry?.ClockInTime != null && activeTimerText != null)
            {
                var duration = DateTime.Now - activeEntry.ClockInTime.Value;
                activeTimerText.Text = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

                // Keep the Today's Summary "Hours Worked" updating in real-time too
                UpdateTodaySummary();

                timerHandler?.PostDelayed(UpdateTimer, 1000);
            }
        }

        private void UpdateCurrentTime()
        {
            if (currentTimeText != null)
            {
                // Update format to show seconds for a more "living" feel
                currentTimeText.Text = DateTime.Now.ToString("hh:mm:ss tt");
            }

            if (currentDateText != null)
            {
                currentDateText.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
            }

            // Update every second to keep clocks in sync
            timerHandler?.PostDelayed(UpdateCurrentTime, 1000);
        }

        private void UpdateTodaySummary()
        {
            if (dbHelper == null) return;

            double totalHours = dbHelper.GetTodayTotalTimeHours(userId);

            // Add active session hours if clocked in
            if (activeEntry?.ClockInTime != null)
            {
                var currentDuration = (DateTime.Now - activeEntry.ClockInTime.Value).TotalHours;
                totalHours += currentDuration;
            }

            if (todayHoursText != null)
            {
                todayHoursText.Text = $"{totalHours:F1}h";
            }

            // Count sessions
            if (todaySessionsText != null)
            {
                var sessions = dbHelper.GetTimeEntries(userId).Where(s => s.ClockInTime.HasValue && s.ClockInTime.Value.Date == DateTime.Today).ToList();
                todaySessionsText.Text = sessions.Count.ToString();
            }
        }

        private void LoadHistory()
        {
            if (dbHelper == null || historyContainer == null) return;

            var entries = dbHelper.GetTimeEntries(userId, 5); // Show last 5
            historyContainer.RemoveAllViews();

            if (entries.Count == 0)
            {
                if (noHistoryText != null) noHistoryText.Visibility = Android.Views.ViewStates.Visible;
                return;
            }

            if (noHistoryText != null) noHistoryText.Visibility = Android.Views.ViewStates.Gone;

            foreach (var entry in entries)
            {
                var view = CreateHistoryItemView(entry);
                historyContainer.AddView(view);
            }
        }

        private Android.Views.View CreateHistoryItemView(TimeEntry entry)
        {
            var card = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.WrapContent)
                {
                    BottomMargin = 12
                }
            };
            card.SetPadding(16, 16, 16, 16);
            card.SetBackgroundResource(Resource.Drawable.quick_action_card);
            card.SetGravity(Android.Views.GravityFlags.CenterVertical);
            card.Elevation = 2;

            // Icon
            var icon = new ImageView(this)
            {
                LayoutParameters = new LinearLayout.LayoutParams(DpToPx(32), DpToPx(32))
                {
                    RightMargin = DpToPx(16)
                }
            };
            icon.SetImageResource(entry.Status == "active" ? Resource.Drawable.ic_timer : Resource.Drawable.ic_check);
            icon.SetColorFilter(entry.Status == "active" ? Android.Graphics.Color.ParseColor("#3B82F6") : Android.Graphics.Color.ParseColor("#10B981"));

            // Info
            var infoLayout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LinearLayout.LayoutParams(0, Android.Views.ViewGroup.LayoutParams.WrapContent, 1)
            };

            var dateText = new TextView(this)
            {
                Text = entry.ClockInTime?.ToString("MMM dd, yyyy") ?? "Unknown Date",
                TextSize = 14,
                Typeface = Android.Graphics.Typeface.DefaultBold
            };
            dateText.SetTextColor(Android.Graphics.Color.ParseColor("#1A202C"));

            var timeText = new TextView(this)
            {
                Text = (entry.ClockInTime.HasValue && entry.ClockOutTime.HasValue)
                    ? $"{entry.ClockInTime.Value:hh:mm tt} - {entry.ClockOutTime.Value:hh:mm tt} ({entry.TotalHours:F1}h)"
                    : (entry.ClockInTime.HasValue ? $"{entry.ClockInTime.Value:hh:mm tt} - Present (Active)" : "Unknown - Active"),
                TextSize = 12
            };
            timeText.SetTextColor(Android.Graphics.Color.ParseColor("#718096"));

            infoLayout.AddView(dateText);
            infoLayout.AddView(timeText);

            if (!string.IsNullOrEmpty(entry.Notes))
            {
                var notesText = new TextView(this)
                {
                    Text = entry.Notes,
                    TextSize = 11,
                    Ellipsize = Android.Text.TextUtils.TruncateAt.End
                };
                notesText.SetMaxLines(1);
                notesText.SetTextColor(Android.Graphics.Color.ParseColor("#4A5568"));
                notesText.SetPadding(0, 4, 0, 0);
                notesText.SetCompoundDrawablesWithIntrinsicBounds(Resource.Drawable.ic_edit, 0, 0, 0);
                notesText.CompoundDrawablePadding = 8;
                // Scale down the icon a bit if needed, but setCompoundDrawablesWithIntrinsicBounds usually works for small icons
                infoLayout.AddView(notesText);
            }

            card.AddView(icon);
            card.AddView(infoLayout);

            card.Click += (s, e) =>
            {
                ShowEntryDetailsDialog(entry);
            };

            return card;
        }

        private void ShowEntryDetailsDialog(TimeEntry entry)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Session Details");

            string message = $"Date: {entry.ClockInTime:MMMM dd, yyyy}\n" +
                             $"Clock In: {entry.ClockInTime:hh:mm:ss tt}\n";

            if (entry.ClockOutTime.HasValue)
            {
                message += $"Clock Out: {entry.ClockOutTime.Value:hh:mm:ss tt}\n" +
                           $"Total Hours: {entry.TotalHours:F2}h\n";
            }
            else
            {
                message += "Status: Currently Active\n";
            }

            if (!string.IsNullOrEmpty(entry.Notes))
            {
                message += $"\nNotes: {entry.Notes}";
            }

            builder.SetMessage(message);
            builder.SetPositiveButton("Close", (s, args) => { });

            if (entry.Status != "active")
            {
                builder.SetNeutralButton("Edit Notes", (s, args) =>
                {
                    ShowEditNotesDialog(entry);
                });
            }

            builder.Show();
        }

        private void ShowEditNotesDialog(TimeEntry entry)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Edit Notes");

            var input = new EditText(this);
            input.Text = entry.Notes ?? "";
            builder.SetView(input);

            builder.SetPositiveButton("Save", (s, args) =>
            {
                if (dbHelper != null && dbHelper.UpdateTimeEntryNotes(entry.EntryId, input.Text))
                {
                    Toast.MakeText(this, "Notes updated!", ToastLength.Short)?.Show();
                    LoadHistory();
                }
            });

            builder.SetNegativeButton("Cancel", (s, args) => { });
            builder.Show();
        }

        private void ClockInButton_Click(object? sender, EventArgs e)
        {
            if (dbHelper == null) return;

            int entryId = dbHelper.ClockIn(userId);

            if (entryId > 0)
            {
                activeEntry = dbHelper.GetActiveTimeEntry(userId);
                UpdateUIForClockedIn();
                StartTimer();
                UpdateTodaySummary();
                LoadHistory();
                Toast.MakeText(this, "Clocked in successfully!", ToastLength.Short)?.Show();
            }
            else
            {
                Toast.MakeText(this, "Error clocking in", ToastLength.Short)?.Show();
            }
        }

        private void ClockOutButton_Click(object? sender, EventArgs e)
        {
            if (dbHelper == null || activeEntry == null) return;

            bool success = dbHelper.ClockOut(activeEntry.EntryId);

            if (success)
            {
                activeEntry = null;
                UpdateUIForClockedOut();
                UpdateTodaySummary();
                LoadHistory();
                Toast.MakeText(this, "Clocked out successfully!", ToastLength.Short)?.Show();
            }
            else
            {
                Toast.MakeText(this, "Error clocking out", ToastLength.Short)?.Show();
            }
        }

        private void AddNotesButton_Click(object? sender, EventArgs e)
        {
            if (activeEntry == null)
            {
                Toast.MakeText(this, "Please clock in first", ToastLength.Short)?.Show();
                return;
            }

            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Add Notes");

            var input = new EditText(this);
            input.Hint = "Enter notes for this session...";
            input.Text = activeEntry.Notes ?? "";
            builder.SetView(input);

            builder.SetPositiveButton("Save", (s, args) =>
            {
                string notes = input.Text;
                activeEntry.Notes = notes;
                if (dbHelper != null && dbHelper.UpdateTimeEntryNotes(activeEntry.EntryId, notes))
                {
                    Toast.MakeText(this, "Notes saved!", ToastLength.Short)?.Show();
                    LoadHistory();
                }
            });

            builder.SetNegativeButton("Cancel", (s, args) => { });
            builder.Show();
        }

        private int DpToPx(int dp)
        {
            float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
            return (int)(dp * density + 0.5f);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            timerHandler?.RemoveCallbacksAndMessages(null);
            dbHelper?.Close();
        }
    }
}
