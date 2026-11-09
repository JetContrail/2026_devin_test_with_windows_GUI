using System.IO;

namespace DrawingApp.Tests.Core;

[Collection(EnduranceTestGroup.Name)]
public sealed class EnduranceScenarioTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("endurance-").FullName;

    public void Dispose() => Directory.Delete(_directory, recursive: true);

    [Fact]
    public void Repeat100Times_WithFakePorts_Completes()
    {
        var background = Path.Combine(_directory, "background.png");
        File.WriteAllBytes(background, [0]);

        EnduranceScenario.Run(new FakeImageEncoder(), new FakeImageDecoder(256, 192), background, _directory);
    }
}
