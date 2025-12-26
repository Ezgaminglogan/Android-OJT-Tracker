using Android.Content;
using Android.Widget;
using OJT_InternTrack.Activities;
using OJT_InternTrack.Database;
using AndroidX.SwipeRefreshLayout.Widget;
using Android.Views;
using Android.OS;
using OJT_InternTrack.Utils;

namespace OJT_InternTrack
{
    [Activity(Label = "Dashboard", Theme = "@style/AppTheme")]
    public class MainActivity : Activity
    {
        private ImageView? profileImage;
        private TextView? userFullNameText;
        private TextView? userEmailText;
        private TextView? userStudentIdText;
        private Button? editProfileButton;
        private ImageButton? logoutButton;
        private ImageButton? infoButton;
        private TextView? totalHoursValue;
        private TextView? tasksCompletedValue;
        private TextView? targetHoursValue;
        private LinearLayout? timeTrackingButton;
        private LinearLayout? scheduleButton;
        private LinearLayout? tasksButton;
        private LinearLayout? reportsButton;
        private LinearLayout? activitiesContainer;
        private TextView? noActivitiesText;
        private SwipeRefreshLayout? swipeRefreshLayout;
        private ProgressBar? ojtProgressBar;
        private TextView? completionPercentageText;
        private TextView? estimatedCompletionDate;
        private LinearLayout? nextShiftCard;
        private TextView? nextShiftTitle;
        private TextView? nextShiftTime;

        private DatabaseHelper? dbHelper;
        private int userId;
        private Handler? statsHandler;
        private bool isStatsTimerRunning;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // Get user ID
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            userId = prefs?.GetInt("user_id", -1) ?? -1;

            // Initialize database helper
            dbHelper = new DatabaseHelper(this);

            // Initialize views
            InitializeViews();

            // Load user info and data
            LoadUserInfo();
            LoadDashboardData();

            // Setup event handlers
            SetupEventHandlers();

            statsHandler = new Handler(Looper.MainLooper!);
        }

