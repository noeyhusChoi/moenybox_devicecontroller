using System.Windows.Media;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public sealed class ExchangeDepositNoteAsset
    {
        public int Denomination { get; set; }
        public string Label => Denomination.ToString();
        public ImageSource? Image { get; set; }
        public string FilePath { get; set; } = string.Empty;
    }
}
