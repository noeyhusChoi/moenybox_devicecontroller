using System.Collections.Generic;

namespace KIOSK.Presentation.Features.Exchange.Resources
{
    public sealed class ExchangeDepositAssets
    {
        public ExchangeDepositAssets(string videoPath, IReadOnlyList<ExchangeDepositNoteAsset> currencyNotes)
        {
            VideoPath = videoPath;
            CurrencyNotes = currencyNotes;
        }

        public string VideoPath { get; }
        public IReadOnlyList<ExchangeDepositNoteAsset> CurrencyNotes { get; }
    }
}
