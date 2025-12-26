using Android.Database.Sqlite;
using Android.Content;
using OJT_InternTrack.Models;

namespace OJT_InternTrack.Database
{
    public class DatabaseHelper : SQLiteOpenHelper
    {
        private new const string DatabaseName = "OJTInternTrack.db";
        private const int DatabaseVersion = 8;

        // Table names
        public const string TableUsers = "users";
        public const string TableSchedules = "schedules";
        public const string TableTasks = "tasks";

        // Users table columns
        public const string ColUserId = "user_id";
        public const string ColEmail = "email";
        public const string ColPassword = "password";
        public const string ColFullName = "full_name";
        public const string ColStudentId = "student_id";
        public const string ColRequiredHours = "required_hours";
        public const string ColOJTStartDate = "ojt_start_date";
        public const string ColWorkDays = "work_days"; // e.g. "1,1,1,1,1,0,0"
        public const string ColFixedShiftStart = "fixed_shift_start";
        public const string ColFixedShiftEnd = "fixed_shift_end";
        public const string ColBreakStart = "break_start";
        public const string ColBreakEnd = "break_end";
        public const string ColCreatedAt = "created_at";

        // Schedules table columns
        public const string ColScheduleId = "schedule_id";
        public const string ColTitle = "title";
        public const string ColDescription = "description";
        public const string ColStartDate = "start_date";
        public const string ColEndDate = "end_date";
        public const string ColStartTime = "start_time";
        public const string ColEndTime = "end_time";
        public const string ColLocation = "location";
        public const string ColAlarmEnabled = "alarm_enabled";
        public const string ColAlarmMinutes = "alarm_minutes";
        public const string ColIsCompleted = "is_completed";
        public const string ColType = "type";
        public const string ColAlarmSound = "alarm_sound";

        // Tasks table columns
        public const string ColTaskId = "task_id";
        public const string ColTaskTitle = "task_title";
        public const string ColTaskDescription = "task_description";
        public const string ColTaskStatus = "task_status";
        public const string ColDueDate = "due_date";
        public const string ColCompletedDate = "completed_date";

        public DatabaseHelper(Context? context)
            : base(context, DatabaseName, null, DatabaseVersion)
        {
        }

        public override void OnCreate(SQLiteDatabase? db)
        {
            if (db == null) return;

            // Create Users table
            string createUsersTable = $@"
                CREATE TABLE {TableUsers} (
                    {ColUserId} INTEGER PRIMARY KEY AUTOINCREMENT,
                    {ColEmail} TEXT UNIQUE NOT NULL,
                    {ColPassword} TEXT NOT NULL,
                    {ColFullName} TEXT,
                    {ColStudentId} TEXT,
                    {ColRequiredHours} INTEGER DEFAULT 600,
                    {ColOJTStartDate} TEXT,
                    {ColWorkDays} TEXT DEFAULT '1,1,1,1,1,0,0',
                    {ColFixedShiftStart} TEXT DEFAULT '08:00:00',
                    {ColFixedShiftEnd} TEXT DEFAULT '17:00:00',
                    {ColBreakStart} TEXT DEFAULT '12:00:00',
                    {ColBreakEnd} TEXT DEFAULT '13:00:00',
                    {ColCreatedAt} DATETIME DEFAULT CURRENT_TIMESTAMP
                )";

            // Create Schedules table
            string createSchedulesTable = $@"
                CREATE TABLE {TableSchedules} (
                    {ColScheduleId} INTEGER PRIMARY KEY AUTOINCREMENT,
                    {ColUserId} INTEGER,
                    {ColTitle} TEXT NOT NULL,
                    {ColDescription} TEXT,
                    {ColStartDate} TEXT NOT NULL,
                    {ColEndDate} TEXT,
                    {ColStartTime} TEXT NOT NULL,
                    {ColEndTime} TEXT NOT NULL,
                    {ColLocation} TEXT,
                    {ColAlarmEnabled} INTEGER DEFAULT 0,
                    {ColAlarmMinutes} INTEGER DEFAULT 30,
                    {ColIsCompleted} INTEGER DEFAULT 0,
                    {ColType} TEXT DEFAULT 'Work',
                    {ColAlarmSound} TEXT,
                    {ColBreakStart} TEXT,
                    {ColBreakEnd} TEXT,
                    {ColCreatedAt} DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY({ColUserId}) REFERENCES {TableUsers}({ColUserId})
                )";

            // Create Tasks table
            string createTasksTable = $@"
                CREATE TABLE {TableTasks} (
                    {ColTaskId} INTEGER PRIMARY KEY AUTOINCREMENT,
                    {ColUserId} INTEGER,
                    {ColTaskTitle} TEXT NOT NULL,
                    {ColTaskDescription} TEXT,
                    {ColTaskStatus} TEXT DEFAULT 'Pending',
                    {ColDueDate} TEXT,
                    {ColCompletedDate} TEXT,
                    {ColCreatedAt} DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY({ColUserId}) REFERENCES {TableUsers}({ColUserId})
                )";

            // Create TimeEntries table
            string createTimeEntriesTable = @"
                CREATE TABLE time_entries (
                    entry_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER,
                    clock_in_time DATETIME,
                    clock_out_time DATETIME,
                    total_hours REAL,
                    location TEXT,
                    notes TEXT,
                    status TEXT DEFAULT 'active',
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(user_id) REFERENCES users(user_id)
                )";

            db.ExecSQL(createUsersTable);
            db.ExecSQL(createSchedulesTable);
            db.ExecSQL(createTasksTable);
            db.ExecSQL(createTimeEntriesTable);

            // Insert default admin user (password: admin123)
            string insertAdmin = $@"
                INSERT INTO {TableUsers} ({ColEmail}, {ColPassword}, {ColFullName}, {ColStudentId})
                VALUES ('admin', 'admin123', 'Administrator', 'ADMIN001')";

            db.ExecSQL(insertAdmin);
        }

