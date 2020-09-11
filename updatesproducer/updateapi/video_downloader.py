from youtube_dl import YoutubeDL
from youtube_dl.utils import ExtractorError

from updatesproducer.updateapi.imedia import IMedia
from updatesproducer.updateapi.update import Update
from updatesproducer.updateapi.mediatype import MediaType


class VideoDownloader:
    def __init__(self, logger):
        self.__logger = logger

    def download_video(self, update: Update, lowres_videos):
        # Try downloading the video of this update
        video = self._try_download_video(update.url)

        # If a video is found
        if video is not None:

            # Remove all old lowres that were found
            for lowres_video in lowres_videos:
                update.media.remove(lowres_video)

            # Add new downloaded video
            update.media.append(IMedia(video, MediaType.YouTubeVideo))

    def _try_download_video(self, url):
        ydl_opts = {
            'format': 'best',
            'quiet': True
        }

        try:
            with YoutubeDL(ydl_opts) as ydl:
                return ydl.extract_info(url, download=False)['url']

        except BaseException as ex:
            self.__logger.error("Error extracting video with youtube-dl, skipping video check: %r", ex)

        return None
