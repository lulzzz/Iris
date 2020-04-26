using Tweetinvi.Models.Entities;
using Iris.Api;

namespace Iris.Twitter
{
    internal static class MediaFactory
    {
        public static Media ToMedia(IMediaEntity mediaEntity)
        {
            return new Media(
                mediaEntity.MediaURLHttps,
                ToMediaType(mediaEntity.MediaType));
        }

        private static MediaType ToMediaType(string mediaType)
        {
            return mediaType.ToLower() switch 
            {
                "photo" => MediaType.Photo,
                "video" => MediaType.Video,
                "animated_gif" => MediaType.AnimatedGif
                };
        }
    }
}