        public override void OnUpgrade(SQLiteDatabase? db, int oldVersion, int newVersion)
        {
            if (db == null) return;

            // Only add time_entries table if upgrading from version 1 to 2
            if (oldVersion < 2)
            {
                // Create TimeEntries table (new in version 2)
                string createTimeEntriesTable = @"
                    CREATE TABLE IF NOT EXISTS time_entries (
                        entry_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        user_id INTEGER,
                        clock_in_time DATETIME,
                        clock_out_time DATETIME,
                        total_hours REAL,
                        location TEXT,
                        notes TEXT,
                        status TEXT DEFAULT 'active',
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY(user_id) REFERENCES users(user_id)
                    )";

                db.ExecSQL(createTimeEntriesTable);
            }

            // Version 3: Added required_hours column to users table
            if (oldVersion < 3)
            {
                try
                {
                    db.ExecSQL($"ALTER TABLE {TableUsers} ADD COLUMN {ColRequiredHours} INTEGER DEFAULT 600");
                }
                catch
                {
                    // Column might already exist if OnCreate ran recently
                }
            }

            // Version 4: Added ojt_start_date column to users table
            if (oldVersion < 4)
            {
                try
                {
                    db.ExecSQL($"ALTER TABLE {TableUsers} ADD COLUMN {ColOJTStartDate} TEXT");
                }
                catch
                {
                    // Column might already exist
                }
            }

            // Version 5: Added work_days and fixed_shift columns to users table
            if (oldVersion < 5)
            {
                try
                {
                    db.ExecSQL($"ALTER TABLE {TableUsers} ADD COLUMN {ColWorkDays} TEXT DEFAULT '1,1,1,1,1,0,0'");
                    db.ExecSQL($"ALTER TABLE {TableUsers} ADD COLUMN {ColFixedShiftStart} TEXT DEFAULT '08:00:00'");
                    db.ExecSQL($"ALTER TABLE {TableUsers} ADD COLUMN {ColFixedShiftEnd} TEXT DEFAULT '17:00:00'");
                }
                catch
                {
                    // Columns might already exist
                }
            }

            // Version 6: Added break_start and break_end columns
            if (oldVersion < 6)
            {
                try
                {
                    db.ExecSQL($"ALTER TABLE {TableUsers} ADD COLUMN {ColBreakStart} TEXT DEFAULT '12:00:00'");
                    db.ExecSQL($"ALTER TABLE {TableUsers} ADD COLUMN {ColBreakEnd} TEXT DEFAULT '13:00:00'");
                }
                catch
                {
                    // Columns might already exist
                }
            }

            // Version 7: Added alarm_sound column
            if (oldVersion < 7)
            {
                try
                {
                    db.ExecSQL($"ALTER TABLE {TableSchedules} ADD COLUMN {ColAlarmSound} TEXT");
                }
                catch
                {
                    // Column might already exist
                }
            }

            // Version 8: Added break times to schedules table
            if (oldVersion < 8)
            {
                try
                {
                    db.ExecSQL($"ALTER TABLE {TableSchedules} ADD COLUMN {ColBreakStart} TEXT");
                    db.ExecSQL($"ALTER TABLE {TableSchedules} ADD COLUMN {ColBreakEnd} TEXT");
                }
                catch
                {
                    // Columns might already exist
                }
            }
        }

