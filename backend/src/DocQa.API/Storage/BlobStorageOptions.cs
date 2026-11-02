using System.ComponentModel.DataAnnotations;

namespace DocQa.API.Storage;

public sealed class BlobStorageOptions
{
    public const string Section = "BlobStorage";

    [Required] public string ConnectionString { get; init; } = "";
    [Required] public string ContainerName { get; init; } = "documents";
}