
namespace IdScannerTool.Services;

public interface IOcrResultConverter
{
    RunOcrResultDto Normalize(RunOcrResultDto source);
}
