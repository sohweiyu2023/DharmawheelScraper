using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AtrBot.Infrastructure.Storage;

/// <summary>
/// Handles upload and retrieval of source PDFs and text artefacts in Azure Blob Storage.
/// </summary>
public sealed class DocumentStorageService
{
    private readonly BlobContainerClient _pdfContainer;

    public DocumentStorageService(BlobServiceClient blobServiceClient, string containerName)
    {
        if (blobServiceClient is null)
        {
            throw new ArgumentNullException(nameof(blobServiceClient));
        }

        _pdfContainer = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<Uri> UploadPdfAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var blobClient = _pdfContainer.GetBlobClient(fileName);
        await _pdfContainer.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
        return blobClient.Uri;
    }

    public async Task<Stream?> DownloadPdfAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var blobClient = _pdfContainer.GetBlobClient(fileName);
        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }
}