        // User authentication (Supports Email or Student ID)
        public bool ValidateUser(string identifier, string password)
        {
            var db = ReadableDatabase;
            if (db == null) return false;

            var cursor = db.RawQuery(
                $@"SELECT * FROM {TableUsers} 
                   WHERE ({ColEmail} = ? OR {ColStudentId} = ?) AND {ColPassword} = ?",
                new[] { identifier, identifier, password }
            );

            bool isValid = cursor != null && cursor.Count > 0;
            cursor?.Close();
            return isValid;
        }

        // Get user ID by Email or Student ID
        public int GetUserId(string identifier)
        {
            var db = ReadableDatabase;
            if (db == null) return -1;

            var cursor = db.RawQuery(
                $@"SELECT {ColUserId} FROM {TableUsers} 
                   WHERE {ColEmail} = ? OR {ColStudentId} = ?",
                new[] { identifier, identifier }
            );

            int userId = -1;
            if (cursor != null && cursor.MoveToFirst())
            {
                userId = cursor.GetInt(0);
            }
            cursor?.Close();
            return userId;
        }

        // Register new user
        public bool RegisterUser(string email, string password, string fullName, string studentId)
        {
            var db = WritableDatabase;
            if (db == null) return false;

            try
            {
                var values = new ContentValues();
                values.Put(ColEmail, email);
                values.Put(ColPassword, password);
                values.Put(ColFullName, fullName);
                values.Put(ColStudentId, studentId);

                long result = db.Insert(TableUsers, null, values);
                return result != -1;
            }
            catch
            {
                return false;
            }
        }

        // Get user full name
        public string GetUserFullName(string email)
        {
            var db = ReadableDatabase;
            if (db == null) return "User";

            var cursor = db.RawQuery(
                $"SELECT {ColFullName} FROM {TableUsers} WHERE {ColEmail} = ?",
                new[] { email }
            );

            string fullName = "User";
            if (cursor != null && cursor.MoveToFirst())
            {
                string? dbFullName = cursor.GetString(0);
                // Only use database value if it's not null or empty
                if (!string.IsNullOrWhiteSpace(dbFullName))
                {
                    fullName = dbFullName;
                }
            }
            cursor?.Close();
            return fullName;
        }

        // Get total hours worked from completed time entries
        public double GetTotalHoursWorked(int userId)
        {
            var db = ReadableDatabase;
            if (db == null) return 0;

            var cursor = db.RawQuery(
                $@"SELECT SUM(total_hours) 
                   FROM time_entries 
                   WHERE user_id = ? AND status = 'completed'",
                new[] { userId.ToString() }
            );

            double totalHours = 0;
            if (cursor != null && cursor.MoveToFirst())
            {
                totalHours = cursor.GetDouble(0);
                cursor.Close();
            }

            return totalHours;
        }

        // Get completed tasks count
        public int GetCompletedTasksCount(int userId)
        {
            var db = ReadableDatabase;
            if (db == null) return 0;

            var cursor = db.RawQuery(
                $"SELECT COUNT(*) FROM {TableTasks} WHERE {ColUserId} = ? AND {ColTaskStatus} = 'Completed'",
                new[] { userId.ToString() }
            );

            int count = 0;
            if (cursor != null && cursor.MoveToFirst())
            {
                count = cursor.GetInt(0);
            }
            cursor?.Close();
            return count;
        }

        // Get pending tasks count
        public int GetPendingTasksCount(int userId)
        {
            var db = ReadableDatabase;
            if (db == null) return 0;

            var cursor = db.RawQuery(
                $"SELECT COUNT(*) FROM {TableTasks} WHERE {ColUserId} = ? AND {ColTaskStatus} = 'Pending'",
                new[] { userId.ToString() }
            );

            int count = 0;
            if (cursor != null && cursor.MoveToFirst())
            {
                count = cursor.GetInt(0);
            }
            cursor?.Close();
            return count;
        }

