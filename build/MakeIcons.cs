#:property PublishAot=false
#:property TreatWarningsAsErrors=false
#:property EnforceCodeStyleInBuild=false
#:property AnalysisLevel=none
// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (c) 2026 Guillermo Garcia Carballo
//
// Builds the icon files of the application from the PNG sizes that build/icons.sh renders from assets/icon/arca.svg:
// arca.ico (Windows), arca.icns (macOS) and the summary of the master that the checks compare with. Pure .NET, so the same
// code runs on macOS, Linux and Windows and needs no image tool.
//
//   dotnet run build/MakeIcons.cs              build the .ico, the .icns and arca.svg.sha256 from assets/icon/arca-<size>.png
//   dotnet run build/MakeIcons.cs -- --check   fail if the summary of the master does not match the derivatives

using System.Buffers.Binary;
using System.Security.Cryptography;

var root = FindRoot();
var folder = Path.Combine(root, "assets", "icon");
var master = Path.Combine(folder, "arca.svg");
var summaryFile = Path.Combine(folder, "arca.svg.sha256");
var summary = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(master))).ToLowerInvariant();

if (args.Contains("--check"))
{
    var stored = File.Exists(summaryFile) ? File.ReadAllText(summaryFile).Trim() : string.Empty;
    if (stored != summary)
    {
        Console.Error.WriteLine("assets/icon/arca.svg changed and the derivatives were not regenerated: run build/icons.sh.");
        return 1;
    }

    Console.WriteLine("OK: the icon files correspond to the master.");
    return 0;
}

byte[] Png(int size) => File.ReadAllBytes(Path.Combine(folder, $"arca-{size}.png"));

// Windows: a header, one entry per size and the PNG images themselves (Windows accepts PNG inside an .ico).
int[] icoSizes = [16, 32, 48, 64, 128, 256];
using (var ico = new MemoryStream())
{
    var images = icoSizes.Select(Png).ToArray();
    Span<byte> header = stackalloc byte[6];
    BinaryPrimitives.WriteUInt16LittleEndian(header[2..], 1); // type: icon
    BinaryPrimitives.WriteUInt16LittleEndian(header[4..], (ushort)icoSizes.Length);
    ico.Write(header);
    var offset = 6 + 16 * icoSizes.Length;
    for (var i = 0; i < icoSizes.Length; i++)
    {
        Span<byte> entry = stackalloc byte[16];
        entry[0] = (byte)(icoSizes[i] == 256 ? 0 : icoSizes[i]); // 0 means 256
        entry[1] = entry[0];
        BinaryPrimitives.WriteUInt16LittleEndian(entry[4..], 1); // colour planes
        BinaryPrimitives.WriteUInt16LittleEndian(entry[6..], 32); // bits per pixel
        BinaryPrimitives.WriteUInt32LittleEndian(entry[8..], (uint)images[i].Length);
        BinaryPrimitives.WriteUInt32LittleEndian(entry[12..], (uint)offset);
        ico.Write(entry);
        offset += images[i].Length;
    }

    foreach (var image in images)
    {
        ico.Write(image);
    }

    File.WriteAllBytes(Path.Combine(folder, "arca.ico"), ico.ToArray());
}

// macOS: 'icns', the total length and one block per size with its type, its length and the PNG (big endian).
(string Type, int Size)[] icnsSizes = [("icp4", 16), ("icp5", 32), ("icp6", 64), ("ic07", 128), ("ic08", 256), ("ic09", 512), ("ic10", 1024)];
using (var icns = new MemoryStream())
{
    var blocks = icnsSizes.Select(s => (s.Type, Data: Png(s.Size))).ToArray();
    var total = 8 + blocks.Sum(b => 8 + b.Data.Length);
    icns.Write("icns"u8);
    Span<byte> length = stackalloc byte[4];
    BinaryPrimitives.WriteUInt32BigEndian(length, (uint)total);
    icns.Write(length);
    foreach (var (type, data) in blocks)
    {
        icns.Write(System.Text.Encoding.ASCII.GetBytes(type));
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)(8 + data.Length));
        icns.Write(length);
        icns.Write(data);
    }

    File.WriteAllBytes(Path.Combine(folder, "arca.icns"), icns.ToArray());
}

File.WriteAllText(summaryFile, summary + "\n");
Console.WriteLine($"OK: arca.ico, arca.icns and arca.svg.sha256 written in {folder}.");
return 0;

static string FindRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    for (var at = new DirectoryInfo(Directory.GetCurrentDirectory()); at is not null; at = at.Parent)
    {
        if (File.Exists(Path.Combine(at.FullName, "Arca.slnx")))
        {
            return at.FullName;
        }
    }

    throw new InvalidOperationException($"Repository root not found from {dir.FullName}");
}
