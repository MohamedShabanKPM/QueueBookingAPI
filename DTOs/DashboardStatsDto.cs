namespace QueueBookingAPI.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalToday { get; set; }
        public int Waiting { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public List<EmployeeStatsDto> EmployeeStats { get; set; } = new();
        public TimeStatsDto? TimeStats { get; set; }
    }

    public class EmployeeStatsDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public string AverageTime { get; set; } = "00:00";
        public string MinTime { get; set; } = "00:00";
        public string MaxTime { get; set; } = "00:00";
    }

    public class TimeStatsDto
    {
        public string Average { get; set; } = "00:00";
        public string Min { get; set; } = "00:00";
        public string Max { get; set; } = "00:00";
        public int TotalCompleted { get; set; }
    }
}

