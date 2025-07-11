namespace ActioNator.GCommon
{
    public static class ValidationConstants
    {
        public static class Goal
        {
            public const int TitleMinLength = 3;
            public const int TitleMaxLength = 300;

            public const int DescriptionMinLength = 10;
            public const int DescriptionMaxLength = 250;
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
        }

        public static class ExerciseTemplate
        {
            public const int NameMinLength = 3;
            public const int NameMaxLength = 300;
        }

        public static class Workout
        {
            public const int TitleMinLength = 3;
            public const int TitleMaxLength = 200;

            public const int NotesMinLength = 0;
            public const int NotesMaxLength = 500;
        }

        public static class Post
        {
            public const int TitleMinLength = 3;
            public const int TitleMaxLength = 20;

            public const int ContentMinLength = 0;
            public const int ContentMaxLength = 150;
        }

        public static class JournalEntry
        {
            public const int TitleMinLength = 3;
            public const int TitleMaxLength = 20;

            public const int ContentMinLength = 0;
            public const int ContentMaxLength = 150;

            public const int MoodTagMinLength = 0;
            public const int MoodTagMaxLength = 50;
        }

        public static class AchievementTemplate
        {
            public const int TitleMinLength = 3;
            public const int TitleMaxLength = 100;

            public const int DescriptionMinLength = 10;
            public const int DescriptionMaxLength = 150;
        }

        public static class Message
        {
            public const int ContentMinLength = 1;
            public const int ContentMaxLength = 1000;
        }

        public static class Comment
        {
            public const int ContentMinLength = 1;
            public const int ContentMaxLength = 500;
        }
    }
}
