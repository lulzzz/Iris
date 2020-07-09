from twitter_scraper import get_tweets
from youtube_dl import YoutubeDL
from youtube_dl.utils import ExtractorError

from twitterproducer.tweets.itweets_provider import ITweetsProvider
from twitterproducer.tweets.tweet import Tweet


class TweetsProvider(ITweetsProvider):
    def __init__(self, logger):
        self.__logger = logger

    def get_tweets(self, user_id):
        for tweet_dict in get_tweets(user_id, pages=1):
            tweet = Tweet(tweet_dict)
            # Downloads the first video only
            if len(tweet_dict['entries']['videos']) > 0:
                tweet.video = self._download_video(tweet.tweetUrl)

            yield tweet

    def _download_video(self, url):
        ydl_opts = {
            'format': 'best',
            'quiet': True,
        }
        try:
            with YoutubeDL(ydl_opts) as ydl:
                return ydl.extract_info(url, download=False)['url']
        except ExtractorError as ex:
            self.__logger.error("Error extracting video with youtube-dl: %r", ex)
        return None

