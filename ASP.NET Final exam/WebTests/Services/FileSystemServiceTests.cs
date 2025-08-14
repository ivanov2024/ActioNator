using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using ActioNator.Services.Implementations.FileServices;
using NUnit.Framework;

namespace WebTests.Services
{
    public class FileSystemServiceTests
    {
        private string _tempRoot = null!;
        private FileSystemService _fs = null!;

        [SetUp]
        public void SetUp()
        {
            _fs = new FileSystemService();
            _tempRoot = Path.Combine(Path.GetTempPath(), "fs_tests_" + Guid.NewGuid());
            _fs.CreateDirectory(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, recursive: true); } catch { /* ignore */ }
        }

        [Test]
        public void DirectoryAndFile_Existence_And_Names()
        {
            Assert.That(_fs.DirectoryExists(_tempRoot), Is.True);
            var file = _fs.CombinePaths(_tempRoot, "a.txt");
            Assert.That(_fs.FileExists(file), Is.False);

            using (var stream = _fs.Create(file))
            {
                var data = Encoding.UTF8.GetBytes("hello");
                stream.Write(data, 0, data.Length);
            }

            Assert.That(_fs.FileExists(file), Is.True);
            Assert.That(_fs.GetFileName(file), Is.EqualTo("a.txt"));
            Assert.That(_fs.GetExtension(file), Is.EqualTo(".txt"));
        }

        [Test]
        public async Task OpenRead_OpenWrite_CopyToAsync_Works()
        {
            var src = _fs.CombinePaths(_tempRoot, "src.bin");
            var dst = _fs.CombinePaths(_tempRoot, "dst.bin");

            // Write source
            await using (var ws = _fs.OpenWrite(src))
            {
                var data = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
                await ws.WriteAsync(data, 0, data.Length);
            }

            // Copy to destination
            await using (var rs = _fs.OpenRead(src))
            await using (var wd = _fs.Create(dst))
            {
                await _fs.CopyToAsync(rs, wd, CancellationToken.None);
            }

            // Verify
            var srcBytes = await File.ReadAllBytesAsync(src);
            var dstBytes = await File.ReadAllBytesAsync(dst);
            Assert.That(dstBytes, Is.EqualTo(srcBytes));
        }
    }
}
