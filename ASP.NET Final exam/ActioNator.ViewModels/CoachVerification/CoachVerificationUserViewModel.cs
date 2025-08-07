using System.Collections.Generic;

namespace ActioNator.ViewModels.CoachVerification
{
    public class CoachVerificationUserViewModel
    {
        public string UserId { get; set; }
        public List<CoachDocumentViewModel> Documents { get; set; }
    }
}
