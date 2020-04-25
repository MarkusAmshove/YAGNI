using Nuke.CoberturaConverter;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotCover;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.CoberturaConverter.CoberturaConverterTasks;

namespace YAGNI.Build
{
    [CheckBuildProjectConfigurations]
    [UnsetVisualStudioEnvironmentVariables]
    class Build : NukeBuild
    {
        /// Support plugins are available for:
        ///   - JetBrains ReSharper        https://nuke.build/resharper
        ///   - JetBrains Rider            https://nuke.build/rider
        ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
        ///   - Microsoft VSCode           https://nuke.build/vscode
        public static int Main() => Execute<Build>(x => x.Compile);

        [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
        readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

        [Solution] readonly Solution Solution;
        [GitRepository] readonly GitRepository GitRepository;

        AbsolutePath SourceDirectory => RootDirectory / "src";
        AbsolutePath TestsDirectory => RootDirectory / "tests";
        AbsolutePath OutputDirectory => RootDirectory / "output";

        Target Clean => _ => _
            .Before(Restore)
            .Executes(() =>
            {
                SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
                TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
                EnsureCleanDirectory(OutputDirectory);
            });

        Target Restore => _ => _
            .Executes(() =>
            {
                DotNetRestore(s => s
                    .SetProjectFile(Solution));
            });

        Target Compile => _ => _
            .DependsOn(Restore)
            .Executes(() =>
            {
                DotNetBuild(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());
            });

        Target Test => _ => _
            .DependsOn(Compile)
            .Executes(() =>
            {
                DotNetTest(s => s
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .EnableNoRestore()
                    .EnableCollectCoverage()
                    .SetResultsDirectory(OutputDirectory)
                    .SetLogger(NUnitLoggerConfiguration));
            });

        Target GithubCoverage => _ => _
            .DependsOn(Compile)
            .Executes(async () =>
            {
                try
                {
                    RunCoverage("DetailedXml", "xml");
                }
                finally
                {
                    await DotCoverToCobertura(s => s
                        .SetInputFile(OutputDirectory / "Coverage.xml")
                        .SetOutputFile(OutputDirectory / "Cobertura.xml"));
                }
            });

        Target Coverage => _ => _
            .DependsOn(Compile)
            .Executes(() =>
            {
                RunCoverage("HTML", "html");
            });

        Target PackApplication => _ => _
            .Executes(() =>
            {
                var publishPath = OutputDirectory / "YAGNI";
                EnsureCleanDirectory(publishPath);

                DotNetPublish(s => s
                    .SetProject(SourceDirectory / "YAGNI" / "YAGNI.csproj")
                    .SetConfiguration(Configuration.Release)
                    .SetOutput(publishPath)
                    .SetSelfContained(true)
                    .SetRuntime("win-x64"));

                var zipFilePath = OutputDirectory / "YAGNI.zip";
                FileSystemTasks.DeleteFile(zipFilePath);
                CompressionTasks.Compress(publishPath, zipFilePath);
            });

        void RunCoverage(string reportType, string extension) =>
            DotCoverTasks.DotCoverCover(s => s
                .SetConfiguration($"/ReportType={reportType}")
                .SetTargetExecutable(DotNetPath)
                .SetTargetArguments($"test {Solution} --logger=\"{NUnitLoggerConfiguration}\"")
                .AddFilters("+:type=YAGNI.*")
                .SetOutputFile(OutputDirectory / $"Coverage.{extension}"));

        string NUnitLoggerConfiguration => $"nunit;LogFilePath={OutputDirectory}/TestResults.xml";
    }
}
