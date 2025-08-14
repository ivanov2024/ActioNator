namespace ActioNator.GCommon
{
    /// <summary>
    /// Centralized validation constants for all domain entities.
    /// Provides both named constants and lookup dictionaries for fast access.
    /// </summary>
    public static class ValidationConstants
    {
        public static class ApplicationUser
        {
            public const int FirstNameMinLength = 3;
            public const int FirstNameMaxLength = 200;

            public const int LastNameMinLength = 3;
            public const int LastNameMaxLength = 200;

            /// <summary>
            /// Lookup table for ApplicationUser field lengths.
            /// </summary>
            public static readonly IReadOnlyDictionary<string, (int Min, int Max)> LengthRules =
                new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    { nameof(FirstNameMinLength), (FirstNameMinLength, FirstNameMaxLength) },
                    { nameof(LastNameMinLength), (LastNameMinLength, LastNameMaxLength) }
                };
        }

        public static class Goal
        {
            public const int TitleMinLength = 3;
            public const int TitleMaxLength = 300;

            public const int DescriptionMinLength = 10;
            public const int DescriptionMaxLength = 250;

            public static readonly IReadOnlyDictionary<string, (int Min, int Max)> LengthRules =
                new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    { nameof(TitleMinLength), (TitleMinLength, TitleMaxLength) },
                    { nameof(DescriptionMinLength), (DescriptionMinLength, DescriptionMaxLength) }
                };
        }

        public static class Exercise
        {
            public const int MinSets = 1;
            public const int MaxSets = 100;

            public const int MinReps = 1;
            public const int MaxReps = 100;

            public const int MinWeight = 0;
            public const int MaxWeight = 1000;

            public const int NotesMinLength = 0;
            public const int NotesMaxLength = 500;

            public static readonly IReadOnlyDictionary<string, (int Min, int Max)> NumericRules =
                new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    { nameof(MinSets), (MinSets, MaxSets) },
                    { nameof(MinReps), (MinReps, MaxReps) },
                    { nameof(MinWeight), (MinWeight, MaxWeight) },
                    { nameof(NotesMinLength), (NotesMinLength, NotesMaxLength) }
                };
        }

        public static class ExerciseTemplate
        {
            public const int NameMinLength = 3;
            public const int NameMaxLength = 300;

            public static readonly (int Min, int Max) NameLength = (NameMinLength, NameMaxLength);
        }

        public static class Workout
        {
            public const int TitleMinLength = 3;
            public const int TitleMaxLength = 200;

            public const int NotesMinLength = 0;
            public const int NotesMaxLength = 500;

            public static readonly IReadOnlyDictionary<string, (int Min, int Max)> LengthRules =
                new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    { nameof(TitleMinLength), (TitleMinLength, TitleMaxLength) },
                    { nameof(NotesMinLength), (NotesMinLength, NotesMaxLength) }
                };
        }

        public static class Post
        {
            public const int ContentMinLength = 0;
            public const int ContentMaxLength = 150;

            public static readonly (int Min, int Max) ContentLength = (ContentMinLength, ContentMaxLength);
        }

        public static class JournalEntry
        {
            public const int TitleMinLength = 3;
            public const int TitleMaxLength = 20;

            public const int ContentMinLength = 0;
            public const int ContentMaxLength = 150;

            public const int MoodTagMinLength = 0;
            public const int MoodTagMaxLength = 50;

            public static readonly IReadOnlyDictionary<string, (int Min, int Max)> LengthRules =
                new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    { nameof(TitleMinLength), (TitleMinLength, TitleMaxLength) },
                    { nameof(ContentMinLength), (ContentMinLength, ContentMaxLength) },
                    { nameof(MoodTagMinLength), (MoodTagMinLength, MoodTagMaxLength) }
                };

            public const string TitleRequiredMessage = "Title is required.";
            public const string TitleLengthMessage = "Title must be between {2} and {1} characters.";

            public const string ContentRequiredMessage = "Content is required.";
            public const string ContentLengthMessage = "Content must be between {2} and {1} characters.";

            public const string MoodTagRequiredMessage = "Mood tag is required.";
            public const string MoodTagLengthMessage = "Mood tag must be between {2} and {1} characters.";
        }

        public static class AchievementTemplate
        {
            public const int TitleMinLength = 3;
            public const int TitleMaxLength = 100;

            public const int DescriptionMinLength = 10;
            public const int DescriptionMaxLength = 150;

            public static readonly IReadOnlyDictionary<string, (int Min, int Max)> LengthRules =
                new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase)
                {
                    { nameof(TitleMinLength), (TitleMinLength, TitleMaxLength) },
                    { nameof(DescriptionMinLength), (DescriptionMinLength, DescriptionMaxLength) }
                };
        }

        public static class Message
        {
            public const int ContentMinLength = 1;
            public const int ContentMaxLength = 1000;

            public static readonly (int Min, int Max) ContentLength = (ContentMinLength, ContentMaxLength);
        }

        public static class Comment
        {
            public const int ContentMinLength = 1;
            public const int ContentMaxLength = 500;

            public static readonly (int Min, int Max) ContentLength = (ContentMinLength, ContentMaxLength);
        }

        public static class UserProfile
        {
            public const int AboutTextMinLength = 0;
            public const int AboutTextMaxLength = 1000;
        }

        public static class Report
        {
            public const int ReasonMaxLength = 100;
            public const int DetailsMaxLength = 1000;
            public const int StatusMaxLength = 50;
            public const int ReviewNotesMaxLength = 1000;
        }
    }
}