using System;
using System.Collections.Generic;

namespace FinalExamUI.ViewModels.UserProfile
{
    public class FriendsTabViewModel
    {
        public Guid UserId { get; set; }
        public int TotalFriendsCount { get; set; }
        public List<FriendItem> Friends { get; set; } = new List<FriendItem>();
        public List<FriendItem> MutualFriends { get; set; } = new List<FriendItem>();
        public List<FriendItem> FriendSuggestions { get; set; } = new List<FriendItem>();
        public Dictionary<string, int> FriendsByLocation { get; set; } = new Dictionary<string, int>();
    }

    public class FriendItem
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Headline { get; set; }
        public string Location { get; set; }
        public int MutualFriendsCount { get; set; }
        public DateTime FriendsSince { get; set; }
        public bool IsOnline { get; set; }
        public FriendshipStatus Status { get; set; }
    }

    public enum FriendshipStatus
    {
        Friend,
        Pending,
        Requested,
        None
    }
}
