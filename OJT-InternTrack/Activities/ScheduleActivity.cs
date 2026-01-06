using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using OJT_InternTrack.Database;
using OJT_InternTrack.Models;
using OJT_InternTrack.Receivers;
using OJT_InternTrack.Utils;

namespace OJT_InternTrack.Activities
{
    [Activity(Label = "My Schedule", Theme = "@style/AppTheme")]
    public class ScheduleActivity : Activity
    {
        private ImageButton? backButton;
        private ImageButton? addScheduleButton;
        private CalendarView? calendarView;
        private Switch? todayAlarmSwitch;
        private TextView? todayTitle;
        private TextView? todayTime;
        private TextView? todayLocation;
        private TextView? editAlarmButton;
        private TextView? alarmStatusText;
        private LinearLayout? todaySessionCard;
        private TextView? currentMonthYearText;
        private LinearLayout? scheduleListContainer;
        private ImageButton? batchDeleteButton;
        private CheckBox? selectAllCheckBox;
        private HashSet<int> selectedShifts = new HashSet<int>();
        private static readonly int RequestCodeRingtone = 1001;
        private string? tempSelectedSoundUri;
        private TextView? tempSelectedSoundText;
        private InternSchedule? tempEditingSchedule;

        private List<InternSchedule> schedules = new List<InternSchedule>();
        private Database.DatabaseHelper? dbHelper;
        private int userId;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_schedule);

            InitializeViews();
            LoadData();
            SetupEventHandlers();
            UpdateMonthDisplay();
            UpdateTodaySession();
            PopulateScheduleList();

            RequestAlarmPermissions();
        }

        private void RequestAlarmPermissions()
        {
#pragma warning disable CA1416
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Android.Content.PM.Permission.Granted)
                {
                    RequestPermissions(new[] { Android.Manifest.Permission.PostNotifications }, 101);
                }
            }

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
            {
                var alarmManager = GetSystemService(AlarmService) as AlarmManager;
                if (alarmManager != null && !alarmManager.CanScheduleExactAlarms())
                {
                    var intent = new Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
                    StartActivity(intent);
                    Toast.MakeText(this, "Please allow exact alarms for internship reminders", ToastLength.Long)?.Show();
                }
            }
