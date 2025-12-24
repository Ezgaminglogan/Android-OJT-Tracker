namespace OJT_InternTrack.Models
{
    public class TimeEntry
    {
        public int EntryId { get; set; }
        public int UserId { get; set; }
        public DateTime? ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }
        public double TotalHours { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "active"; // active, completed, cancelled

        // Helper methods
        public string GetFormattedClockIn()
        {
            return ClockInTime?.ToString("hh:mm tt") ?? "--:--";
        }

        public string GetFormattedClockOut()
        {
            return ClockOutTime?.ToString("hh:mm tt") ?? "--:--";
        }

        public string GetFormattedDate()
        {
            return ClockInTime?.ToString("MMM dd, yyyy") ?? "";
        }

        public string GetDuration()
        {
            if (ClockOutTime.HasValue && ClockInTime.HasValue)
            {
                var duration = ClockOutTime.Value - ClockInTime.Value;
                return $"{duration.Hours}h {duration.Minutes}m";
            }
            else if (ClockInTime.HasValue)
            {
                var duration = DateTime.Now - ClockInTime.Value;
                return $"{(int)duration.TotalHours}h {duration.Minutes}m (ongoing)";
            }
            return "0h 0m";
        }

        public bool IsActive()
        {
            return Status == "active" && ClockInTime.HasValue && !ClockOutTime.HasValue;
        }

        public void CalculateTotalHours()
        {
            if (ClockInTime.HasValue && ClockOutTime.HasValue)
            {
                TotalHours = (ClockOutTime.Value - ClockInTime.Value).TotalHours;
            }
        }
    }
}
