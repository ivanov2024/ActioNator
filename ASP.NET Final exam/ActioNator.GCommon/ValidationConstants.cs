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
            public const int NameMinLength = 3;
            public const int NameMaxLength = 300;

            public const int MinSets = 1;
            public const int MaxSets = 100;

            public const int MinReps = 1;
            public const int MaxReps = 100;

            public const int MinWeight = 0;
            public const int MaxWeight = 1000;

            public const int NotesMinLength = 0;
            public const int NotesMaxLength = 500;
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
    }
}