#pragma warning restore CA1416
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == RequestCodeRingtone && resultCode == Result.Ok && data != null)
            {
                var uri = data.GetParcelableExtra(Android.Media.RingtoneManager.ExtraRingtonePickedUri) as Android.Net.Uri;
                if (uri != null)
                {
                    tempSelectedSoundUri = uri.ToString();

                    // Update the display text in the dialog
                    var ringtone = Android.Media.RingtoneManager.GetRingtone(this, uri);
                    string name = ringtone?.GetTitle(this) ?? "Unknown Sound";

                    if (tempSelectedSoundText != null)
                    {
                        tempSelectedSoundText.Text = name;
                    }

                    if (tempEditingSchedule != null)
                    {
                        tempEditingSchedule.AlarmSoundUri = tempSelectedSoundUri ?? string.Empty;
                        UpdateAlarmInDatabase(tempEditingSchedule);
                        SetAlarm(tempEditingSchedule);
                        ToastUtils.ShowCustomToast(this, $"Sound updated to {name}");
                        tempEditingSchedule = null;
                        UpdateTodaySession();
                    }
                }
            }
        }

        private void LoadData()
        {
            dbHelper = new Database.DatabaseHelper(this);
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            if (prefs != null)
            {
                userId = prefs.GetInt("user_id", -1);
            }

            if (userId != -1)
            {
                schedules = dbHelper.GetSchedules(userId);
            }
        }

        private void UpdateMonthDisplay()
        {
            if (currentMonthYearText != null)
            {
                currentMonthYearText.Text = DateTime.Now.ToString("MMMM yyyy");
            }
        }

        private void InitializeViews()
        {
            backButton = FindViewById<ImageButton>(Resource.Id.backButton);
            addScheduleButton = FindViewById<ImageButton>(Resource.Id.addScheduleButton);
            calendarView = FindViewById<CalendarView>(Resource.Id.calendarView);
            todayAlarmSwitch = FindViewById<Switch>(Resource.Id.todayAlarmSwitch);
            todayTitle = FindViewById<TextView>(Resource.Id.todayTitle);
            todayTime = FindViewById<TextView>(Resource.Id.todayTime);
            todayLocation = FindViewById<TextView>(Resource.Id.todayLocation);
            editAlarmButton = FindViewById<TextView>(Resource.Id.editAlarmButton);
            todaySessionCard = FindViewById<LinearLayout>(Resource.Id.todaySessionCard);
            currentMonthYearText = FindViewById<TextView>(Resource.Id.currentMonthYearText);
            scheduleListContainer = FindViewById<LinearLayout>(Resource.Id.scheduleListContainer);
            alarmStatusText = FindViewById<TextView>(Resource.Id.alarmStatusText);
            int batchDeleteId = Resources.GetIdentifier("btnBatchDelete", "id", PackageName ?? "");
            int layoutId = Resources.GetIdentifier("notification_alarm", "layout", PackageName ?? "");
            if (batchDeleteId != 0) batchDeleteButton = FindViewById<ImageButton>(batchDeleteId);

            int selectAllId = Resources.GetIdentifier("cbSelectAll", "id", PackageName ?? "");
            if (selectAllId != 0) selectAllCheckBox = FindViewById<CheckBox>(selectAllId);
        }



        private void SetupEventHandlers()
        {
            if (batchDeleteButton != null)
            {
                batchDeleteButton.Click += BatchDeleteButton_Click;
            }

            if (selectAllCheckBox != null)
            {
                selectAllCheckBox.CheckedChange += SelectAllCheckBox_CheckedChange;
            }

            if (backButton != null)
            {
                backButton.Click += (s, e) => Finish();
            }

            if (addScheduleButton != null)
            {
                addScheduleButton.Click += AddScheduleButton_Click;
            }

            if (todayAlarmSwitch != null)
            {
                todayAlarmSwitch.CheckedChange += TodayAlarmSwitch_CheckedChange;
            }

            if (editAlarmButton != null)
            {
                editAlarmButton.Click += EditAlarmButton_Click;
            }

            if (calendarView != null)
            {
                calendarView.DateChange += CalendarView_DateChange;
            }

            if (todaySessionCard != null)
            {
                todaySessionCard.LongClick += (s, e) =>
                {
                    var todaySchedule = schedules.FirstOrDefault(sh => sh.IsToday());
                    if (todaySchedule != null)
                    {
                        var builder = new AlertDialog.Builder(this);
                        builder.SetTitle("Delete Shift");
                        builder.SetMessage($"Are you sure you want to remove today's shift '{todaySchedule.Title}'?");
                        builder.SetPositiveButton("Delete", (sd, args) =>
                        {
                            if (dbHelper != null && dbHelper.DeleteSchedule(todaySchedule.Id))
                            {
                                ToastUtils.ShowCustomToast(this, "Shift removed");
                                LoadData();
                                UpdateTodaySession();
                                PopulateScheduleList();
                            }
                        });
                        builder.SetNegativeButton("Cancel", (sd, args) => { });
                        builder.Show();
                    }
                };
            }
        }

        private void UpdateTodaySession()
        {
            if (dbHelper == null || userId == -1) return;

            // DYNAMIC ATTENDANCE DETECTION
            var activeEntry = dbHelper.GetActiveTimeEntry(userId);
            if (activeEntry != null && todaySessionCard != null)
            {
                // SHOW LIVE SESSION STATUS
                todaySessionCard.Visibility = ViewStates.Visible;
                todaySessionCard.SetBackgroundResource(Resource.Drawable.active_session_card_bg); // Custom blue/purple for active

                if (todayTitle != null) todayTitle.Text = "ACTIVE WORK SESSION";

                if (todayTime != null)
                {
                    TimeSpan duration = activeEntry.ClockInTime.HasValue
                        ? DateTime.Now - activeEntry.ClockInTime.Value
                        : TimeSpan.Zero;
                    todayTime.Text = $"In Progress: {duration.Hours}h {duration.Minutes}m";
                }

                if (todayLocation != null) todayLocation.Text = "Currently Clocked In";
                if (alarmStatusText != null) alarmStatusText.Text = "Auto-Detecting Hours...";
                if (todayAlarmSwitch != null) todayAlarmSwitch.Visibility = ViewStates.Gone;

                return;
            }

            // Normal Highlight Logic (if not clocked in)
            if (todayAlarmSwitch != null) todayAlarmSwitch.Visibility = ViewStates.Visible;
            if (todaySessionCard != null) todaySessionCard.SetBackgroundResource(Resource.Drawable.today_session_background);

            var highlightShift = schedules.FirstOrDefault(s => s.IsToday())
                                ?? schedules.Where(s => s.StartDate >= DateTime.Today && !s.IsCompleted)
                                            .OrderBy(s => s.StartDate).ThenBy(s => s.StartTime).FirstOrDefault();

            if (highlightShift != null && todaySessionCard != null)
            {
                todaySessionCard.Visibility = ViewStates.Visible;

                if (todayTitle != null)
                {
                    string prefix = highlightShift.IsToday() ? "" : "Next Shift: ";
                    todayTitle.Text = prefix + highlightShift.Title;
                }

                if (todayTime != null)
                {
                    string datePrefix = highlightShift.IsToday() ? "" : highlightShift.StartDate.ToString("MMM dd • ");
                    todayTime.Text = datePrefix + highlightShift.GetFormattedTime();
                }

                if (todayLocation != null)
                    todayLocation.Text = highlightShift.Location;

                if (todayAlarmSwitch != null)
                    todayAlarmSwitch.Checked = highlightShift.AlarmEnabled;

                if (alarmStatusText != null)
                {
                    if (highlightShift.AlarmEnabled)
                    {
                        var alarmTime = highlightShift.StartDate.Date + highlightShift.StartTime - TimeSpan.FromMinutes(highlightShift.AlarmMinutesBefore);
                        alarmStatusText.Text = $"Alarm: {alarmTime:hh:mm tt} ({highlightShift.AlarmMinutesBefore}m before)";
                        alarmStatusText.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        alarmStatusText.Text = "Alarm NOT set";
                        alarmStatusText.Visibility = ViewStates.Visible;
                    }
                }
            }
            else if (todaySessionCard != null)
            {
                todaySessionCard.Visibility = ViewStates.Gone;
            }
        }

        private void TodayAlarmSwitch_CheckedChange(object? sender, CompoundButton.CheckedChangeEventArgs e)
        {
            var highlightShift = GetHighlightedShift();
            if (highlightShift != null)
            {
                highlightShift.AlarmEnabled = e.IsChecked;
                UpdateAlarmInDatabase(highlightShift);

                if (e.IsChecked)
                {
                    SetAlarm(highlightShift);
                    ToastUtils.ShowCustomToast(this, $"Alarm set for {highlightShift.GetAlarmTime()}");
                }
                else
                {
                    CancelAlarm(highlightShift);
                    ToastUtils.ShowCustomToast(this, "Alarm cancelled");
                }

                UpdateTodaySession();
            }
        }

        private InternSchedule? GetHighlightedShift()
        {
            return schedules.FirstOrDefault(s => s.IsToday())
                                ?? schedules.Where(s => s.StartDate >= DateTime.Today && !s.IsCompleted)
                                            .OrderBy(s => s.StartDate).ThenBy(s => s.StartTime).FirstOrDefault();
        }

        private void EditAlarmButton_Click(object? sender, EventArgs e)
        {
            var highlightShift = GetHighlightedShift();
            if (highlightShift != null)
            {
                ShowAlarmTimePickerDialog(highlightShift);
            }
        }

        private void ShowAlarmTimePickerDialog(InternSchedule schedule)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Alarm Settings");
            builder.SetMessage("Reminder: 5 Minutes Before Shift\n\nChange notification sound?");
            builder.SetPositiveButton("Change Sound", (s, args) =>
            {
                // Select sound
                tempEditingSchedule = schedule;
                tempSelectedSoundText = null;

                var intent = new Intent(Android.Media.RingtoneManager.ActionRingtonePicker);
                intent.PutExtra(Android.Media.RingtoneManager.ExtraRingtoneTitle, "Select Alarm Sound");
                intent.PutExtra(Android.Media.RingtoneManager.ExtraRingtoneType, (int)Android.Media.RingtoneType.Alarm);
                intent.PutExtra(Android.Media.RingtoneManager.ExtraRingtoneShowDefault, true);
                intent.PutExtra(Android.Media.RingtoneManager.ExtraRingtoneShowSilent, false);
                if (!string.IsNullOrEmpty(schedule.AlarmSoundUri))
                    intent.PutExtra(Android.Media.RingtoneManager.ExtraRingtoneExistingUri, Android.Net.Uri.Parse(schedule.AlarmSoundUri));

                StartActivityForResult(intent, RequestCodeRingtone);
            });
            builder.SetNegativeButton("Cancel", (s, args) => { });
            builder.Show();
        }

        private void SetAlarm(InternSchedule schedule)
        {
            var alarmManager = GetSystemService(AlarmService) as AlarmManager;
            if (alarmManager == null) return;

            // Get user ID for auto-clock operations
            var prefs = GetSharedPreferences("OJT_InternTrack", FileCreationMode.Private);
            int currentUserId = prefs?.GetInt("user_id", -1) ?? -1;

            // 1. Reminder Alarm (X minutes before shift starts) - NOTIFICATION ONLY
            SetSingleAlarm(schedule, 0, schedule.StartTime, "Shift Starting Soon", schedule.Location, "notification", currentUserId, schedule.AlarmMinutesBefore);

            // 2. Auto Clock-In Alarm (at exact shift start time) - AUTO CLOCK IN
            SetSingleAlarm(schedule, 1, schedule.StartTime, "Shift Started - Auto Clocked In", schedule.Location, "clock_in", currentUserId, 0);

            // 3. Break Start Alarm (at break time) - BREAK START
            SetSingleAlarm(schedule, 2, schedule.BreakStart, "Break Time", "Take a rest!", "break_start", currentUserId, 0);

            // 4. Break End Alarm (when break ends) - BREAK END
            SetSingleAlarm(schedule, 3, schedule.BreakEnd, "Break Ending", "Back to work!", "break_end", currentUserId, 0);

            // 5. Auto Clock-Out Alarm (at shift end time) - AUTO CLOCK OUT
            SetSingleAlarm(schedule, 4, schedule.EndTime, "Shift Ended - Auto Clocked Out", schedule.Location, "clock_out", currentUserId, 0);
        }

        private void SetSingleAlarm(InternSchedule schedule, int typeOffset, TimeSpan time, string alarmTitle, string alarmLocation, string actionType, int userId, int minutesBefore)
        {
            var alarmManager = GetSystemService(AlarmService) as AlarmManager;
            if (alarmManager == null || !schedule.AlarmEnabled) return;

            var intent = new Intent(this, typeof(AlarmReceiver));
            intent.PutExtra("scheduleId", schedule.Id);
            intent.PutExtra("title", alarmTitle);
            intent.PutExtra("location", alarmLocation);
            intent.PutExtra("actionType", actionType); // notification, clock_in, break_start, break_end, clock_out
            intent.PutExtra("userId", userId);

            var timeDt = DateTime.Today.Add(time);
            intent.PutExtra("time", timeDt.ToString("hh:mm tt"));
            intent.PutExtra("soundUri", schedule.AlarmSoundUri);

            int requestCode = schedule.Id * 10 + typeOffset;
            var pendingIntent = PendingIntent.GetBroadcast(
                this,
                requestCode,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
            );

            if (pendingIntent == null) return;

            // Calculate alarm time (subtract minutesBefore for notification alarms, 0 for auto-clock alarms)
            var alarmDateTime = schedule.StartDate.Date + time - TimeSpan.FromMinutes(minutesBefore);

            // If alarm time has already passed for today, don't set it
            if (alarmDateTime < DateTime.Now) return;

            var alarmTimeMillis = new DateTimeOffset(alarmDateTime).ToUnixTimeMilliseconds();

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
            {
                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, alarmTimeMillis, pendingIntent);
            }
            else
            {
                alarmManager.SetExact(AlarmType.RtcWakeup, alarmTimeMillis, pendingIntent);
            }
        }

        private void CancelAlarm(InternSchedule schedule)
        {
            var alarmManager = GetSystemService(AlarmService) as AlarmManager;
            if (alarmManager == null) return;

            // Cancel all 5 alarm types
            int[] typeOffsets = { 0, 1, 2, 3, 4 };
            foreach (var offset in typeOffsets)
            {
                var intent = new Intent(this, typeof(AlarmReceiver));
                var pendingIntent = PendingIntent.GetBroadcast(
                    this,
                    schedule.Id * 10 + offset,
                    intent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                );

                if (pendingIntent != null)
                {
                    alarmManager.Cancel(pendingIntent);
                    pendingIntent.Cancel();
                }
            }
        }

        private void AddScheduleButton_Click(object? sender, EventArgs e)
        {
            ShowAddScheduleDialog();
        }

        private void ShowAddScheduleDialog()
        {
            var dialog = new AlertDialog.Builder(this).Create();
            var view = LayoutInflater.Inflate(Resource.Layout.dialog_add_schedule, null);
            if (view == null) return;
            dialog.SetView(view);


            var editTitle = view.FindViewById<EditText>(Resource.Id.editTitle);
            var editLocation = view.FindViewById<EditText>(Resource.Id.editLocation);
            var textSelectedDate = view.FindViewById<TextView>(Resource.Id.textSelectedDate);
            DateTime selectedDate = DateTime.Today;
            var textStartTime = view.FindViewById<TextView>(Resource.Id.textStartTime);
            var textEndTime = view.FindViewById<TextView>(Resource.Id.textEndTime);
            var spinnerType = view.FindViewById<Spinner>(Resource.Id.spinnerType);
            var btnSave = view.FindViewById<Button>(Resource.Id.btnSave);
            var btnCancel = view.FindViewById<Button>(Resource.Id.btnCancel);
            var datePickerContainer = view.FindViewById<LinearLayout>(Resource.Id.datePickerContainer);
            var startTimeContainer = view.FindViewById<LinearLayout>(Resource.Id.startTimeContainer);
            var endTimeContainer = view.FindViewById<LinearLayout>(Resource.Id.endTimeContainer);

            // Plan Section Views
            var planHours = view.FindViewById<EditText>(Resource.Id.planHours);
            var textPlanProjection = view.FindViewById<TextView>(Resource.Id.textPlanProjection);

            var textFixedShiftStart = view.FindViewById<TextView>(Resource.Id.textFixedShiftStart);
            var textFixedShiftEnd = view.FindViewById<TextView>(Resource.Id.textFixedShiftEnd);
            var textBreakStart = view.FindViewById<TextView>(Resource.Id.textBreakStart);
            var textBreakEnd = view.FindViewById<TextView>(Resource.Id.textBreakEnd);

            var fixedShiftStartContainer = view.FindViewById<LinearLayout>(Resource.Id.fixedShiftStartContainer);
            var fixedShiftEndContainer = view.FindViewById<LinearLayout>(Resource.Id.fixedShiftEndContainer);
            var breakStartContainer = view.FindViewById<LinearLayout>(Resource.Id.breakStartContainer);
            var breakEndContainer = view.FindViewById<LinearLayout>(Resource.Id.breakEndContainer);


            // Load Existing Plan Data
            int requiredHours = 600;
            string workDays = "1,1,1,1,1,0,0";
            TimeSpan shiftStart = new TimeSpan(8, 0, 0);
            TimeSpan shiftEnd = new TimeSpan(17, 0, 0);
            TimeSpan breakStart = new TimeSpan(12, 0, 0);
            TimeSpan breakEnd = new TimeSpan(13, 0, 0);

            if (dbHelper != null && userId != -1)
            {
                var db = dbHelper.ReadableDatabase;
                var cursor = db.RawQuery($"SELECT {DatabaseHelper.ColRequiredHours}, {DatabaseHelper.ColOJTStartDate}, {DatabaseHelper.ColWorkDays}, {DatabaseHelper.ColFixedShiftStart}, {DatabaseHelper.ColFixedShiftEnd}, {DatabaseHelper.ColBreakStart}, {DatabaseHelper.ColBreakEnd} FROM {DatabaseHelper.TableUsers} WHERE {DatabaseHelper.ColUserId} = ?", new[] { userId.ToString() });
                if (cursor != null && cursor.MoveToFirst())
                {
                    requiredHours = cursor.GetInt(0);
                    if (requiredHours <= 0) requiredHours = 600;

                    string? sdStr = cursor.GetString(1);
                    if (!string.IsNullOrEmpty(sdStr) && DateTime.TryParse(sdStr, out var sd))
                    {
                        selectedDate = sd;
                    }

                    string? wdStr = cursor.GetString(2);
                    if (!string.IsNullOrEmpty(wdStr)) workDays = wdStr;

                    if (TimeSpan.TryParse(cursor.GetString(3), out var ss)) shiftStart = ss;
                    if (TimeSpan.TryParse(cursor.GetString(4), out var se)) shiftEnd = se;
                    if (TimeSpan.TryParse(cursor.GetString(5), out var bs)) breakStart = bs;
                    if (TimeSpan.TryParse(cursor.GetString(6), out var be)) breakEnd = be;
                }
                cursor?.Close();
            }

            if (textFixedShiftStart != null) textFixedShiftStart.Text = DateTime.Today.Add(shiftStart).ToString("hh:mm tt");
            if (textFixedShiftEnd != null) textFixedShiftEnd.Text = DateTime.Today.Add(shiftEnd).ToString("hh:mm tt");
            if (textBreakStart != null) textBreakStart.Text = DateTime.Today.Add(breakStart).ToString("hh:mm tt");
            if (textBreakEnd != null) textBreakEnd.Text = DateTime.Today.Add(breakEnd).ToString("hh:mm tt");
            if (textSelectedDate != null) textSelectedDate.Text = selectedDate.ToString("MMM dd, yyyy");

            // DAY TOGGLE LOGIC
            TextView[] dayViews = new TextView[7];
            bool[] activeDays = workDays.Split(',').Select(s => s == "1").ToArray();
            if (activeDays.Length < 7) activeDays = new bool[] { true, true, true, true, true, false, false };

            for (int i = 0; i < 7; i++)
            {
                int index = i;
                dayViews[i] = view.FindViewById<TextView>(Resources.GetIdentifier($"day{i}", "id", PackageName));
                if (dayViews[i] != null)
                {
                    UpdateDayUI(dayViews[i], activeDays[index]);
                    dayViews[i].Click += (s, e) =>
                    {
                        activeDays[index] = !activeDays[index];
                        UpdateDayUI(dayViews[index], activeDays[index]);
                    };
                }
            }

            if (planHours != null) planHours.Text = requiredHours.ToString();

            // Fetch total hours worked so far for accurate projection
            double totalWorked = dbHelper?.GetTotalHoursWorked(userId) ?? 0;

            // Plan Calculation Logic
            Action updateProjection = () =>
            {
                if (textPlanProjection == null) return;
                int.TryParse(planHours?.Text, out int totalReq);
                if (totalReq <= 0) totalReq = 600;

                double remaining = totalReq - totalWorked;
                if (remaining <= 0)
                {
                    textPlanProjection.Text = "Goal Reached!";
                    return;
                }

                // Smart Calc: Only count active days minus breaks
                double totalShiftHours = (shiftEnd - shiftStart).TotalHours;
                double breakHours = (breakEnd - breakStart).TotalHours;
                if (breakHours < 0) breakHours = 0;

                double netHoursPerDay = totalShiftHours - breakHours;
                if (netHoursPerDay <= 0) netHoursPerDay = 8.0;

                int activeDaysCount = activeDays.Count(d => d);
                if (activeDaysCount == 0)
                {
                    textPlanProjection.Text = "Select work days";
                    return;
                }

                int daysNeeded = (int)Math.Ceiling(remaining / netHoursPerDay);
                DateTime current = selectedDate;
                int count = 0;

                // Check if today is a work day
                int todayIdx = ((int)current.DayOfWeek + 6) % 7;
                if (activeDays[todayIdx]) count++;

                while (count < daysNeeded)
                {
                    current = current.AddDays(1);
                    int dayIdx = ((int)current.DayOfWeek + 6) % 7;
                    if (activeDays[dayIdx]) count++;
                }

                textPlanProjection.Text = $"{current:MMM dd, yyyy}";
            };
            updateProjection();


            for (int i = 0; i < 7; i++) if (dayViews[i] != null) dayViews[i].Click += (s, e) => updateProjection();
            if (planHours != null) planHours.TextChanged += (s, e) => updateProjection();

            // Time Picker Setup
            if (fixedShiftStartContainer != null) fixedShiftStartContainer.Click += (s, e) =>
            {
                new TimePickerDialog(this, (sender, args) =>
                {
                    shiftStart = new TimeSpan(args.HourOfDay, args.Minute, 0);
                    if (textFixedShiftStart != null) textFixedShiftStart.Text = DateTime.Today.Add(shiftStart).ToString("hh:mm tt");
                    updateProjection();
                }, shiftStart.Hours, shiftStart.Minutes, false).Show();
            };
            if (fixedShiftEndContainer != null) fixedShiftEndContainer.Click += (s, e) =>
            {
                new TimePickerDialog(this, (sender, args) =>
                {
                    shiftEnd = new TimeSpan(args.HourOfDay, args.Minute, 0);
                    if (textFixedShiftEnd != null) textFixedShiftEnd.Text = DateTime.Today.Add(shiftEnd).ToString("hh:mm tt");
                    updateProjection();
                }, shiftEnd.Hours, shiftEnd.Minutes, false).Show();
            };
            if (breakStartContainer != null) breakStartContainer.Click += (s, e) =>
            {
                new TimePickerDialog(this, (sender, args) =>
                {
                    breakStart = new TimeSpan(args.HourOfDay, args.Minute, 0);
                    if (textBreakStart != null) textBreakStart.Text = DateTime.Today.Add(breakStart).ToString("hh:mm tt");
                    updateProjection();
                }, breakStart.Hours, breakStart.Minutes, false).Show();
            };
            if (breakEndContainer != null) breakEndContainer.Click += (s, e) =>
            {
                new TimePickerDialog(this, (sender, args) =>
                {
                    breakEnd = new TimeSpan(args.HourOfDay, args.Minute, 0);
                    if (textBreakEnd != null) textBreakEnd.Text = DateTime.Today.Add(breakEnd).ToString("hh:mm tt");
                    updateProjection();
                }, breakEnd.Hours, breakEnd.Minutes, false).Show();
            };

            // Default shift values
            selectedDate = DateTime.Today;
            TimeSpan startTime = shiftStart;
            TimeSpan endTime = shiftEnd;

            if (textSelectedDate != null) textSelectedDate.Text = selectedDate.ToString("MMM dd, yyyy");
            if (textStartTime != null) textStartTime.Text = DateTime.Today.Add(startTime).ToString("hh:mm tt");
            if (textEndTime != null) textEndTime.Text = DateTime.Today.Add(endTime).ToString("hh:mm tt");

            // Setup Spinner
            var types = new[] { "Work", "Meeting", "Training", "Special Event" };
            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, types);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            if (spinnerType != null) spinnerType.Adapter = adapter;

            // Date Picker
            if (datePickerContainer != null)
            {
                datePickerContainer.Click += (s, e) =>
                {
                    var picker = new DatePickerDialog(this, (sender, args) =>
                    {
                        selectedDate = args.Date;
                        if (textSelectedDate != null) textSelectedDate.Text = selectedDate.ToString("MMM dd, yyyy");
                        updateProjection();
                    }, selectedDate.Year, selectedDate.Month - 1, selectedDate.Day);
                    picker.Show();
                };
            }

            // Start Time Picker
            if (startTimeContainer != null)
            {
                startTimeContainer.Click += (s, e) =>
                {
                    var picker = new TimePickerDialog(this, (sender, args) =>
                    {
                        startTime = new TimeSpan(args.HourOfDay, args.Minute, 0);
                        if (textStartTime != null) textStartTime.Text = DateTime.Today.Add(startTime).ToString("hh:mm tt");
                    }, startTime.Hours, startTime.Minutes, false);
                    picker.Show();
                };
            }

            // End Time Picker
            if (endTimeContainer != null)
            {
                endTimeContainer.Click += (s, e) =>
                {
                    var picker = new TimePickerDialog(this, (sender, args) =>
                    {
                        endTime = new TimeSpan(args.HourOfDay, args.Minute, 0);
                        if (textEndTime != null) textEndTime.Text = DateTime.Today.Add(endTime).ToString("hh:mm tt");
                    }, endTime.Hours, endTime.Minutes, false);
                    picker.Show();
                };
            }

            if (btnCancel != null) btnCancel.Click += (s, e) => dialog.Dismiss();

            if (btnSave != null)
            {
                btnSave.Click += (s, e) =>
                {
                    // 1. Save Plan Changes
                    int.TryParse(planHours?.Text, out int hours);
                    string newWorkDays = string.Join(",", activeDays.Select(d => d ? "1" : "0"));

                    if (dbHelper != null && userId != -1)
                    {
                        var db = dbHelper.WritableDatabase;
                        var values = new ContentValues();
                        values.Put(DatabaseHelper.ColRequiredHours, hours);
                        values.Put(DatabaseHelper.ColWorkDays, newWorkDays);
                        values.Put(DatabaseHelper.ColFixedShiftStart, shiftStart.ToString(@"hh\:mm\:ss"));
                        values.Put(DatabaseHelper.ColFixedShiftEnd, shiftEnd.ToString(@"hh\:mm\:ss"));
                        values.Put(DatabaseHelper.ColBreakStart, breakStart.ToString(@"hh\:mm\:ss"));
                        values.Put(DatabaseHelper.ColBreakEnd, breakEnd.ToString(@"hh\:mm\:ss"));
                        values.Put(DatabaseHelper.ColOJTStartDate, selectedDate.ToString("yyyy-MM-dd"));
                        db.Update(DatabaseHelper.TableUsers, values, $"{DatabaseHelper.ColUserId} = ?", new[] { userId.ToString() });

                        // Regenerate future schedules based on new plan
                        dbHelper.RegenerateSchedule(userId, selectedDate);
                    }

                    // 2. Save New Shift (if title is present)
                    string title = editTitle?.Text?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(title))
                    {
                        string location = editLocation?.Text?.Trim() ?? "";
                        string type = spinnerType?.SelectedItem?.ToString() ?? "Work";

                        var newSchedule = new InternSchedule
                        {
                            Title = title,
                            Location = location,
                            Type = type,
                            StartDate = selectedDate,
                            EndDate = selectedDate,
                            StartTime = startTime,
                            EndTime = endTime,
                            BreakStart = breakStart,
                            BreakEnd = breakEnd,
                            AlarmEnabled = true,
                            AlarmMinutesBefore = 5,
                            AlarmSoundUri = tempSelectedSoundUri ?? string.Empty
                        };

                        if (dbHelper != null && userId != -1)
                        {
                            long id = dbHelper.SaveSchedule(userId, newSchedule);
                            if (id > 0)
                            {
                                newSchedule.Id = (int)id;
                                SetAlarm(newSchedule);
                            }
                        }
                    }

                    ToastUtils.ShowCustomToast(this, "Plan updated!");

                    // Refresh UI
                    LoadData();
                    UpdateTodaySession();
                    PopulateScheduleList();

                    // Check for past incomplete shifts and prompt for backfill
                    CheckAndPromptBackfill();
                    
                    dialog.Dismiss();
                };
            }

            dialog.Show();
        }

        private void UpdateDayUI(TextView view, bool active)
        {
            if (active)
            {
                view.SetBackgroundResource(Resource.Drawable.circle_day_active);
                view.SetTextColor(Android.Graphics.Color.White);
            }
            else
            {
                view.SetBackgroundResource(Resource.Drawable.circle_day_inactive);
                view.SetTextColor(Android.Graphics.Color.ParseColor("#718096"));
            }
        }

        private void PopulateScheduleList()
        {
            if (scheduleListContainer == null) return;
            scheduleListContainer.RemoveAllViews();

            var upcomingSchedules = schedules.Where(s => !s.IsToday() && s.StartDate >= DateTime.Today).OrderBy(s => s.StartDate).ToList();

            foreach (var schedule in upcomingSchedules)
            {
                var card = new LinearLayout(this)
                {
                    Orientation = Orientation.Horizontal,
                    LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
                    {
                        BottomMargin = ConvertDpToPx(12)
                    }
                };
                card.SetBackgroundResource(Resource.Drawable.quick_action_card);
                card.SetPadding(ConvertDpToPx(16), ConvertDpToPx(16), ConvertDpToPx(16), ConvertDpToPx(16));
                card.Elevation = 2;

                // Checkbox for selection
                var checkBox = new CheckBox(this)
                {
                    LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent)
                    {
                        RightMargin = ConvertDpToPx(8)
                    }
                };
                checkBox.Checked = selectedShifts.Contains(schedule.Id);
                checkBox.CheckedChange += (s, e) =>
                {
                    if (e.IsChecked) selectedShifts.Add(schedule.Id);
                    else selectedShifts.Remove(schedule.Id);
                    UpdateBatchDeleteButtonVisibility();
                };

                // Date Section
                var dateLayout = new LinearLayout(this)
                {
                    Orientation = Orientation.Vertical,
                    LayoutParameters = new LinearLayout.LayoutParams(ConvertDpToPx(60), ViewGroup.LayoutParams.WrapContent)
                    {
                        RightMargin = ConvertDpToPx(16)
                    }
                };
                dateLayout.SetGravity(GravityFlags.Center);

                var dayText = new TextView(this)
                {
                    Text = schedule.StartDate.Day.ToString(),
                    TextSize = 22,
                    Typeface = Android.Graphics.Typeface.DefaultBold
                };
                dayText.SetTextColor(Android.Graphics.Color.ParseColor("#10B981"));

                var monthText = new TextView(this)
                {
                    Text = schedule.StartDate.ToString("MMM").ToUpper(),
                    TextSize = 11
                };
                monthText.SetTextColor(Android.Graphics.Color.ParseColor("#718096"));

                dateLayout.AddView(dayText);
                dateLayout.AddView(monthText);

                // Info Section
                var infoLayout = new LinearLayout(this)
                {
                    Orientation = Orientation.Vertical,
                    LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1)
                };

                var titleText = new TextView(this)
                {
                    Text = schedule.Title,
                    TextSize = 16,
                    Typeface = Android.Graphics.Typeface.DefaultBold
                };
                titleText.SetTextColor(Android.Graphics.Color.ParseColor("#1A202C"));

                var timeText = new TextView(this)
                {
                    Text = schedule.GetFormattedTime(),
                    TextSize = 13
                };
                timeText.SetTextColor(Android.Graphics.Color.ParseColor("#718096"));
                timeText.SetPadding(0, ConvertDpToPx(4), 0, 0);

                var locText = new TextView(this)
                {
                    Text = $"📍 {schedule.Location}",
                    TextSize = 11
                };
                locText.SetTextColor(Android.Graphics.Color.ParseColor("#94A3B8"));
                locText.SetPadding(0, ConvertDpToPx(2), 0, 0);

                infoLayout.AddView(titleText);
                infoLayout.AddView(timeText);
                infoLayout.AddView(locText);

                // Switch
                var pSwitch = new Switch(this)
                {
                    Checked = schedule.AlarmEnabled
                };
                pSwitch.CheckedChange += (s, e) =>
                {
                    schedule.AlarmEnabled = e.IsChecked;
                    UpdateAlarmInDatabase(schedule);
                    if (e.IsChecked)
                    {
                        SetAlarm(schedule);
                        ToastUtils.ShowCustomToast(this, $"Alarm set for {schedule.GetAlarmTime()}");
                    }
                    else
                    {
                        CancelAlarm(schedule);
                        ToastUtils.ShowCustomToast(this, "Alarm cancelled");
                    }
                };

                card.AddView(checkBox);
                card.AddView(dateLayout);
                card.AddView(infoLayout);
                card.AddView(pSwitch);

                // Add Long Click to Delete
                card.LongClick += (s, e) =>
                {
                    var builder = new AlertDialog.Builder(this);
                    builder.SetTitle("Delete Shift");
                    builder.SetMessage($"Are you sure you want to remove '{schedule.Title}' from your schedule?");
                    builder.SetPositiveButton("Delete", (sd, args) =>
                    {
                        if (dbHelper != null && dbHelper.DeleteSchedule(schedule.Id))
                        {
                            ToastUtils.ShowCustomToast(this, "Shift removed");
                            LoadData();
                            UpdateTodaySession();
                            PopulateScheduleList();
                        }
                    });
                    builder.SetNegativeButton("Cancel", (sd, args) => { });
                    builder.Show();
                };

                scheduleListContainer.AddView(card);
            }
            UpdateBatchDeleteButtonVisibility();
        }

        private void BatchDeleteButton_Click(object? sender, EventArgs e)
        {
            if (selectedShifts.Count == 0) return;

            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Delete Selected Shifts");
            builder.SetMessage($"Are you sure you want to delete {selectedShifts.Count} selected shifts?");
            builder.SetPositiveButton("Delete", (sd, args) =>
            {
                if (dbHelper != null)
                {
                    int deletedCount = 0;
                    foreach (var id in selectedShifts)
                    {
                        if (dbHelper.DeleteSchedule(id)) deletedCount++;
                    }
                    ToastUtils.ShowCustomToast(this, $"{deletedCount} shifts removed");
                    selectedShifts.Clear();
                    LoadData();
                    UpdateTodaySession();
                    PopulateScheduleList();
                }
            });
            builder.SetNegativeButton("Cancel", (sd, args) => { });
            builder.Show();
        }

        private void SelectAllCheckBox_CheckedChange(object? sender, CompoundButton.CheckedChangeEventArgs e)
        {
            var upcomingSchedules = schedules.Where(s => !s.IsToday() && s.StartDate >= DateTime.Today).ToList();
            if (e.IsChecked)
            {
                foreach (var s in upcomingSchedules) selectedShifts.Add(s.Id);
            }
            else
            {
                // Only clear if we are genuinely unchecking manually
                // We need to be careful not to create an infinite loop if PopulateScheduleList resets this
                selectedShifts.Clear();
            }
            PopulateScheduleList();
        }

        private void UpdateBatchDeleteButtonVisibility()
        {
            if (batchDeleteButton != null)
            {
                batchDeleteButton.Visibility = selectedShifts.Count > 0 ? ViewStates.Visible : ViewStates.Gone;
            }

            // Sync Select All checkbox
            var upcomingSchedules = schedules.Where(s => !s.IsToday() && s.StartDate >= DateTime.Today).ToList();
            if (selectAllCheckBox != null && upcomingSchedules.Count > 0)
            {
                selectAllCheckBox.CheckedChange -= SelectAllCheckBox_CheckedChange;
                selectAllCheckBox.Checked = upcomingSchedules.All(s => selectedShifts.Contains(s.Id));
                selectAllCheckBox.CheckedChange += SelectAllCheckBox_CheckedChange;
            }
            else if (selectAllCheckBox != null)
            {
                selectAllCheckBox.Checked = false;
            }
        }

        private int ConvertDpToPx(int dp)
        {
            return (int)Android.Util.TypedValue.ApplyDimension(Android.Util.ComplexUnitType.Dip, dp, Resources?.DisplayMetrics);
        }

        private void CalendarView_DateChange(object? sender, CalendarView.DateChangeEventArgs e)
        {
            var selectedDate = new DateTime((int)e.Year, (int)e.Month + 1, (int)e.DayOfMonth);
            var schedulesOnDate = schedules.Where(s => s.StartDate.Date == selectedDate.Date).ToList();

            if (schedulesOnDate.Any())
            {
                var message = $"Shifts for {selectedDate:MMM dd, yyyy}:\n";
                foreach (var schedule in schedulesOnDate)
                {
                    message += $"• {schedule.Title} ({schedule.GetFormattedTime()})\n";
                }
                ToastUtils.ShowCustomToast(this, message);
            }
            else
            {
                ToastUtils.ShowCustomToast(this, "No sessions scheduled");
            }
        }

        private void UpdateAlarmInDatabase(InternSchedule schedule)
        {
            if (dbHelper == null) return;
            var db = dbHelper.WritableDatabase;
            if (db == null) return;

            var values = new ContentValues();
            values.Put("alarm_enabled", schedule.AlarmEnabled ? 1 : 0);
            values.Put("alarm_minutes", schedule.AlarmMinutesBefore);
            values.Put("alarm_sound", schedule.AlarmSoundUri);

            db.Update("schedules", values, "schedule_id = ?", new[] { schedule.Id.ToString() });
        }

        private void CheckAndPromptBackfill()
        {
            if (dbHelper == null || userId == -1) return;

            // Check for past incomplete shifts
            int pastShiftsCount = dbHelper.GetPastIncompleteShiftsCount(userId);
            
            if (pastShiftsCount > 0)
            {
                // Calculate total hours that can be backfilled
                double totalHours = dbHelper.CalculatePastShiftsHours(userId);

                // Show dialog to user
                RunOnUiThread(() =>
                {
                    var builder = new AlertDialog.Builder(this);
                    builder.SetTitle("📊 Past Shifts Detected");
                    builder.SetMessage(
                        $"Found {pastShiftsCount} past shift{(pastShiftsCount > 1 ? "s" : "")} " +
                        $"that {(pastShiftsCount > 1 ? "haven't" : "hasn't")} been logged yet.\n\n" +
                        $"💡 Auto-complete with {totalHours:F1} hours?\n\n" +
                        $"This will:\n" +
                        $"✓ Create time entries for past dates\n" +
                        $"✓ Mark those schedules as complete\n" +
                        $"✓ Update your dashboard progress\n\n" +
                        $"(Breaks are automatically deducted)"
                    );

                    builder.SetPositiveButton("✅ Auto-Complete", (s, e) =>
                    {
                        // Perform backfill
                        int backfilledCount = dbHelper.BackfillPastShifts(userId);
                        
                        if (backfilledCount > 0)
                        {
                            ToastUtils.ShowCustomToast(this, 
                                $"✅ {backfilledCount} shift{(backfilledCount > 1 ? "s" : "")} auto-completed! " +
                                $"+{totalHours:F1} hours added");
                            
                            // Refresh all data
                            LoadData();
                            UpdateTodaySession();
                            PopulateScheduleList();
                        }
                        else
                        {
                            Toast.MakeText(this, "Failed to backfill shifts", ToastLength.Short)?.Show();
                        }
                    });

                    builder.SetNegativeButton("⏭️ Skip", (s, e) =>
                    {
                        Toast.MakeText(this, "You can manually log hours in Time Tracking", ToastLength.Long)?.Show();
                    });

                    builder.SetCancelable(false); // Force user to make a choice
                    builder.Show();
                });
            }
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();
            dbHelper?.Close();
        }
    }
}
