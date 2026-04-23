using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CommandSynergy.Application.Contracts;
using CommandSynergy.Application.Decks.Portability;

namespace CommandSynergy.Client.Services;

public sealed class ImportedDeckLibrarySerializer : IImportedDeckLibrarySerializer
{
    private const string CompressedPayloadPrefix = "gz:";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };

    public string Serialize(ImportedDeckLibraryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var json = JsonSerializer.Serialize(document.ToContract(), SerializerOptions);
        var compressedPayload = Compress(json);
        return compressedPayload.Length < json.Length
            ? CompressedPayloadPrefix + compressedPayload
            : json;
    }

    public ImportedDeckLibraryDocument Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ImportedDeckLibraryDocument.Empty;
        }

        var normalizedJson = json.StartsWith(CompressedPayloadPrefix, StringComparison.Ordinal)
            ? Decompress(json[CompressedPayloadPrefix.Length..])
            : json;

        var contract = JsonSerializer.Deserialize<ImportedDeckLibraryDocumentContract>(normalizedJson, SerializerOptions)
            ?? throw new JsonException("The imported deck library payload was empty.");

        if (contract.SchemaVersion < 1)
        {
            throw new JsonException("The imported deck library schema version is not supported.");
        }

        return ImportedDeckLibraryDocument.FromContract(contract);
    }

    private static string Compress(string json)
    {
        var inputBytes = Encoding.UTF8.GetBytes(json);
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzipStream.Write(inputBytes, 0, inputBytes.Length);
        }

        return Convert.ToBase64String(outputStream.ToArray());
    }

    private static string Decompress(string compressedPayload)
    {
        var compressedBytes = Convert.FromBase64String(compressedPayload);
        using var inputStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        gzipStream.CopyTo(outputStream);
        return Encoding.UTF8.GetString(outputStream.ToArray());
    }
}
