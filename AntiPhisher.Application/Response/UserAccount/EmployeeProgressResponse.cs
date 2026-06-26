using System;
using System.Collections.Generic;

namespace AntiPhisher.Application.Response.UserAccount
{
    public class EmployeeProgressResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Tổng quan
        public int TotalLessons { get; set; }
        public int TotalLessonsCompleted { get; set; }
        public int TotalAttempts { get; set; }
        public double AverageScore { get; set; }

        // Lộ trình bài học: Phase → Module → Lesson
        public List<PhaseProgressItem> Phases { get; set; } = new();

        // Chiến dịch đã được giao
        public List<CampaignProgressItem> Campaigns { get; set; } = new();
    }

    public class PhaseProgressItem
    {
        public int PhaseId { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public List<ModuleProgressItem> Modules { get; set; } = new();
    }

    public class ModuleProgressItem
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public List<LessonProgressItem> Lessons { get; set; } = new();
    }

    public class LessonProgressItem
    {
        public int LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class CampaignProgressItem
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public int TotalScenarios { get; set; }
        public int AttemptedScenarios { get; set; }
        public int TotalAttempts { get; set; }
        public double AverageScore { get; set; }
    }
}
