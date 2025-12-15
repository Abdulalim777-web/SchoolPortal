public class DashboardViewModel
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Balance { get; set; }

    public List<MonthlyIncomeDto>? MonthlyIncome { get; set; }
    public List<MonthlyExpenseDto>? MonthlyExpenses { get; set; }
}

public class MonthlyIncomeDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalIncome { get; set; }
}

public class MonthlyExpenseDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalExpense { get; set; }
}
