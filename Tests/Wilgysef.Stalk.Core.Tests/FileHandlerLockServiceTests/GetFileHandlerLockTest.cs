using Shouldly;
using Wilgysef.Stalk.Core.FileHandlerLockServices;
using Wilgysef.Stalk.TestBase;

namespace Wilgysef.Stalk.Core.Tests.FileHandlerLockServiceTests;

public class GetFileHandlerLockTest : BaseTest
{
    private readonly IFileHandlerLockService _fileHandlerLockService;

    public GetFileHandlerLockTest()
    {
        _fileHandlerLockService = GetRequiredService<IFileHandlerLockService>();
    }

    [Fact]
    public void Get_File_Handler_Lock()
    {
        var lock1 = _fileHandlerLockService.GetFileHandlerLock("abc");
        var lock2 = _fileHandlerLockService.GetFileHandlerLock("def");
        var lock1Copy = _fileHandlerLockService.GetFileHandlerLock("abc");

        lock1Copy.ShouldBe(lock1);
        lock2.ShouldNotBe(lock1);
    }
}
