using System;
using System.Collections.Generic;
using System.Linq;

namespace SchoolPortal.Models.Configuration
{
    /// <summary>
    /// Configuration for class-based balance allocation.
    /// Defines the default balance amount for each class level when a student is registered/enrolled.
    /// </summary>
    public static class ClassBalanceConfiguration
    {
        /// <summary>
        /// Dictionary mapping class names to their default balance amounts
        /// </summary>
        private static readonly Dictionary<string, decimal> ClassBalances = new()
        {
            // Creche level
            { "Creche", 10000m },
            {"playgroup", 10000m },
            
            // Kindergarten level
            { "Kindergarten", 20000m },
            {"Nursery", 25000m },
            
            // Primary levels (Basic 1 to 6)
            { "Basic 1", 30000m },
            { "Basic 2", 35000m },
            { "Basic 3", 45000m },
            { "Basic 4", 55000m },
            { "Basic 5", 65000m },
            { "Basic 6", 70000m },
            
        };

        /// <summary>
        /// Get the default balance amount for a specific class
        /// </summary>
        /// <param name="className">The name of the class (e.g., "Creche", "Basic 1", "JHS 2")</param>
        /// <returns>The default balance amount for the class, or 0 if class not found</returns>
        public static decimal GetBalanceForClass(string? className)
        {
            if (string.IsNullOrWhiteSpace(className))
                return 0m;

            if (ClassBalances.TryGetValue(className, out var balance))
                return balance;

            return 0m;
        }

        /// <summary>
        /// Get all configured classes and their balance amounts
        /// </summary>
        /// <returns>Dictionary of class name to balance amount</returns>
        public static Dictionary<string, decimal> GetAllClassBalances()
        {
            return new Dictionary<string, decimal>(ClassBalances);
        }

        /// <summary>
        /// Get list of all configured class names
        /// </summary>
        /// <returns>List of class names</returns>
        public static List<string> GetAllClasses()
        {
            return ClassBalances.Keys.ToList();
        }

        /// <summary>
        /// Check if a class is configured
        /// </summary>
        /// <param name="className">The name of the class</param>
        /// <returns>True if the class is configured, false otherwise</returns>
        public static bool IsClassConfigured(string? className)
        {
            if (string.IsNullOrWhiteSpace(className))
                return false;

            return ClassBalances.ContainsKey(className);
        }
    }
}