        private void InitializeViews()
        {
            profileImage = FindViewById<ImageView>(Resource.Id.profileImage);
            userFullNameText = FindViewById<TextView>(Resource.Id.userFullNameText);
            userEmailText = FindViewById<TextView>(Resource.Id.userEmailText);
            userStudentIdText = FindViewById<TextView>(Resource.Id.userStudentIdText);
            editProfileButton = FindViewById<Button>(Resource.Id.editProfileButton);
            logoutButton = FindViewById<ImageButton>(Resource.Id.logoutButton);
            infoButton = FindViewById<ImageButton>(Resource.Id.infoButton);
            totalHoursValue = FindViewById<TextView>(Resource.Id.totalHoursValue);
            tasksCompletedValue = FindViewById<TextView>(Resource.Id.tasksCompletedValue);
            targetHoursValue = FindViewById<TextView>(Resource.Id.targetHoursValue);
            timeTrackingButton = FindViewById<LinearLayout>(Resource.Id.timeTrackingButton);
            scheduleButton = FindViewById<LinearLayout>(Resource.Id.scheduleButton);
            tasksButton = FindViewById<LinearLayout>(Resource.Id.tasksButton);
            reportsButton = FindViewById<LinearLayout>(Resource.Id.reportsButton);
            activitiesContainer = FindViewById<LinearLayout>(Resource.Id.activitiesContainer);
            noActivitiesText = FindViewById<TextView>(Resource.Id.noActivitiesText);
            swipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefreshLayout);
            ojtProgressBar = FindViewById<ProgressBar>(Resource.Id.ojtProgressBar);
            completionPercentageText = FindViewById<TextView>(Resource.Id.completionPercentageText);
            estimatedCompletionDate = FindViewById<TextView>(Resource.Id.estimatedCompletionDate);
            nextShiftCard = FindViewById<LinearLayout>(Resource.Id.nextShiftCard);
            nextShiftTitle = FindViewById<TextView>(Resource.Id.nextShiftTitle);
            nextShiftTime = FindViewById<TextView>(Resource.Id.nextShiftTime);
        }

        private void LoadUserInfo()
        {
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            string? userEmail = prefs?.GetString("user_email", "");

            if (dbHelper != null && !string.IsNullOrEmpty(userEmail))
            {
                string fullName = dbHelper.GetUserFullName(userEmail);

                if (userFullNameText != null)
                {
                    userFullNameText.Text = fullName;
                }

                if (userEmailText != null)
                {
                    userEmailText.Text = userEmail;
                }

                // Load student ID
                var db = dbHelper.ReadableDatabase;
                if (db != null)
                {
                    var cursor = db.RawQuery(
                        $"SELECT {DatabaseHelper.ColStudentId} FROM {DatabaseHelper.TableUsers} WHERE {DatabaseHelper.ColEmail} = ?",
                        new[] { userEmail }
                    );

                    if (cursor != null && cursor.MoveToFirst())
                    {
                        string studentId = cursor.GetString(0) ?? "N/A";
                        if (userStudentIdText != null)
                        {
                            userStudentIdText.Text = $"ID: {studentId}";
                        }
                    }
                    cursor?.Close();
                }
            }

            // Load profile image
            LoadProfileImage();
        }

        private void LoadProfileImage()
        {
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            string? imagePath = prefs?.GetString("profile_image_path", null);

            if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
            {
                try
                {
                    var bitmap = Android.Graphics.BitmapFactory.DecodeFile(imagePath);
                    if (bitmap != null && profileImage != null)
                    {
                        // Create circular bitmap
                        var circularBitmap = GetCircularBitmap(bitmap);
                        profileImage.SetImageBitmap(circularBitmap);
                        bitmap.Recycle();
                    }
                }
                catch
                {
                    // If loading fails, keep default image
                }
            }
        }

        private Android.Graphics.Bitmap GetCircularBitmap(Android.Graphics.Bitmap bitmap)
        {
            int size = Math.Min(bitmap.Width, bitmap.Height);

            var output = Android.Graphics.Bitmap.CreateBitmap(size, size, Android.Graphics.Bitmap.Config.Argb8888!);
            var canvas = new Android.Graphics.Canvas(output);

            var paint = new Android.Graphics.Paint();
            var rect = new Android.Graphics.Rect(0, 0, size, size);

            paint.AntiAlias = true;
            canvas.DrawARGB(0, 0, 0, 0);
            paint.Color = Android.Graphics.Color.White;

            // Draw circle
            canvas.DrawCircle(size / 2f, size / 2f, size / 2f, paint);

            // Cut out the middle
            paint.SetXfermode(new Android.Graphics.PorterDuffXfermode(Android.Graphics.PorterDuff.Mode.SrcIn!));

            // Center the source bitmap
            int xOffset = (bitmap.Width - size) / 2;
            int yOffset = (bitmap.Height - size) / 2;
            var srcRect = new Android.Graphics.Rect(xOffset, yOffset, xOffset + size, yOffset + size);

            canvas.DrawBitmap(bitmap, srcRect, rect, paint);

            return output;
        }

        private void LoadDashboardData()
        {
            if (dbHelper == null || userId == -1) return;

            // Load completed hours
            double totalCompletedHours = dbHelper.GetTotalHoursWorked(userId);

            // Check for active session
            var activeEntry = dbHelper.GetActiveTimeEntry(userId);
            double currentSessionHours = 0;
            if (activeEntry?.ClockInTime != null)
            {
                currentSessionHours = (DateTime.Now - activeEntry.ClockInTime.Value).TotalHours;
            }

            if (totalHoursValue != null)
            {
                totalHoursValue.Text = $"{(totalCompletedHours + currentSessionHours):F1}";
            }

            // Load completed tasks count
            int tasksCompleted = dbHelper.GetCompletedTasksCount(userId);
            if (tasksCompletedValue != null)
            {
                tasksCompletedValue.Text = tasksCompleted.ToString();
            }

            // Load recent activities
            LoadRecentActivities();

            // Load OJT Progress
            LoadOJTProgress(totalCompletedHours + currentSessionHours);

            // Load Next Shift
            LoadNextShift();
        }

        private void LoadOJTProgress(double totalHours)
        {
            if (dbHelper == null || userId == -1) return;

            // Get user settings for accurate projection
            int requiredHours = 600;
            DateTime? ojtStartDate = null;
            bool[] workDays = new bool[7] { true, true, true, true, true, false, false }; // Mon-Sun
            TimeSpan shiftStart = new TimeSpan(8, 0, 0);
            TimeSpan shiftEnd = new TimeSpan(17, 0, 0);
            TimeSpan breakStart = new TimeSpan(12, 0, 0);
            TimeSpan breakEnd = new TimeSpan(13, 0, 0);

            var db = dbHelper.ReadableDatabase;
            if (db != null)
            {
                var cursor = db.RawQuery(
                    $"SELECT {DatabaseHelper.ColRequiredHours}, {DatabaseHelper.ColOJTStartDate}, {DatabaseHelper.ColWorkDays}, " +
                    $"{DatabaseHelper.ColFixedShiftStart}, {DatabaseHelper.ColFixedShiftEnd}, {DatabaseHelper.ColBreakStart}, {DatabaseHelper.ColBreakEnd} " +
                    $"FROM {DatabaseHelper.TableUsers} WHERE {DatabaseHelper.ColUserId} = ?",
                    new[] { userId.ToString() }
                );

                if (cursor != null && cursor.MoveToFirst())
                {
                    requiredHours = cursor.GetInt(0);
                    if (requiredHours <= 0) requiredHours = 600;

                    string? startDateStr = cursor.GetString(1);
                    if (!string.IsNullOrEmpty(startDateStr) && DateTime.TryParse(startDateStr, out var sd))
                        ojtStartDate = sd;

                    string workDaysStr = cursor.GetString(2) ?? "1,1,1,1,1,0,0";
                    workDays = workDaysStr.Split(',').Select(s => s.Trim() == "1").ToArray();

                    if (TimeSpan.TryParse(cursor.GetString(3), out var ts)) shiftStart = ts;
                    if (TimeSpan.TryParse(cursor.GetString(4), out var te)) shiftEnd = te;
                    if (TimeSpan.TryParse(cursor.GetString(5), out var bs)) breakStart = bs;
                    if (TimeSpan.TryParse(cursor.GetString(6), out var be)) breakEnd = be;
                }
                cursor?.Close();
            }

            // Update Target Hours Card
            if (targetHoursValue != null)
                targetHoursValue.Text = requiredHours.ToString();

            // Calculate percentage
            int percentage = (int)((totalHours / requiredHours) * 100);
            if (percentage > 100) percentage = 100;

            if (ojtProgressBar != null) ojtProgressBar.Progress = percentage;
            if (completionPercentageText != null) completionPercentageText.Text = $"{percentage}%";

            // Project completion date dynamically
            if (estimatedCompletionDate != null)
            {
                double remainingHours = requiredHours - totalHours;
                if (remainingHours <= 0)
                {
                    estimatedCompletionDate.Text = "Goal Reached! OJT Completed.";
                }
                else
                {
                    // Calculate hours per workday
                    double netHoursPerDay = (shiftEnd - shiftStart).TotalHours - (breakEnd - breakStart).TotalHours;
                    if (netHoursPerDay <= 0) netHoursPerDay = 8.0;

                    // Project forward skipping non-workdays
                    DateTime current = DateTime.Today;
                    if (ojtStartDate.HasValue && ojtStartDate.Value > current)
                        current = ojtStartDate.Value;

                    double accumulated = 0;
                    int daysCount = 0;
                    while (accumulated < remainingHours && daysCount < 1000) // Safety break at 1000 days
                    {
                        // Sunday is 0 in DayOfWeek, but in our array Mon=0...Sun=6
                        // DayOfWeek: Sun=0, Mon=1, Tue=2, Wed=3, Thu=4, Fri=5, Sat=6
                        int dayIdx = (int)current.DayOfWeek - 1;
                        if (dayIdx < 0) dayIdx = 6; // Sunday fix

                        if (workDays[dayIdx])
                        {
                            accumulated += netHoursPerDay;
                        }

                        if (accumulated < remainingHours)
                        {
                            current = current.AddDays(1);
                            daysCount++;
                        }
                    }

                    estimatedCompletionDate.Text = $"Est. Finish: {current:MMM dd, yyyy}";
                }
            }
        }

        private void LoadNextShift()
        {
            if (dbHelper == null || userId == -1) return;

            var schedules = dbHelper.GetSchedules(userId);
            var nextShift = schedules
                .Where(s => s.StartDate.Date >= DateTime.Today && !s.IsCompleted)
                .OrderBy(s => s.StartDate)
                .ThenBy(s => s.StartTime)
                .FirstOrDefault();

            if (nextShift != null)
            {
                if (nextShiftCard != null) nextShiftCard.Visibility = ViewStates.Visible;
                if (nextShiftTitle != null) nextShiftTitle.Text = nextShift.Title;
                if (nextShiftTime != null)
                {
                    string dateStr = nextShift.IsToday() ? "Today" : nextShift.StartDate.ToString("MMM dd");
                    nextShiftTime.Text = $"{dateStr} • {nextShift.GetFormattedTime()}";
                }

                if (nextShiftCard != null)
                {
                    nextShiftCard.Click += (s, e) =>
                    {
                        StartActivity(new Intent(this, typeof(Activities.ScheduleActivity)));
                    };
                }
            }
            else
            {
                if (nextShiftCard != null) nextShiftCard.Visibility = ViewStates.Gone;
            }
        }

        private void UpdateRealTimeStats()
        {
            if (!isStatsTimerRunning || dbHelper == null || userId == -1) return;

            // Check if there is an active session to update
            var activeEntry = dbHelper.GetActiveTimeEntry(userId);
            if (activeEntry?.ClockInTime != null)
            {
                double completedHours = dbHelper.GetTotalHoursWorked(userId);
                double currentSessionHours = (DateTime.Now - activeEntry.ClockInTime.Value).TotalHours;

                if (totalHoursValue != null)
                {
                    totalHoursValue.Text = $"{(completedHours + currentSessionHours):F1}";
                }

                // Update progress bar in real-time too
                LoadOJTProgress(completedHours + currentSessionHours);
            }

            // Run again in 5 seconds (dashboard doesn't need 1s resolution, but feels real-time)
            statsHandler?.PostDelayed(UpdateRealTimeStats, 5000);
        }

        private void LoadRecentActivities()
        {
            if (dbHelper == null || activitiesContainer == null) return;

            // Clear existing activities
            activitiesContainer.RemoveAllViews();

            // Get recent activities from database
            var activities = dbHelper.GetRecentActivities(userId, 5);

            if (activities.Count == 0)
            {
                // Show no activities message
                if (noActivitiesText != null)
                {
                    noActivitiesText.Visibility = Android.Views.ViewStates.Visible;
                }
            }
            else
            {
                // Hide no activities message
                if (noActivitiesText != null)
                {
                    noActivitiesText.Visibility = Android.Views.ViewStates.Gone;
                }

                // Add each activity
                foreach (var activity in activities)
                {
                    var activityView = CreateActivityView(activity);
                    activitiesContainer.AddView(activityView);
                }
            }
        }

        private View CreateActivityView(ActivityItem activity)
        {
            var inflater = LayoutInflater.From(this);
            var view = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.MatchParent,
                    LinearLayout.LayoutParams.WrapContent)
            };

            // Set margins and padding
            var layoutParams = (LinearLayout.LayoutParams)view.LayoutParameters;
            layoutParams.SetMargins(0, 0, 0, DpToPx(8));
            view.SetPadding(DpToPx(16), DpToPx(16), DpToPx(16), DpToPx(16));
            view.SetBackgroundResource(Resource.Drawable.card_background);

            // Icon
            var icon = new ImageView(this)
            {
                LayoutParameters = new LinearLayout.LayoutParams(DpToPx(48), DpToPx(48))
            };
            icon.SetImageResource(activity.GetIcon());
            icon.SetPadding(DpToPx(12), DpToPx(12), DpToPx(12), DpToPx(12));
            icon.SetColorFilter(Android.Graphics.Color.White);
            icon.SetBackgroundResource(Resource.Drawable.activity_icon_background);
            var iconParams = (LinearLayout.LayoutParams)icon.LayoutParameters;
            iconParams.SetMargins(0, 0, DpToPx(12), 0);
            view.AddView(icon);

            // Content container
            var contentLayout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical,
                LayoutParameters = new LinearLayout.LayoutParams(
                    0,
                    LinearLayout.LayoutParams.WrapContent,
                    1f)
            };

            // Title
            var title = new TextView(this)
            {
                Text = activity.Title,
                TextSize = 16f
            };
            title.SetTypeface(null, Android.Graphics.TypefaceStyle.Bold);
            title.SetTextColor(Android.Graphics.Color.ParseColor("#1A202C"));
            contentLayout.AddView(title);

            // Date
            var date = new TextView(this)
            {
                Text = activity.GetFormattedDate(),
                TextSize = 12f
            };
            date.SetTextColor(Android.Graphics.Color.ParseColor("#718096"));
            var dateParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WrapContent,
                LinearLayout.LayoutParams.WrapContent);
            dateParams.SetMargins(0, DpToPx(4), 0, 0);
            date.LayoutParameters = dateParams;
            contentLayout.AddView(date);

            view.AddView(contentLayout);

            return view;
        }

        private int DpToPx(int dp)
        {
            float density = Resources?.DisplayMetrics?.Density ?? 1f;
            return (int)(dp * density);
        }

        private void SetupEventHandlers()
        {
            if (editProfileButton != null)
            {
                editProfileButton.Click += EditProfileButton_Click;
            }

            if (logoutButton != null)
            {
                logoutButton.Click += LogoutButton_Click;
            }

            if (timeTrackingButton != null)
            {
                timeTrackingButton.Click += TimeTrackingButton_Click;
            }

            if (scheduleButton != null)
            {
                scheduleButton.Click += ScheduleButton_Click;
            }

            if (tasksButton != null)
            {
                tasksButton.Click += TasksButton_Click;
            }

            if (reportsButton != null)
            {
                reportsButton.Click += (s, e) =>
                {
                    StartActivity(new Intent(this, typeof(Activities.ReportsActivity)));
                };
            }

            if (swipeRefreshLayout != null)
            {
                // Set emerald color for refresh indicator
                swipeRefreshLayout.SetColorSchemeColors(
                    Android.Graphics.Color.ParseColor("#10B981"),
                    Android.Graphics.Color.ParseColor("#059669"),
                    Android.Graphics.Color.ParseColor("#047857")
                );
                swipeRefreshLayout.Refresh += SwipeRefreshLayout_Refresh;
            }

            // Info button to show About screen
            if (infoButton != null)
            {
                infoButton.Click += (s, e) =>
                {
                    StartActivity(new Intent(this, typeof(Activities.AboutActivity)));
                };
            }
        }

        private void EditProfileButton_Click(object? sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(ProfileEditActivity));
            StartActivity(intent);
        }

        private void LogoutButton_Click(object? sender, EventArgs e)
        {
            // Clear only session-specific data, preserving persistent settings like profile image path
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            var editor = prefs?.Edit();
            if (editor != null)
            {
                editor.Remove("is_logged_in");
                editor.Remove("user_email");
                editor.Remove("user_id");
                editor.Apply();
            }

            // Navigate back to login
            var intent = new Intent(this, typeof(Activities.LoginActivity));
            intent.SetFlags(ActivityFlags.ClearTask | ActivityFlags.NewTask);
            StartActivity(intent);
            Finish();
        }

        private void TimeTrackingButton_Click(object? sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(Activities.TimeTrackingActivity));
            StartActivity(intent);
        }

        private void ScheduleButton_Click(object? sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(ScheduleActivity));
            StartActivity(intent);
        }

        private void TasksButton_Click(object? sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(Activities.TasksActivity));
            StartActivity(intent);
        }

        private async void SwipeRefreshLayout_Refresh(object? sender, EventArgs e)
        {
            // Simulate data refresh
            await Task.Delay(1000);

            // Reload all data
            LoadUserInfo();
            LoadDashboardData();

            // Show toast
            Toast.MakeText(this, "Dashboard refreshed!", ToastLength.Short)?.Show();

            // Stop refreshing animation
            if (swipeRefreshLayout != null)
            {
                swipeRefreshLayout.Refreshing = false;
            }
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Check if user is logged in
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            bool isLoggedIn = prefs?.GetBoolean("is_logged_in", false) ?? false;

            if (!isLoggedIn)
            {
                // Not logged in, redirect to login
                var intent = new Intent(this, typeof(LoginActivity));
                StartActivity(intent);
                Finish();
                return;
            }

            // Reload user info and dashboard data when returning to this activity
            LoadUserInfo();
            LoadDashboardData();

            // Start real-time stats timer
            isStatsTimerRunning = true;
            UpdateRealTimeStats();
        }

        protected override void OnPause()
        {
            base.OnPause();
            isStatsTimerRunning = false;
            statsHandler?.RemoveCallbacksAndMessages(null);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            isStatsTimerRunning = false;
            statsHandler?.RemoveCallbacksAndMessages(null);
            dbHelper?.Close();
        }
    }
}
