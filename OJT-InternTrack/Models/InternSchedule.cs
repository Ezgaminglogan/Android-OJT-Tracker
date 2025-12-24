namespace OJT_InternTrack.Models
{
    public class InternSchedule
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan BreakStart { get; set; }
        public TimeSpan BreakEnd { get; set; }
        public string Location { get; set; } = string.Empty;
        public bool IsRecurring { get; set; }
        public string RecurringDays { get; set; } = string.Empty; // "Mon,Wed,Fri"
        public bool AlarmEnabled { get; set; }
        public int AlarmMinutesBefore { get; set; } = 5;
        public bool IsCompleted { get; set; }
        public string Type { get; set; } = "Work"; // Work, Training, Meeting, etc.
        public string AlarmSoundUri { get; set; } = string.Empty;

        public string GetFormattedDate()
        {
            return StartDate.ToString("MMM dd, yyyy");
        }

        public string GetFormattedTime()
        {
            var start = DateTime.Today.Add(StartTime);
            var end = DateTime.Today.Add(EndTime);
            return $"{start:hh:mm tt} - {end:hh:mm tt}";
        }

        public string GetAlarmTime()
        {
            var alarmDateTime = StartDate.Date + StartTime - TimeSpan.FromMinutes(AlarmMinutesBefore);
            return alarmDateTime.ToString("hh:mm tt");
        }

        public bool IsToday()
        {
            return StartDate.Date == DateTime.Today;
        }

        public bool IsUpcoming()
        {
            return StartDate >= DateTime.Today && !IsCompleted;
        }
    }
}
