using System;
using System.Collections.Generic;

namespace FinalExamUI.ViewModels.UserProfile
{
    public class OverviewTabViewModel
    {
        public Guid UserId { get; set; }
        public string Bio { get; set; }
        public List<EducationItem> Education { get; set; } = new List<EducationItem>();
        public List<WorkExperienceItem> WorkExperience { get; set; } = new List<WorkExperienceItem>();
        public List<string> Skills { get; set; } = new List<string>();
        public List<string> Interests { get; set; } = new List<string>();
        public Dictionary<string, string> SocialLinks { get; set; } = new Dictionary<string, string>();
    }

    public class EducationItem
    {
        public string School { get; set; }
        public string Degree { get; set; }
        public string FieldOfStudy { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrentlyStudying { get; set; }
        public string Description { get; set; }
    }

    public class WorkExperienceItem
    {
        public string Company { get; set; }
        public string Position { get; set; }
        public string Location { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsCurrentlyWorking { get; set; }
        public string Description { get; set; }
    }
}
