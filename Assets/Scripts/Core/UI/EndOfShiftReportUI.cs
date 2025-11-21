using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Expo.Core.Events;
using Expo.Data;
using Expo.Core.Progression;

namespace Expo.UI
{
    /// <summary>
    /// Displays the end-of-shift report with mistakes and grade.
    /// UI Structure:
    /// - Panel (this script attached)
    ///   - ReportText (TextMeshProUGUI) - displays formatted report
    /// </summary>
    public class EndOfShiftReportUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI reportText;
        
        [Header("Manager References")]
        [SerializeField] private Expo.Managers.ShiftTimerManager shiftTimerManager;
        
        [Header("Formatting")]
        [SerializeField] private bool useRichText = true;
        [SerializeField] private int fontSize = 24;
        [SerializeField] private int headerFontSize = 32;
        [SerializeField] private float minFontSize = 12f;
        [SerializeField] private float maxFontSize = 36f;
        
        private void Start()
        {
            // Hide panel by default
            gameObject.SetActive(false);
            
            // Enable auto-sizing on the text component
            if (reportText != null)
            {
                reportText.enableAutoSizing = true;
                reportText.fontSizeMin = minFontSize;
                reportText.fontSizeMax = maxFontSize;
            }
        }
        
        /// <summary>
        /// Display the end-of-shift report with mistakes and grade.
        /// Called by ScoringManager when shift ends.
        /// </summary>
        public void DisplayReport(List<Mistake> mistakes, int totalTickets, float shiftDuration)
        {
            if (reportText == null)
            {
                Debug.LogError("[EndOfShiftReportUI] ReportText reference is missing!");
                return;
            }
            
            // Load progression config for grading
            var progressionConfig = ProgressionConfigLoader.LoadProgressionConfig();
            var gradingThresholds = progressionConfig.gradingThresholds;
            
            // Calculate grade
            int mistakeCount = mistakes.Count;
            string grade = gradingThresholds.CalculateGrade(mistakeCount);
            Color gradeColor = gradingThresholds.GetGradeColor(grade);
            
            // Build formatted report
            string report = BuildFormattedReport(mistakes, totalTickets, shiftDuration, grade, gradeColor, mistakeCount);
            
            // Display
            reportText.text = report;
            gameObject.SetActive(true);
            
            Debug.Log($"[EndOfShiftReportUI] Displayed report: {mistakeCount} mistakes, Grade: {grade}");
        }
        
        /// <summary>
        /// Build the formatted report string with all details
        /// </summary>
        private string BuildFormattedReport(List<Mistake> mistakes, int totalTickets, float shiftDuration, 
            string grade, Color gradeColor, int mistakeCount)
        {
            var report = new System.Text.StringBuilder();
            
            // Header
            if (useRichText)
            {
                report.AppendLine($"<size={headerFontSize}><b>END OF SHIFT REPORT</b></size>");
                report.AppendLine();
            }
            else
            {
                report.AppendLine("END OF SHIFT REPORT");
                report.AppendLine("===================");
            }
            
            // Summary stats
            report.AppendLine($"Tickets Served: {totalTickets}");
            report.AppendLine($"Shift Duration: {FormatSimulatedDuration(shiftDuration)}");
            report.AppendLine();
            
            // Grade (big and colorful!)
            if (useRichText)
            {
                string colorHex = ColorUtility.ToHtmlStringRGB(gradeColor);
                report.AppendLine($"<size={headerFontSize + 8}><b><color=#{colorHex}>GRADE: {grade}</color></b></size>");
            }
            else
            {
                report.AppendLine($"GRADE: {grade}");
            }
            report.AppendLine();
            
            // Mistakes section
            if (useRichText)
            {
                report.AppendLine($"<size={headerFontSize}><b>Mistakes: {mistakeCount}</b></size>");
            }
            else
            {
                report.AppendLine($"MISTAKES: {mistakeCount}");
                report.AppendLine("----------");
            }
            
            if (mistakeCount == 0)
            {
                report.AppendLine();
                if (useRichText)
                {
                    report.AppendLine("<color=#20C020><b>PERFECT SHIFT! No mistakes made!</b></color>");
                }
                else
                {
                    report.AppendLine("PERFECT SHIFT! No mistakes made!");
                }
            }
            else
            {
                report.AppendLine();
                
                // Group mistakes by type
                var staggeredCourses = mistakes.FindAll(m => m.Type == MistakeType.StaggeredCourse);
                var deadDishes = mistakes.FindAll(m => m.Type == MistakeType.DeadDish);
                var wrongTableDishes = mistakes.FindAll(m => m.Type == MistakeType.WrongTable);
                
                // Staggered courses
                if (staggeredCourses.Count > 0)
                {
                    if (useRichText)
                        report.AppendLine("<b>Staggered Courses:</b>");
                    else
                        report.AppendLine("Staggered Courses:");
                    
                    foreach (var mistake in staggeredCourses)
                    {
                        report.AppendLine($"  • {mistake.Description}");
                    }
                    report.AppendLine();
                }
                
                // Dead dishes
                if (deadDishes.Count > 0)
                {
                    if (useRichText)
                        report.AppendLine("<b>Dead Dishes:</b>");
                    else
                        report.AppendLine("Dead Dishes:");
                    
                    foreach (var mistake in deadDishes)
                    {
                        report.AppendLine($"  • {mistake.Description}");
                    }
                    report.AppendLine();
                }
                
                // Wrong table
                if (wrongTableDishes.Count > 0)
                {
                    if (useRichText)
                        report.AppendLine("<b>Wrong Table:</b>");
                    else
                        report.AppendLine("Wrong Table:");
                    
                    foreach (var mistake in wrongTableDishes)
                    {
                        report.AppendLine($"  • {mistake.Description}");
                    }
                    report.AppendLine();
                }
            }
            
            // Footer message based on grade
            report.AppendLine();
            report.AppendLine(GetGradeMessage(grade));
            
            return report.ToString();
        }
        
