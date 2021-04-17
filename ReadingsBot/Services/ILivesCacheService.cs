using ReadingsBot.Data;
using System.Collections.Generic;

namespace ReadingsBot
{
    public interface ILivesCacheService
    {
        List<EmbedWithImage> GetLives();
    }
}