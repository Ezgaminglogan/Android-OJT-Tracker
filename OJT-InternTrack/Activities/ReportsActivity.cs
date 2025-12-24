using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using OJT_InternTrack.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OJT_InternTrack.Activities
{
    [Activity(Label = "Performance Reports", Theme = "@style/Theme.AppCompat.Light.NoActionBar")]
    public class ReportsActivity : Activity
    {
        private TextView? reportHoursText;
        private TextView? reportPercentageText;
        private ProgressBar? reportProgressBar;
        private TextView? weeklyAvgText;
        private TextView? taskRateText;
        private LinearLayout? weeklyChartContainer;
        private ImageButton? backButton;
        private SwipeRefreshLayout? swipeRefreshLayout;
        private DatabaseHelper? dbHelper;
        private int userId;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_reports);

            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            userId = prefs?.GetInt("user_id", -1) ?? -1;

            if (userId == -1)
            {
                Finish();
                return;
            }

            dbHelper = new DatabaseHelper(this);
            InitializeViews();
            LoadReportData();
        }

        private void InitializeViews()
        {
            reportHoursText = FindViewById<TextView>(Resource.Id.reportHoursText);
            reportPercentageText = FindViewById<TextView>(Resource.Id.reportPercentageText);
            reportProgressBar = FindViewById<ProgressBar>(Resource.Id.reportProgressBar);
            weeklyAvgText = FindViewById<TextView>(Resource.Id.weeklyAvgText);
            taskRateText = FindViewById<TextView>(Resource.Id.taskRateText);
            weeklyChartContainer = FindViewById<LinearLayout>(Resource.Id.weeklyChartContainer);
            backButton = FindViewById<ImageButton>(Resource.Id.backButton);
            swipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefreshLayout);

            if (backButton != null)
            {
                backButton.Click += (s, e) => Finish();
            }

            if (swipeRefreshLayout != null)
            {
                swipeRefreshLayout.SetColorSchemeColors(Android.Graphics.Color.ParseColor("#10B981"));
                swipeRefreshLayout.Refresh += (s, e) => {
                    LoadReportData();
                    swipeRefreshLayout.Refreshing = false;
                };
            }
        }

        private void LoadReportData()
        {
            if (dbHelper == null) return;

            // 1. Progress Stats
            double totalHours = dbHelper.GetTotalHoursWorked(userId);
            int requiredHours = 600; // Default
            
            // Try to get actual required hours from user table
            var db = dbHelper.ReadableDatabase;
            if (db != null)
            {
                var cursor = db.RawQuery($"SELECT {DatabaseHelper.ColRequiredHours} FROM {DatabaseHelper.TableUsers} WHERE {DatabaseHelper.ColUserId} = ?", new[] { userId.ToString() });
                if (cursor != null && cursor.MoveToFirst())
                {
                    requiredHours = cursor.GetInt(0);
                    if (requiredHours <= 0) requiredHours = 600;
                }
                cursor?.Close();
            }

            if (reportHoursText != null) reportHoursText.Text = $"{totalHours:F1} / {requiredHours}";
            int percentage = (int)((totalHours / requiredHours) * 100);
            if (reportPercentageText != null) reportPercentageText.Text = $"{percentage}%";
            if (reportProgressBar != null) reportProgressBar.Progress = Math.Min(percentage, 100);

            // 2. Task Completion Rate
            int completedTasks = dbHelper.GetCompletedTasksCount(userId);
            int pendingTasks = dbHelper.GetPendingTasksCount(userId);
            int totalTasks = completedTasks + pendingTasks;
            int taskRate = totalTasks > 0 ? (int)((double)completedTasks / totalTasks * 100) : 0;
            if (taskRateText != null) taskRateText.Text = $"{taskRate}%";

            // 3. Weekly Stats & Chart
            UpdateWeeklyStats();
        }

        private void UpdateWeeklyStats()
        {
            if (dbHelper == null || weeklyChartContainer == null) return;

            weeklyChartContainer.RemoveAllViews();
            
            // Get history for last 7 days
            var entries = dbHelper.GetTimeEntries(userId, 50);
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .Reverse()
                .ToList();

            double totalWeeklyHours = 0;
            double maxDayHours = 1; // Avoid divide by zero

            var data = last7Days.Select(date => {
                double dayHours = entries
                    .Where(e => e.ClockInTime.HasValue && e.ClockInTime.Value.Date == date.Date && e.Status == "completed")
                    .Sum(e => e.TotalHours);
                totalWeeklyHours += dayHours;
                if (dayHours > maxDayHours) maxDayHours = dayHours;
                return new { Date = date, Hours = dayHours };
            }).ToList();

            if (weeklyAvgText != null) weeklyAvgText.Text = $"{(totalWeeklyHours / 7):F1}h";

            // Build simplified bar chart
            foreach (var day in data)
            {
                var barContainer = new LinearLayout(this)
                {
                    Orientation = Orientation.Vertical,
                    LayoutParameters = new LinearLayout.LayoutParams(0, Android.Views.ViewGroup.LayoutParams.MatchParent, 1)
                };
                barContainer.SetGravity(Android.Views.GravityFlags.Bottom | Android.Views.GravityFlags.CenterHorizontal);

                // The bar itself
                var bar = new View(this)
                {
                    Background = new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.ParseColor("#10B981")),
                };
                
                int barHeight = (int)(150 * (day.Hours / maxDayHours));
                if (barHeight < 5) barHeight = 5; // Min height

                var barParams = new LinearLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, barHeight);
                barParams.SetMargins(8, 0, 8, 4);
                bar.LayoutParameters = barParams;

                // Day Label
                var label = new TextView(this)
                {
                    Text = day.Date.ToString("ddd"),
                    TextSize = 10,
                    Gravity = Android.Views.GravityFlags.Center
                };

                barContainer.AddView(bar);
                barContainer.AddView(label);
                weeklyChartContainer.AddView(barContainer);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            dbHelper?.Close();
        }
    }
}
