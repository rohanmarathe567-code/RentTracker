namespace RentTrackerClient.Models
{
    public class FinancialSummary
    {
        public string PropertyId { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetCashflow { get; set; }
        public DateRangePeriod DateRange { get; set; } = new DateRangePeriod();
        public Dictionary<string, decimal> IncomeByCategory { get; set; } = new Dictionary<string, decimal>();
        public Dictionary<string, decimal> ExpensesByCategory { get; set; } = new Dictionary<string, decimal>();
    }

    public class DateRangePeriod
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}