using BenchmarkDotNet.Running;
using Motorsports.Benchmarks;

if (args is ["--phase8-audit-reset", var reportPath])
{
    await Phase8Audit.RunAsync(reportPath);
}
else
{
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

internal static partial class Program;