        // Task Methods
        public List<InternTask> GetTasks(int userId)
        {
            var tasks = new List<InternTask>();
            var db = ReadableDatabase;
            if (db == null) return tasks;

            var cursor = db.RawQuery(
                $"SELECT * FROM {TableTasks} WHERE {ColUserId} = ? ORDER BY {ColCreatedAt} DESC",
                new[] { userId.ToString() }
            );

            if (cursor != null)
            {
                while (cursor.MoveToNext())
                {
                    var task = new InternTask
                    {
                        Id = cursor.GetInt(cursor.GetColumnIndex(ColTaskId)),
                        UserId = cursor.GetInt(cursor.GetColumnIndex(ColUserId)),
                        Title = cursor.GetString(cursor.GetColumnIndex(ColTaskTitle)) ?? string.Empty,
                        Description = cursor.GetString(cursor.GetColumnIndex(ColTaskDescription)) ?? string.Empty,
                        Status = cursor.GetString(cursor.GetColumnIndex(ColTaskStatus)) ?? "Pending",
                        DueDate = DateTime.TryParse(cursor.GetString(cursor.GetColumnIndex(ColDueDate)), out var dd) ? dd : null,
                        CompletedDate = DateTime.TryParse(cursor.GetString(cursor.GetColumnIndex(ColCompletedDate)), out var cd) ? cd : null,
                        CreatedAt = DateTime.TryParse(cursor.GetString(cursor.GetColumnIndex(ColCreatedAt)), out var cr) ? cr : DateTime.Now
                    };
                    tasks.Add(task);
                }
                cursor.Close();
            }
            return tasks;
        }

        public long SaveTask(int userId, InternTask task)
        {
            var db = WritableDatabase;
            if (db == null) return -1;

            var values = new ContentValues();
            values.Put(ColUserId, userId);
            values.Put(ColTaskTitle, task.Title);
            values.Put(ColTaskDescription, task.Description);
            values.Put(ColTaskStatus, task.Status);
            values.Put(ColDueDate, task.DueDate?.ToString("yyyy-MM-dd HH:mm:ss"));
            values.Put(ColCompletedDate, task.CompletedDate?.ToString("yyyy-MM-dd HH:mm:ss"));

            return db.Insert(TableTasks, null, values);
        }

