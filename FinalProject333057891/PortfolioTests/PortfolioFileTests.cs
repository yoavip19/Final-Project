using System.IO.Compression;
using System.Xml;
using Xunit;

namespace PortfolioTests;

/// <summary>
/// Verifies that the project portfolio document ("Project Securio Full.docx") is present
/// and can be opened without errors. A .docx file is a ZIP archive; the main content
/// lives in word/document.xml. These tests confirm the ZIP is intact and that
/// word/document.xml is well-formed XML.
/// </summary>
public class PortfolioFileTests
{
    /// <summary>
    /// Walk up from the test-assembly directory until we reach the repository root
    /// (the directory that directly contains "Project Securio Full.docx").
    /// </summary>
    private static string LocatePortfolioFile()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Project Securio Full.docx");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(
            "Could not locate 'Project Securio Full.docx' by walking up from the test assembly directory.");
    }

    [Fact]
    public void PortfolioFile_Exists()
    {
        var path = LocatePortfolioFile();
        Assert.True(File.Exists(path), $"Portfolio file not found at: {path}");
    }

    [Fact]
    public void PortfolioFile_IsValidZip()
    {
        var path = LocatePortfolioFile();
        using var zip = ZipFile.OpenRead(path);
        Assert.NotEmpty(zip.Entries);
    }

    [Fact]
    public void PortfolioFile_ContainsRequiredOoxmlParts()
    {
        var path = LocatePortfolioFile();
        using var zip = ZipFile.OpenRead(path);

        var entryNames = zip.Entries.Select(e => e.FullName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("[Content_Types].xml", entryNames);
        Assert.Contains("word/document.xml", entryNames);
    }

    [Fact]
    public void PortfolioFile_DocumentXmlIsWellFormed()
    {
        var path = LocatePortfolioFile();
        using var zip = ZipFile.OpenRead(path);

        var entry = zip.GetEntry("word/document.xml")
            ?? zip.GetEntry("Word/Document.xml");

        Assert.NotNull(entry);

        using var stream = entry!.Open();
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using var reader = XmlReader.Create(stream, settings);

        var exception = Record.Exception(() =>
        {
            while (reader.Read()) { }
        });

        Assert.Null(exception);
    }

    [Fact]
    public void PortfolioFile_ZipPassesCrcCheck()
    {
        var path = LocatePortfolioFile();
        using var zip = ZipFile.OpenRead(path);

        foreach (var entry in zip.Entries)
        {
            using var entryStream = entry.Open();
            var buffer = new byte[4096];
            while (entryStream.Read(buffer, 0, buffer.Length) > 0) { }
        }
        Assert.True(true);
    }
}
