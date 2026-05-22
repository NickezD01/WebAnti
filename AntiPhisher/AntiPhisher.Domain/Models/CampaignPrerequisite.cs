using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Domain.Models
{
    public partial class Phase
    {
        public int PhaseId { get; set; }

        public string PhaseName { get; set; }

        public string Description { get; set; }

        public int OrderIndex { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
    }

    // Bảng theo dõi tiến độ học lý thuyết của User
    public partial class Module
    {
        public int ModuleId { get; set; }

        public int PhaseId { get; set; }

        public string ModuleName { get; set; }

        public int OrderIndex { get; set; }

        public bool IsActive { get; set; }

        public virtual Phase Phase { get; set; }

        public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
    public partial class Lesson
    {
        public int LessonId { get; set; }

        public int ModuleId { get; set; }

        public string Title { get; set; }

        public string TheoryContent { get; set; }

        public string SimulationGuide { get; set; }

        public int OrderIndex { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public virtual Module Module { get; set; }

        public virtual ICollection<CampaignPrerequisite> CampaignPrerequisites { get; set; } = new List<CampaignPrerequisite>();

        public virtual ICollection<UserLessonProgress> UserLessonProgresses { get; set; } = new List<UserLessonProgress>();
    }

    public partial class CampaignPrerequisite
    {
        public int Id { get; set; }

        public int CampaignId { get; set; }

        public int RequiredLessonId { get; set; }

        public virtual Campaign Campaign { get; set; }

        public virtual Lesson RequiredLesson { get; set; }
    }

    public partial class UserLessonProgress
    {
        public int ProgressId { get; set; }

        public int UserId { get; set; } // Đồng bộ kiểu int khớp hoàn toàn với UserId trong file User.cs của bạn

        public int LessonId { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; }

        public virtual Lesson Lesson { get; set; }

        public virtual User User { get; set; }
    }
}
