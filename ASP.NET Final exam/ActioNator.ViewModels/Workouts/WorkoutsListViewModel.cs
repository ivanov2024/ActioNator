using System;
using System.Collections.Generic;

namespace ActioNator.ViewModels.Workouts
{
    public class WorkoutsListViewModel
    {
        public IReadOnlyList<WorkoutCardViewModel> Workouts { get; set; } = Array.Empty<WorkoutCardViewModel>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 3;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 0;
    }
}
