using System;

namespace OJT_InternTrack.Models
{
    public class InternTask
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsCompleted => Status == "Completed";
        
        public string GetStatusEmoji()
        {
            return Status switch
            {
                "Completed" => "✅",
                "In Progress" => "⏳",
                "Pending" => "📋",
                _ => "📝"
            };
        }

        public string GetFormattedDueDate()
        {
            if (!DueDate.HasValue) return "No due date";
            var diff = DueDate.Value.Date - DateTime.Today;
            if (diff.TotalDays == 0) return "Due Today";
            if (diff.TotalDays == 1) return "Due Tomorrow";
            if (diff.TotalDays < 0) return $"Overdue ({(int)Math.Abs(diff.TotalDays)}d)";
            return $"Due {DueDate.Value:MMM dd}";
        }
    }
}