        public bool UpdateTaskStatus(int taskId, string status)
        {
            var db = WritableDatabase;
            if (db == null) return false;

            var values = new ContentValues();
            values.Put(ColTaskStatus, status);
            if (status == "Completed")
            {
                values.Put(ColCompletedDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                values.PutNull(ColCompletedDate);
            }

            int rows = db.Update(TableTasks, values, $"{ColTaskId} = ?", new[] { taskId.ToString() });
            return rows > 0;
        }

        public bool DeleteTask(int taskId)
        {
            var db = WritableDatabase;
            if (db == null) return false;

            int rows = db.Delete(TableTasks, $"{ColTaskId} = ?", new[] { taskId.ToString() });
            return rows > 0;
        }

        // Get all schedules for a user
        public List<Models.InternSchedule> GetSchedules(int userId)
        {
            var schedules = new List<Models.InternSchedule>();
            var db = ReadableDatabase;
            if (db == null) return schedules;

            var cursor = db.RawQuery(
                $"SELECT * FROM {TableSchedules} WHERE {ColUserId} = ? ORDER BY {ColStartDate} ASC",
                new[] { userId.ToString() }
            );

            if (cursor != null)
            {
                while (cursor.MoveToNext())
                {
                    var schedule = new Models.InternSchedule
                    {
                        Id = cursor.GetInt(cursor.GetColumnIndex(ColScheduleId)),
                        Title = cursor.GetString(cursor.GetColumnIndex(ColTitle)) ?? string.Empty,
                        Description = cursor.GetString(cursor.GetColumnIndex(ColDescription)) ?? string.Empty,
                        StartDate = DateTime.TryParse(cursor.GetString(cursor.GetColumnIndex(ColStartDate)), out var sd) ? sd : DateTime.Today,
                        EndDate = DateTime.TryParse(cursor.GetString(cursor.GetColumnIndex(ColEndDate)), out var ed) ? ed : DateTime.Today,
                        StartTime = TimeSpan.TryParse(cursor.GetString(cursor.GetColumnIndex(ColStartTime)), out var st) ? st : TimeSpan.Zero,
                        EndTime = TimeSpan.TryParse(cursor.GetString(cursor.GetColumnIndex(ColEndTime)), out var et) ? et : TimeSpan.Zero,
                        Location = cursor.GetString(cursor.GetColumnIndex(ColLocation)) ?? string.Empty,
                        AlarmEnabled = cursor.GetInt(cursor.GetColumnIndex(ColAlarmEnabled)) == 1,
                        AlarmMinutesBefore = cursor.GetInt(cursor.GetColumnIndex(ColAlarmMinutes)),
                        IsCompleted = cursor.GetInt(cursor.GetColumnIndex(ColIsCompleted)) == 1,
                        Type = cursor.GetString(cursor.GetColumnIndex(ColType)) ?? "Work",
                        AlarmSoundUri = cursor.GetString(cursor.GetColumnIndex(ColAlarmSound)) ?? string.Empty,
                        BreakStart = TimeSpan.TryParse(cursor.GetString(cursor.GetColumnIndex(ColBreakStart)), out var bs) ? bs : new TimeSpan(12, 0, 0),
                        BreakEnd = TimeSpan.TryParse(cursor.GetString(cursor.GetColumnIndex(ColBreakEnd)), out var be) ? be : new TimeSpan(13, 0, 0)
                    };
                    schedules.Add(schedule);
                }
                cursor.Close();
            }
            return schedules;
        }

        // Save a new schedule
        public long SaveSchedule(int userId, Models.InternSchedule schedule)
        {
            var db = WritableDatabase;
            if (db == null) return -1;

            var values = new ContentValues();
            values.Put(ColUserId, userId);
            values.Put(ColTitle, schedule.Title);
            values.Put(ColDescription, schedule.Description);
            values.Put(ColStartDate, schedule.StartDate.ToString("yyyy-MM-dd"));
            values.Put(ColEndDate, schedule.EndDate.ToString("yyyy-MM-dd"));
            values.Put(ColStartTime, schedule.StartTime.ToString(@"hh\:mm\:ss"));
            values.Put(ColEndTime, schedule.EndTime.ToString(@"hh\:mm\:ss"));
            values.Put(ColLocation, schedule.Location);
            values.Put(ColAlarmEnabled, schedule.AlarmEnabled ? 1 : 0);
            values.Put(ColAlarmMinutes, schedule.AlarmMinutesBefore);
            values.Put(ColIsCompleted, schedule.IsCompleted ? 1 : 0);
            values.Put(ColType, schedule.Type);
            values.Put(ColAlarmSound, schedule.AlarmSoundUri);
            values.Put(ColBreakStart, schedule.BreakStart.ToString(@"hh\:mm\:ss"));
            values.Put(ColBreakEnd, schedule.BreakEnd.ToString(@"hh\:mm\:ss"));

            return db.Insert(TableSchedules, null, values);
        }

        // Regenerate future schedule based on new start date
        public void RegenerateSchedule(int userId, DateTime newStartDate)
        {
            var db = WritableDatabase;
            if (db == null) return;

            try
            {
                // 1. Get User Settings
                var userCursor = db.RawQuery(
                    $"SELECT {ColWorkDays}, {ColFixedShiftStart}, {ColFixedShiftEnd}, {ColBreakStart}, {ColBreakEnd}, {ColRequiredHours} FROM {TableUsers} WHERE {ColUserId} = ?",
                    new[] { userId.ToString() }
                );

                if (userCursor == null || !userCursor.MoveToFirst())
                {
                    userCursor?.Close();
                    return;
                }

                string workDaysStr = userCursor.GetString(0) ?? "1,1,1,1,1,0,0";
                string shiftStartStr = userCursor.GetString(1) ?? "08:00:00";
                string shiftEndStr = userCursor.GetString(2) ?? "17:00:00";
                string breakStartStr = userCursor.GetString(3) ?? "12:00:00";
                string breakEndStr = userCursor.GetString(4) ?? "13:00:00";
                int targetHours = userCursor.GetInt(5);
                userCursor.Close();

                // Parse settings
                var workDays = workDaysStr.Split(',').Select(s => s.Trim() == "1").ToArray(); // Mon=0, Sun=6
                TimeSpan shiftStart = TimeSpan.Parse(shiftStartStr);
                TimeSpan shiftEnd = TimeSpan.Parse(shiftEndStr);
                TimeSpan breakStart = TimeSpan.Parse(breakStartStr);
                TimeSpan breakEnd = TimeSpan.Parse(breakEndStr);

                // 2. Delete existing INCOMPLETE schedules
                // We keep completed ones as history.
                db.Delete(TableSchedules, $"{ColUserId} = ? AND {ColIsCompleted} = 0", new[] { userId.ToString() });

                // 3. Generate new schedules
                // Calculate hours per day
                double hoursPerDay = (shiftEnd - shiftStart).TotalHours - (breakEnd - breakStart).TotalHours;
                if (hoursPerDay <= 0) hoursPerDay = 8; // Fallback

                double currentHours = GetTotalHoursWorked(userId); // Use existing method
                double remainingHours = targetHours - currentHours;
                if (remainingHours <= 0) return; // Goal reached

                DateTime currentDate = newStartDate.Date;
                // If new start date is in past, catch up to today, but only generate if no conflict
                // Actually, let's just generate forward from the new start date.

                int safetyCounter = 0;
                while (remainingHours > 0 && safetyCounter < 365) // Cap at 1 year ahead
                {
                    // Check if it's a work day
                    // DayOfWeek: Sunday=0, Monday=1...
                    // Our array: index 0 is Monday (usually). Let's standardise.
                    // Default string "1,1,1,1,1,0,0" usually implies Mon->Sun
                    int dayIndex = (int)currentDate.DayOfWeek; // Sun=0, Mon=1
                    // Map C# DayOfWeek to 0-based index where 0=Mon, 6=Sun to match "1,1,1,1,1,0,0" convention
                    // If current is Sun(0) -> index 6
                    // If current is Mon(1) -> index 0
                    int workDayIndex = (dayIndex == 0) ? 6 : dayIndex - 1;

                    if (workDays.Length > workDayIndex && workDays[workDayIndex])
                    {
                        // Check if a completed schedule already exists for this date
                        var existingCursor = db.RawQuery(
                            $"SELECT count(*) FROM {TableSchedules} WHERE {ColUserId} = ? AND {ColStartDate} = ? AND {ColIsCompleted} = 1",
                            new[] { userId.ToString(), currentDate.ToString("yyyy-MM-dd") }
                        );

                        int exists = 0;
                        if (existingCursor.MoveToFirst())
                        {
                            exists = existingCursor.GetInt(0);
                        }
                        existingCursor.Close();

                        if (exists == 0)
                        {
                            // Create new schedule entry
                            var values = new ContentValues();
                            values.Put(ColUserId, userId);
                            values.Put(ColTitle, "Internship Shift");
                            values.Put(ColLocation, "Main Location");
                            values.Put(ColStartDate, currentDate.ToString("yyyy-MM-dd"));
                            values.Put(ColEndDate, currentDate.ToString("yyyy-MM-dd"));
                            values.Put(ColStartTime, shiftStartStr);
                            values.Put(ColEndTime, shiftEndStr);
                            values.Put(ColBreakStart, breakStartStr);
                            values.Put(ColBreakEnd, breakEndStr);
                            values.Put(ColType, "Work");
                            values.Put(ColIsCompleted, 0);
                            values.Put(ColAlarmEnabled, 1);
                            values.Put(ColAlarmMinutes, 5); // Default

                            db.Insert(TableSchedules, null, values);
                            remainingHours -= hoursPerDay;
                        }
                    }

                    currentDate = currentDate.AddDays(1);
                    safetyCounter++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error regenerating schedule: {ex.Message}");
            }
        }

        // Delete a schedule
        public bool DeleteSchedule(int scheduleId)
        {
            var db = WritableDatabase;
            if (db == null) return false;

            int rows = db.Delete(TableSchedules, $"{ColScheduleId} = ?", new[] { scheduleId.ToString() });
            return rows > 0;
        }

        // Get upcoming schedules count
        public int GetUpcomingSchedulesCount(int userId)
        {
            var db = ReadableDatabase;
            if (db == null) return 0;

            string today = DateTime.Today.ToString("yyyy-MM-dd");
            var cursor = db.RawQuery(
                $"SELECT COUNT(*) FROM {TableSchedules} WHERE {ColUserId} = ? AND {ColStartDate} >= ? AND {ColIsCompleted} = 0",
                new[] { userId.ToString(), today }
            );

            int count = 0;
            if (cursor != null && cursor.MoveToFirst())
            {
                count = cursor.GetInt(0);
            }
            cursor?.Close();
            return count;
        }

        // Get recent activities (recent tasks and schedules)
        public List<ActivityItem> GetRecentActivities(int userId, int limit = 5)
        {
            var activities = new List<ActivityItem>();
            var db = ReadableDatabase;
            if (db == null) return activities;

            // Get recent completed tasks
            var taskCursor = db.RawQuery(
                $@"SELECT {ColTaskTitle}, {ColCompletedDate}, 'Task' as Type 
                   FROM {TableTasks} 
                   WHERE {ColUserId} = ? AND {ColTaskStatus} = 'Completed' 
                   ORDER BY {ColCompletedDate} DESC 
                   LIMIT ?",
                new[] { userId.ToString(), limit.ToString() }
            );

            if (taskCursor != null)
            {
                while (taskCursor.MoveToNext())
                {
                    activities.Add(new ActivityItem
                    {
                        Title = taskCursor.GetString(0) ?? string.Empty,
                        Date = taskCursor.GetString(1) ?? string.Empty,
                        Type = "Task"
                    });
                }
                taskCursor.Close();
            }

            // Get recent completed schedules
            var scheduleCursor = db.RawQuery(
                $@"SELECT {ColTitle}, {ColStartDate}, 'Schedule' as Type 
                   FROM {TableSchedules} 
                   WHERE {ColUserId} = ? AND {ColIsCompleted} = 1 
                   ORDER BY {ColStartDate} DESC 
                   LIMIT ?",
                new[] { userId.ToString(), limit.ToString() }
            );

            if (scheduleCursor != null)
            {
                while (scheduleCursor.MoveToNext())
                {
                    activities.Add(new ActivityItem
                    {
                        Title = scheduleCursor.GetString(0) ?? string.Empty,
                        Date = scheduleCursor.GetString(1) ?? string.Empty,
                        Type = "Schedule"
                    });
                }
                scheduleCursor.Close();
            }

            // Sort by date and limit
            return activities.OrderByDescending(a => a.Date).Take(limit).ToList();
        }

        // Time Tracking Methods
        public int ClockIn(int userId, string? location = null)
        {
            var db = WritableDatabase;
            if (db == null) return -1;

            var values = new ContentValues();
            values.Put("user_id", userId);
            values.Put("clock_in_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            values.Put("location", location ?? "");
            values.Put("status", "active");

            long entryId = db.Insert("time_entries", null, values);
            return (int)entryId;
        }

        public bool ClockOut(int entryId, string? notes = null)
        {
            var db = WritableDatabase;
            if (db == null) return false;

            var clockOutTime = DateTime.Now;

            var cursor = db.RawQuery(
                "SELECT clock_in_time FROM time_entries WHERE entry_id = ?",
                new[] { entryId.ToString() }
            );

            if (cursor != null && cursor.MoveToFirst())
            {
                var clockInStr = cursor.GetString(0);
                cursor.Close();

                if (DateTime.TryParse(clockInStr, out var clockInTime))
                {
                    var totalHours = (clockOutTime - clockInTime).TotalHours;

                    var values = new ContentValues();
                    values.Put("clock_out_time", clockOutTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    values.Put("total_hours", totalHours);
                    values.Put("notes", notes ?? "");
                    values.Put("status", "completed");

                    int rows = db.Update("time_entries", values, "entry_id = ?", new[] { entryId.ToString() });
                    return rows > 0;
                }
            }
            return false;
        }

        public Models.TimeEntry? GetActiveTimeEntry(int userId)
        {
            var db = ReadableDatabase;
            if (db == null) return null;

            var cursor = db.RawQuery(
                @"SELECT entry_id, user_id, clock_in_time, clock_out_time, total_hours, location, notes, status 
                  FROM time_entries 
                  WHERE user_id = ? AND status = 'active' 
                  LIMIT 1",
                new[] { userId.ToString() }
            );

            Models.TimeEntry? entry = null;
            if (cursor != null && cursor.MoveToFirst())
            {
                entry = new Models.TimeEntry
                {
                    EntryId = cursor.GetInt(0),
                    UserId = cursor.GetInt(1),
                    ClockInTime = DateTime.TryParse(cursor.GetString(2), out var cin) ? cin : null,
                    ClockOutTime = cursor.IsNull(3) ? null : DateTime.TryParse(cursor.GetString(3), out var cout) ? cout : null,
                    TotalHours = cursor.GetDouble(4),
                    Location = cursor.GetString(5) ?? string.Empty,
                    Notes = cursor.GetString(6) ?? string.Empty,
                    Status = cursor.GetString(7) ?? string.Empty
                };
                cursor.Close();
            }
            return entry;
        }

        public double GetTodayTotalTimeHours(int userId)
        {
            var db = ReadableDatabase;
            if (db == null) return 0;

            string today = DateTime.Today.ToString("yyyy-MM-dd");
            var cursor = db.RawQuery(
                "SELECT SUM(total_hours) FROM time_entries WHERE user_id = ? AND DATE(clock_in_time) = ? AND status = 'completed'",
                new[] { userId.ToString(), today }
            );

            double total = 0;
            if (cursor != null && cursor.MoveToFirst() && !cursor.IsNull(0))
            {
                total = cursor.GetDouble(0);
            }
            cursor?.Close();
            return total;
        }

        // Update user profile
        public bool UpdateUserProfile(string email, string fullName, string studentId, int requiredHours, string? ojtStartDate)
        {
            var db = WritableDatabase;
            if (db == null) return false;

            try
            {
                var values = new ContentValues();
                values.Put(ColFullName, fullName);
                values.Put(ColStudentId, studentId);
                values.Put(ColRequiredHours, requiredHours);
                if (ojtStartDate != null)
                {
                    values.Put(ColOJTStartDate, ojtStartDate);
                }

                int rows = db.Update(TableUsers, values, $"{ColEmail} = ?", new[] { email });
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }

        // Update user password
        public bool UpdateUserPassword(string email, string newPassword)
        {
            var db = WritableDatabase;
            if (db == null) return false;

            try
            {
                var values = new ContentValues();
                values.Put(ColPassword, newPassword);

                int rows = db.Update(TableUsers, values, $"{ColEmail} = ?", new[] { email });
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }

        // Delete user account
        public bool DeleteUser(string email)
        {
            var db = WritableDatabase;
            if (db == null) return false;

            try
            {
                // Get user ID first
                int userId = GetUserId(email);
                if (userId == -1) return false;

                // Delete user's related data
                db.Delete(TableTasks, $"user_id = ?", new[] { userId.ToString() });
                db.Delete(TableSchedules, $"user_id = ?", new[] { userId.ToString() });
                db.ExecSQL($"DELETE FROM time_entries WHERE user_id = {userId}");

                // Delete user account
                int rows = db.Delete(TableUsers, $"{ColEmail} = ?", new[] { email });
                return rows > 0;
            }
            catch
            {
                return false;
            }
        }

        public List<TimeEntry> GetTimeEntries(int userId, int limit = 50)
        {
            var entries = new List<TimeEntry>();
            var db = ReadableDatabase;
            if (db == null) return entries;

            var cursor = db.RawQuery(
                "SELECT * FROM time_entries WHERE user_id = ? ORDER BY clock_in_time DESC LIMIT ?",
                new[] { userId.ToString(), limit.ToString() }
            );

            if (cursor != null && cursor.MoveToFirst())
            {
                do
                {
                    var entry = new TimeEntry
                    {
                        EntryId = cursor.GetInt(cursor.GetColumnIndex("entry_id")),
                        UserId = cursor.GetInt(cursor.GetColumnIndex("user_id")),
                        ClockInTime = DateTime.Parse(cursor.GetString(cursor.GetColumnIndex("clock_in_time"))),
                        ClockOutTime = cursor.IsNull(cursor.GetColumnIndex("clock_out_time")) ? null : (DateTime?)DateTime.Parse(cursor.GetString(cursor.GetColumnIndex("clock_out_time"))),
                        TotalHours = cursor.GetDouble(cursor.GetColumnIndex("total_hours")),
                    };

                    int notesIdx = cursor.GetColumnIndex("notes");
                    if (notesIdx != -1) entry.Notes = cursor.GetString(notesIdx);

                    int locIdx = cursor.GetColumnIndex("location");
                    if (locIdx != -1) entry.Location = cursor.GetString(locIdx);

                    int statusIdx = cursor.GetColumnIndex("status");
                    if (statusIdx != -1) entry.Status = cursor.GetString(statusIdx);

                    entries.Add(entry);
                } while (cursor.MoveToNext());
            }
            cursor?.Close();
            return entries;
        }

        public bool UpdateTimeEntryNotes(int entryId, string notes)
        {
            var db = WritableDatabase;
            if (db == null) return false;

            var values = new ContentValues();
            values.Put("notes", notes);

            int rows = db.Update("time_entries", values, "entry_id = ?", new[] { entryId.ToString() });
            return rows > 0;
        }
    }

    // Helper class for activities
    public class ActivityItem
    {
        public string Title { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        public string GetFormattedDate()
        {
            if (DateTime.TryParse(Date, out var date))
            {
                var diff = DateTime.Now - date;
                if (diff.TotalDays < 1) return "Today";
                if (diff.TotalDays < 2) return "Yesterday";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
                return date.ToString("MMM dd");
            }
            return Date;
        }

        public int GetIcon()
        {
            return Type == "Task" ? Resource.Drawable.ic_task : Resource.Drawable.ic_calendar;
        }
    }
}
