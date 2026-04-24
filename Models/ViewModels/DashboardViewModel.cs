using SchoolPortal.Models;

public class DashboardViewModel
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal Balance { get; set; }

    public List<MonthlyIncomeDto>? MonthlyIncome { get; set; }
    public List<MonthlyExpenseDto>? MonthlyExpenses { get; set; }

    // Admin Dashboard
    public int PendingPayments { get; set; }
    public int TotalStudents { get; set; }
    public int TotalStaff { get; set; }

    // Bursar Dashboard
    public List<ExpenseCategoryDto>? ExpenseByCategory { get; set; }
    public int InitiatedPaymentsCount { get; set; }
    public decimal TotalSchoolFeesPaid { get; set; }
    public List<ClassFeeBreakdownDto>? ClassFeeBreakdown { get; set; }
    public int ApprovedPaymentsCount { get; set; }

    // Teacher Dashboard
    public List<SalaryHistoryDto>? SalaryHistory { get; set; }
    public string? TeacherName { get; set; }
    public string? TeacherPosition { get; set; }
    public decimal TeacherTotalSalaryPaid { get; set; }
    public decimal TeacherPendingSalary { get; set; }

    // Student Dashboard
    public string? StudentName { get; set; }
    public string? StudentClass { get; set; }
    public decimal StudentTotalPaid { get; set; }
    public decimal StudentBalance { get; set; }
    public List<Payment>? StudentPayments { get; set; }
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

public class ExpenseCategoryDto
{
    public string? Category { get; set; }
    public decimal Total { get; set; }
    public int Count { get; set; }
}

public class ClassFeeBreakdownDto
{
    public string? ClassName { get; set; }
    public decimal TotalExpected { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalBalance { get; set; }
    public int StudentCount { get; set; }
}

public class SalaryHistoryDto
{
    public string? Month { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
}
