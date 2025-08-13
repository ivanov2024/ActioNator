using System;
using System.Collections.Generic;

namespace ActioNator.ViewModels.Goal
{
    public class GoalsListViewModel
    {
        public List<GoalViewModel> Goals { get; set; } = new List<GoalViewModel>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 3;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 1;
    }
}
