using System.Collections.Generic;

namespace FinalExamUI.ViewModels.CoachVerification
{
    public class CoachVerificationUserViewModel
    {
        public string UserId { get; set; }
        public List<CoachDocumentViewModel> Documents { get; set; }
    }
}
