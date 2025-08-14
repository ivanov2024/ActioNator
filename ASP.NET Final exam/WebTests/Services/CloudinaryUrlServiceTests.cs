using ActioNator.Services.Implementations.Cloud;
using NUnit.Framework;

namespace WebTests.Services
{
    public class CloudinaryUrlServiceTests
    {
        private CloudinaryUrlService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _service = new CloudinaryUrlService();
        }

        [Test]
        public void GetPublicId_ReturnsEmpty_ForNullOrEmpty()
        {
            Assert.That(_service.GetPublicId(null!), Is.EqualTo(string.Empty));
            Assert.That(_service.GetPublicId(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(_service.GetPublicId("   "), Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetPublicId_Parses_Id_From_Jpg_Url()
        {
            var url = "https://res.cloudinary.com/demo/image/upload/v1712345678/folder/sub/abc123_myphoto.jpg";
            var id = _service.GetPublicId(url);
            Assert.That(id, Is.EqualTo("folder/sub/abc123_myphoto"));
        }

        [Test]
        public void GetPublicId_Parses_Id_From_Png_And_Gif_Urls_CaseInsensitive()
        {
            var png = "https://res.cloudinary.com/demo/image/upload/v1/path/to/file_name.PNG";
            var gif = "https://res.cloudinary.com/demo/image/upload/v98765/only/file.gif";

            Assert.That(_service.GetPublicId(png), Is.EqualTo("path/to/file_name"));
            Assert.That(_service.GetPublicId(gif), Is.EqualTo("only/file"));
        }

        [Test]
        public void GetPublicId_ReturnsEmpty_When_No_Version_Or_No_Extension()
        {
            var noVersion = "https://res.cloudinary.com/demo/image/upload/path/to/file.jpg";
            var noExt = "https://res.cloudinary.com/demo/image/upload/v2/path/to/file";
            Assert.That(_service.GetPublicId(noVersion), Is.EqualTo(string.Empty));
            Assert.That(_service.GetPublicId(noExt), Is.EqualTo(string.Empty));
        }
    }
}
