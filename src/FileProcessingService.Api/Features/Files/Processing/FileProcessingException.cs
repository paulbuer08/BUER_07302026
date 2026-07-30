namespace FileProcessingService.Api.Features.Files.Processing;


public sealed class FileProcessingException : Exception
{
    private const string DefaultErrorCode = "file_processing_failed";

    public FileProcessingException()
        : this(DefaultErrorCode, "The file could not be processed.")
    {
    }


    public FileProcessingException(string message)
        : this(DefaultErrorCode, message)
    {
    }


    public FileProcessingException(string message, Exception innerException)
        : this(DefaultErrorCode, message, innerException)
    {
    }


    public FileProcessingException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }


    public FileProcessingException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }


    public string ErrorCode { get; }
}
