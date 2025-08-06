namespace FinalExamUI.ViewModels.CoachVerification
{
    public class CoachDocumentViewModel
    {
        public string FileName { get; set; }
        public string RelativePath { get; set; } // e.g., for use in links
        public string FileType { get; set; }     // image / pdf
    }
}
