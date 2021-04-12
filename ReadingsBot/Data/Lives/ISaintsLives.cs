using System.Collections.Generic;

namespace ReadingsBot.Data
{
    public interface ISaintsLives<SaintsLife> where SaintsLife : ISaintsLife
    {
        List<SaintsLife> Commemorations { get; }
        string DateWithOffset { get; }

        List<EmbedWithImage> GetEmbeds();
    }
}