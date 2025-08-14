using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActioNator.Data.Models;
using ActioNator.Services.Implementations.ReportVerificationService;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace WebTests.Services
{
    [TestFixture]
    public class ReportReviewServiceTests
    {
        private static TestInMemoryModerationDbContext CreateDb()
            => new TestInMemoryModerationDbContext($"moderation-{Guid.NewGuid()}");

        private static (ApplicationUser user, ApplicationUser reporter) SeedUsers(TestInMemoryModerationDbContext db)
        {
            var u = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "author",
                FirstName = "John",
                LastName = "Doe",
                ProfilePictureUrl = "https://example.com/p.png",
                IsDeleted = false
            };
            var r = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "reporter",
                FirstName = "Jane",
                LastName = "Roe",
                ProfilePictureUrl = "https://example.com/r.png",
                IsDeleted = false
            };
            db.Users.AddRange(u, r);
            db.SaveChanges();
            return (u, r);
        }

        private static Post SeedPost(TestInMemoryModerationDbContext db, ApplicationUser author, string? content, bool isPublic = true, bool isDeleted = false)
        {
            var p = new Post
            {
                Id = Guid.NewGuid(),
                UserId = author.Id,
                ApplicationUser = author,
                Content = content,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                IsPublic = isPublic,
                IsDeleted = isDeleted
            };
            db.Posts.Add(p);
            db.SaveChanges();
            return p;
        }

        private static Comment SeedComment(TestInMemoryModerationDbContext db, ApplicationUser author, Post post, string content, bool isDeleted = false)
        {
            var c = new Comment
            {
                Id = Guid.NewGuid(),
                AuthorId = author.Id,
                Author = author,
                PostId = post.Id,
                Post = post,
                Content = content,
                CreatedAt = DateTime.UtcNow.AddMinutes(-9),
                IsDeleted = isDeleted
            };
            db.Comments.Add(c);
            db.SaveChanges();
            return c;
        }

        private static void SeedPostReports(TestInMemoryModerationDbContext db, Post post, ApplicationUser reporter, int count, string reason = "Spam", string status = "Sent")
        {
            var now = DateTime.UtcNow;
            var list = new List<PostReport>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new PostReport
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    Post = post,
                    ReportedByUserId = reporter.Id,
                    ReportedByUser = reporter,
                    Reason = reason,
                    Details = $"details-{i}",
                    CreatedAt = now.AddMinutes(-i - 1),
                    Status = status,
                    ReviewNotes = string.Empty
                });
            }
            db.PostReports.AddRange(list);
            db.SaveChanges();
        }

        private static void SeedCommentReports(TestInMemoryModerationDbContext db, Comment comment, ApplicationUser reporter, int count, string reason = "Abuse", string status = "Sent")
        {
            var now = DateTime.UtcNow;
            var list = new List<CommentReport>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new CommentReport
                {
                    Id = Guid.NewGuid(),
                    CommentId = comment.Id,
                    Comment = comment,
                    ReportedByUserId = reporter.Id,
                    ReportedByUser = reporter,
                    Reason = reason,
                    Details = $"c-details-{i}",
                    CreatedAt = now.AddMinutes(-i - 2),
                    Status = status,
                    ReviewNotes = string.Empty
                });
            }
            db.CommentReports.AddRange(list);
            db.SaveChanges();
        }

        [Test]
        public async Task GetReportedPostsAsync_Aggregates_Sorts_And_Previews()
        {
            using var db = CreateDb();
            var (author, reporter) = SeedUsers(db);
            var longContent = new string('x', 120);
            var p1 = SeedPost(db, author, longContent, isPublic: true);
            var p2 = SeedPost(db, author, "short", isPublic: true);
            SeedPostReports(db, p1, reporter, 3, reason: "Spam");
            SeedPostReports(db, p2, reporter, 1, reason: "Offensive");

            var sut = new ReportReviewService(db);
            var list = await sut.GetReportedPostsAsync();

            Assert.That(list.Count, Is.EqualTo(2));
            // Aggregation and ordering by TotalReports desc
            Assert.That(list.First().PostId, Is.EqualTo(p1.Id));
            Assert.That(list.First().TotalReports, Is.EqualTo(3));
            // Preview trimming to 100 + ...
            Assert.That(list.First().ContentPreview!.Length, Is.EqualTo(103));
            StringAssert.EndsWith("...", list.First().ContentPreview);
            // Earliest report time used
            var earliest = db.PostReports.Where(r => r.PostId == p1.Id).Min(r => r.CreatedAt);
            Assert.That(list.First().ReportedAt, Is.EqualTo(earliest));
        }

        [Test]
        public async Task GetReportedCommentsAsync_Aggregates_Sorts_And_Previews()
        {
            using var db = CreateDb();
            var (author, reporter) = SeedUsers(db);
            var p = SeedPost(db, author, "parent", isPublic: true);
            var longComment = new string('y', 140);
            var c1 = SeedComment(db, author, p, longComment);
            var c2 = SeedComment(db, author, p, "short comment");
            SeedCommentReports(db, c1, reporter, 2);
            SeedCommentReports(db, c2, reporter, 1);

            var sut = new ReportReviewService(db);
            var list = await sut.GetReportedCommentsAsync();

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list.First().CommentId, Is.EqualTo(c1.Id));
            Assert.That(list.First().TotalReports, Is.EqualTo(2));
            Assert.That(list.First().ContentPreview!.Length, Is.EqualTo(103));
            StringAssert.EndsWith("...", list.First().ContentPreview);
            var earliest = db.CommentReports.Where(r => r.CommentId == c1.Id).Min(r => r.CreatedAt);
            Assert.That(list.First().ReportedAt, Is.EqualTo(earliest));
        }

        [Test]
        public async Task GetPendingPostReportsCountAsync_Counts_Distinct_Sent_Excluding_Deleted()
        {
            using var db = CreateDb();
            var (author, reporter) = SeedUsers(db);
            var p1 = SeedPost(db, author, "p1", isPublic: true);
            var p2 = SeedPost(db, author, "p2", isPublic: true);
            var p3 = SeedPost(db, author, "p3", isPublic: true, isDeleted: true); // should be excluded
            SeedPostReports(db, p1, reporter, 2, status: "Sent");
            SeedPostReports(db, p1, reporter, 1, status: "Dismissed"); // not counted
            SeedPostReports(db, p2, reporter, 1, status: "Sent");
            SeedPostReports(db, p3, reporter, 1, status: "Sent"); // excluded due to deleted post

            var sut = new ReportReviewService(db);
            var count = await sut.GetPendingPostReportsCountAsync();
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetPendingCommentReportsCountAsync_Counts_Distinct_Sent_Excluding_Deleted()
        {
            using var db = CreateDb();
            var (author, reporter) = SeedUsers(db);
            var p = SeedPost(db, author, "p", isPublic: true);
            var c1 = SeedComment(db, author, p, "c1");
            var c2 = SeedComment(db, author, p, "c2");
            var c3 = SeedComment(db, author, p, "c3", isDeleted: true); // should be excluded
            SeedCommentReports(db, c1, reporter, 2, status: "Sent");
            SeedCommentReports(db, c1, reporter, 1, status: "Dismissed");
            SeedCommentReports(db, c2, reporter, 1, status: "Sent");
            SeedCommentReports(db, c3, reporter, 1, status: "Sent");

            var sut = new ReportReviewService(db);
            var count = await sut.GetPendingCommentReportsCountAsync();
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task DeletePostAsync_Removes_Post_And_Reports()
        {
            using var db = CreateDb();
            var (author, reporter) = SeedUsers(db);
            var p = SeedPost(db, author, "content", isPublic: true);
            SeedPostReports(db, p, reporter, 2);

            var sut = new ReportReviewService(db);
            var ok = await sut.DeletePostAsync(p.Id);
            Assert.That(ok, Is.True);
            var post = db.Posts.IgnoreQueryFilters().Single(x => x.Id == p.Id);
            Assert.That(post.IsDeleted, Is.True);
            Assert.That(db.PostReports.Any(r => r.PostId == p.Id), Is.False);
        }

        [Test]
        public void DeletePostAsync_Throws_When_NotFound()
        {
            using var db = CreateDb();
            var sut = new ReportReviewService(db);
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.DeletePostAsync(Guid.NewGuid()));
        }

        [Test]
        public async Task DeleteCommentAsync_Removes_Comment_And_Reports()
        {
            using var db = CreateDb();
            var (author, reporter) = SeedUsers(db);
            var p = SeedPost(db, author, "content", isPublic: true);
            var c = SeedComment(db, author, p, "comment");
            SeedCommentReports(db, c, reporter, 2);

            var sut = new ReportReviewService(db);
            var ok = await sut.DeleteCommentAsync(c.Id);
            Assert.That(ok, Is.True);
            var comment = db.Comments.IgnoreQueryFilters().Single(x => x.Id == c.Id);
            Assert.That(comment.IsDeleted, Is.True);
            Assert.That(db.CommentReports.Any(r => r.CommentId == c.Id), Is.False);
        }

        [Test]
        public async Task DeleteCommentAsync_ReturnsFalse_When_NotFound()
        {
            using var db = CreateDb();
            var sut = new ReportReviewService(db);
            var ok = await sut.DeleteCommentAsync(Guid.NewGuid());
            Assert.That(ok, Is.False);
        }

        [Test]
        public async Task DismissPostReportAsync_Removes_Reports_And_ReturnsTrue()
        {
            using var db = CreateDb();
            var (author, reporter) = SeedUsers(db);
            var p = SeedPost(db, author, "content", isPublic: true);
            SeedPostReports(db, p, reporter, 3);

            var sut = new ReportReviewService(db);
            var ok = await sut.DismissPostReportAsync(p.Id);
            Assert.That(ok, Is.True);
            Assert.That(db.PostReports.Any(r => r.PostId == p.Id), Is.False);
            // Post remains
            Assert.That(db.Posts.Any(x => x.Id == p.Id), Is.True);
        }

        [Test]
        public async Task DismissPostReportAsync_ReturnsFalse_When_NoReports()
        {
            using var db = CreateDb();
            var (author, _) = SeedUsers(db);
            var p = SeedPost(db, author, "content", isPublic: true);

            var sut = new ReportReviewService(db);
            var ok = await sut.DismissPostReportAsync(p.Id);
            Assert.That(ok, Is.False);
        }

        [Test]
        public async Task DismissCommentReportAsync_Removes_Reports_And_ReturnsTrue()
        {
            using var db = CreateDb();
            var (author, reporter) = SeedUsers(db);
            var p = SeedPost(db, author, "content", isPublic: true);
            var c = SeedComment(db, author, p, "comment");
            SeedCommentReports(db, c, reporter, 2);

            var sut = new ReportReviewService(db);
            var ok = await sut.DismissCommentReportAsync(c.Id);
            Assert.That(ok, Is.True);
            Assert.That(db.CommentReports.Any(r => r.CommentId == c.Id), Is.False);
            Assert.That(db.Comments.Any(x => x.Id == c.Id), Is.True);
        }

        [Test]
        public async Task DismissCommentReportAsync_ReturnsFalse_When_NoReports()
        {
            using var db = CreateDb();
            var (author, _) = SeedUsers(db);
            var p = SeedPost(db, author, "content", isPublic: true);
            var c = SeedComment(db, author, p, "comment");

            var sut = new ReportReviewService(db);
            var ok = await sut.DismissCommentReportAsync(c.Id);
            Assert.That(ok, Is.False);
        }
    }
}
