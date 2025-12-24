using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using OJT_InternTrack.Database;
using OJT_InternTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OJT_InternTrack.Activities
{
    [Activity(Label = "Work History", Theme = "@style/Theme.AppCompat.Light.NoActionBar")]
    public class TimesheetActivity : Activity
    {
        private LinearLayout? historyListContainer;
        private TextView? totalHistoryHours;
        private TextView? totalSessionsCount;
        private ImageButton? backButton;
        private SwipeRefreshLayout? swipeRefreshLayout;
        private DatabaseHelper? dbHelper;
        private int userId;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_timesheet);

            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            userId = prefs?.GetInt("user_id", -1) ?? -1;

            if (userId == -1)
            {
                Finish();
                return;
            }

            dbHelper = new DatabaseHelper(this);
            InitializeViews();
            LoadAllHistory();
        }

        private void InitializeViews()
        {
            historyListContainer = FindViewById<LinearLayout>(Resource.Id.historyListContainer);
            totalHistoryHours = FindViewById<TextView>(Resource.Id.totalHistoryHours);
            totalSessionsCount = FindViewById<TextView>(Resource.Id.totalSessionsCount);
            backButton = FindViewById<ImageButton>(Resource.Id.backButton);
            swipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefreshLayout);

            if (backButton != null)
            {
                backButton.Click += (s, e) => Finish();
            }

            if (swipeRefreshLayout != null)
            {
                swipeRefreshLayout.SetColorSchemeColors(Android.Graphics.Color.ParseColor("#10B981"));
                swipeRefreshLayout.Refresh += (s, e) =>
                {
                    LoadAllHistory();
                    swipeRefreshLayout.Refreshing = false;
                };
            }
        }

        private void LoadAllHistory()
        {
            if (dbHelper == null || historyListContainer == null) return;

            var entries = dbHelper.GetTimeEntries(userId, 100); // Show last 100 entries
            historyListContainer.RemoveAllViews();

            double totalHours = entries.Where(e => e.Status == "completed").Sum(e => e.TotalHours);
            if (totalHistoryHours != null) totalHistoryHours.Text = totalHours.ToString("F1");
            if (totalSessionsCount != null) totalSessionsCount.Text = entries.Count.ToString();

            if (entries.Count == 0)
            {
                var empty = new TextView(this)
                {
                    Text = "No history available.",
                    Gravity = Android.Views.GravityFlags.Center,
                    LayoutParameters = new LinearLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, 200)
                };
                historyListContainer.AddView(empty);
                return;
            }

            foreach (var entry in entries)
            {
                var view = CreateHistoryItemView(entry);
                historyListContainer.AddView(view);
            }
        }

        private Android.Views.View CreateHistoryItemView(TimeEntry entry)
        {
            var card = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.WrapContent)
                {
                    BottomMargin = 16
                }
            };
            card.SetPadding(20, 20, 20, 20);
            card.SetBackgroundResource(Resource.Drawable.quick_action_card);
            card.SetGravity(Android.Views.GravityFlags.CenterVertical);
            card.Elevation = 4;

            // Icon Container
            var iconCircle = new FrameLayout(this)
            {
                LayoutParameters = new LinearLayout.LayoutParams(40, 40) { RightMargin = 16 }
            };
            iconCircle.SetBackgroundResource(entry.Status == "active" ? Resource.Drawable.circle_background_light_blue : Resource.Drawable.circle_background_light_purple);
            
            var icon = new TextView(this)
            {
                Text = entry.Status == "active" ? "⏱️" : "✅",
                TextSize = 18,
                Gravity = Android.Views.GravityFlags.Center
            };
            iconCircle.AddView(icon);

            // Info
            var infoLayout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LinearLayout.LayoutParams(0, Android.Views.ViewGroup.LayoutParams.WrapContent, 1)
            };

            var dateText = new TextView(this)
            {
                Text = entry.ClockInTime?.ToString("dddd, MMM dd") ?? "Invalid Date",
                TextSize = 14,
                Typeface = Android.Graphics.Typeface.DefaultBold
            };
            dateText.SetTextColor(Android.Graphics.Color.ParseColor("#1A202C"));

            var subInfo = new TextView(this)
            {
                Text = entry.ClockOutTime.HasValue 
                    ? $"{entry.ClockInTime?.ToString("hh:mm tt")} - {entry.ClockOutTime.Value:hh:mm tt} • {entry.TotalHours:F1} hours"
                    : $"{entry.ClockInTime?.ToString("hh:mm tt")} - Ongoing",
                TextSize = 12
            };
            subInfo.SetTextColor(Android.Graphics.Color.ParseColor("#718096"));

            infoLayout.AddView(dateText);
            infoLayout.AddView(subInfo);

            if (!string.IsNullOrEmpty(entry.Notes))
            {
                var notes = new TextView(this)
                {
                    Text = $"\"{entry.Notes}\"",
                    TextSize = 12,
                    Typeface = Android.Graphics.Typeface.DefaultFromStyle(Android.Graphics.TypefaceStyle.Italic)
                };
                notes.SetTextColor(Android.Graphics.Color.ParseColor("#4A5568"));
                notes.SetPadding(0, 4, 0, 0);
                infoLayout.AddView(notes);
            }

            card.AddView(iconCircle);
            card.AddView(infoLayout);

            card.Click += (s, e) => ShowOptionsDialog(entry);

            return card;
        }

        private void ShowOptionsDialog(TimeEntry entry)
        {
            var options = new string[] { "View Details", "Edit Notes", "Delete Entry" };
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Options");
            builder.SetItems(options, (s, e) =>
            {
                switch (e.Which)
                {
                    case 0: ShowDetails(entry); break;
                    case 1: EditNotes(entry); break;
                    case 2: DeleteEntry(entry); break;
                }
            });
            builder.Show();
        }

        private void ShowDetails(TimeEntry entry)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Session Details");
            builder.SetMessage($"Clock In: {entry.ClockInTime:F}\n" +
                               $"Clock Out: {(entry.ClockOutTime.HasValue ? entry.ClockOutTime.Value.ToString("F") : "Active")}\n" +
                               $"Total: {entry.TotalHours:F2} hours\n\n" +
                               $"Notes: {entry.Notes}");
            builder.SetPositiveButton("OK", (s, a) => { });
            builder.Show();
        }

        private void EditNotes(TimeEntry entry)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Edit Notes");
            var input = new EditText(this) { Text = entry.Notes ?? "" };
            builder.SetView(input);
            builder.SetPositiveButton("Save", (s, a) =>
            {
                if (dbHelper != null && dbHelper.UpdateTimeEntryNotes(entry.EntryId, input.Text))
                {
                    LoadAllHistory();
                }
            });
            builder.Show();
        }

        private void DeleteEntry(TimeEntry entry)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Delete Entry");
            builder.SetMessage("Are you sure you want to delete this session? This cannot be undone.");
            builder.SetPositiveButton("Delete", (s, a) =>
            {
                var db = dbHelper?.WritableDatabase;
                if (db != null)
                {
                    db.Delete("time_entries", "entry_id = ?", new string[] { entry.EntryId.ToString() });
                    LoadAllHistory();
                }
            });
            builder.SetNegativeButton("Cancel", (s, a) => { });
            builder.Show();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            dbHelper?.Close();
        }
    }
}