        /// <summary>
        /// Format shift duration using simulated time (e.g., "5:00 PM - 9:23 PM")
        /// Converts real time elapsed to simulated hours/minutes
        /// </summary>
        private string FormatSimulatedDuration(float realSecondsElapsed)
        {
            if (shiftTimerManager == null)
            {
                // Fallback to real time if no manager reference
                int minutes = Mathf.FloorToInt(realSecondsElapsed / 60f);
                int secs = Mathf.FloorToInt(realSecondsElapsed % 60f);
                return $"{minutes}m {secs}s";
            }
            
            // Use reflection to get the shift configuration from ShiftTimerManager
            // Since the fields are private, we'll calculate based on the standard configuration
            int shiftStartHour = 17; // 5PM (standard start)
            float realMinutesPerSimHour = 1f; // Standard time conversion
            
            // Calculate simulated time elapsed
            float simulatedHoursElapsed = realSecondsElapsed / (realMinutesPerSimHour * 60f);
            
            // Calculate end hour and minute
            int totalSimMinutes = shiftStartHour * 60 + Mathf.FloorToInt(simulatedHoursElapsed * 60);
            int endHour = totalSimMinutes / 60;
            int endMinute = totalSimMinutes % 60;
            
            // Format start time
            string startTime = FormatSimTime(shiftStartHour, 0);
            
            // Format end time
            string endTime = FormatSimTime(endHour, endMinute);
            
            // Calculate hours and minutes worked
            int hoursWorked = Mathf.FloorToInt(simulatedHoursElapsed);
            int minutesWorked = Mathf.FloorToInt((simulatedHoursElapsed - hoursWorked) * 60);
            
            return $"{startTime} - {endTime} ({hoursWorked}h {minutesWorked}m)";
        }
        
        /// <summary>
        /// Format simulated time to 12-hour format (e.g., "5:30 PM")
        /// </summary>
        private string FormatSimTime(int hour, int minute)
        {
            string ampm = hour >= 12 ? "PM" : "AM";
            int displayHour = hour > 12 ? hour - 12 : (hour == 0 ? 12 : hour);
            return $"{displayHour}:{minute:D2} {ampm}";
        }
        
        /// <summary>
        /// Get encouraging/feedback message based on grade
        /// </summary>
        private string GetGradeMessage(string grade)
        {
            switch (grade)
            {
                case "A":
                    return "Perfect shift. No mistakes. Do this again every day";
                case "B":
                    return "Could've been better. Try harder.";
                case "C":
                    return "What the fuck was that?";
                case "D":
                    return "Your shit got rocked.";
                case "F":
                    return "You're a fucking disaster!";
                default:
                    return "Shift complete.";
            }
        }
        
        /// <summary>
        /// Close the report (for a button click)
        /// </summary>
        public void CloseReport()
        {
            gameObject.SetActive(false);
        }
    }
}
