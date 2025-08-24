using System.Diagnostics;
using System.Text.Json;
using Bloodcraft.Utilities;

namespace Bloodcraft.Tests;

public class LocalizationHelpersIntegrationTests
{
    static string RepositoryRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    [Fact]
    public void ProtectRoundTrip_MatchesPythonImplementation()
    {
        string input = "[player] <b>bold</b> {name} ${score} 50%";

        var (safeCs, tokensCs) = LocalizationHelpers.Protect(input);
        string restoredCs = LocalizationHelpers.Unprotect(safeCs, tokensCs);

        PyResult py = RunPython(input);

        Assert.Equal(safeCs, py.Safe);
        Assert.Equal(tokensCs, py.Tokens);
        Assert.Equal(input, restoredCs);
        Assert.Equal(input, py.Restored);
    }

    static PyResult RunPython(string text)
    {
        string script = "import sys, json, translate_argos; text=sys.stdin.read(); safe, tokens = translate_argos.protect_strict(text); restored = translate_argos.unprotect(safe, tokens); ordered = [tokens[str(i)] for i in range(len(tokens))]; print(json.dumps({'safe': safe, 'tokens': ordered, 'restored': restored}))";
        ProcessStartInfo psi = new("python")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = RepositoryRoot
        };
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(script);
        psi.Environment["PYTHONPATH"] = Path.Combine(RepositoryRoot, "Tools");

        using Process process = Process.Start(psi)!;
        process.StandardInput.Write(text);
        process.StandardInput.Close();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Python failed: {error}");

        return JsonSerializer.Deserialize<PyResult>(output)!;
    }

    record PyResult(string Safe, string[] Tokens, string Restored);
}
