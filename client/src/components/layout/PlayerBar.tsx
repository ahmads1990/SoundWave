import {
  Heart,
  Laptop2,
  ListMusic,
  Maximize2,
  Mic2,
  Pause,
  Play,
  Repeat,
  Repeat1,
  Shuffle,
  SkipBack,
  SkipForward,
  Volume2,
  VolumeX,
} from 'lucide-react';
import React from 'react';
import { Link } from 'react-router-dom';
import { usePlayer } from '../../contexts/PlayerContext';
import { cn } from '../../utils/cn';

function formatTime(seconds: number): string {
  if (isNaN(seconds) || seconds < 0) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs < 10 ? '0' : ''}${secs}`;
}

export const PlayerBar: React.FC = () => {
  const {
    currentTrack,
    isPlaying,
    currentTime,
    duration,
    volume,
    isMuted,
    repeatMode,
    isShuffle,
    togglePlay,
    seek,
    setVolume,
    toggleMute,
    toggleShuffle,
    toggleRepeat,
    nextTrack,
    prevTrack,
    toggleLikeTrack,
    isLiked,
  } = usePlayer();

  if (!currentTrack) return null;

  const liked = isLiked(currentTrack.id);
  const progressPercent = duration > 0 ? (currentTime / duration) * 100 : 0;
  const volumePercent = isMuted ? 0 : volume * 100;

  return (
    <footer className="h-20 bg-spotify-black border-t border-white/5 px-4 flex items-center justify-between text-spotify-white select-none z-50">
      {/* Left: Track Details */}
      <div className="flex items-center gap-3.5 w-1/4 min-w-[180px]">
        <div className="relative group/cover w-14 h-14 rounded overflow-hidden flex-shrink-0 bg-spotify-card shadow-md">
          {currentTrack.coverImageUrl ? (
            <img
              src={currentTrack.coverImageUrl}
              alt={currentTrack.title}
              className="w-full h-full object-cover"
            />
          ) : (
            <div className="w-full h-full flex items-center justify-center bg-spotify-card text-spotify-muted">
              🎵
            </div>
          )}
        </div>

        <div className="flex flex-col min-w-0">
          <Link
            to={`/track/${currentTrack.id}`}
            className="text-sm font-semibold text-white hover:underline truncate"
          >
            {currentTrack.title}
          </Link>
          <Link
            to={`/artist/${currentTrack.artistId}`}
            className="text-xs text-spotify-muted hover:underline hover:text-white truncate"
          >
            {currentTrack.artistName}
          </Link>
        </div>

        <button
          onClick={() => toggleLikeTrack(currentTrack.id)}
          title={liked ? 'Remove from Liked Songs' : 'Save to Liked Songs'}
          className="p-1 text-spotify-muted hover:text-white transition-colors cursor-pointer ml-1"
        >
          <Heart
            className={cn(
              'w-5 h-5 transition-transform duration-150 active:scale-125',
              liked ? 'text-spotify-green fill-spotify-green' : 'hover:text-white'
            )}
          />
        </button>
      </div>

      {/* Center: Playback Controls & Scrubber */}
      <div className="flex flex-col items-center gap-1.5 w-2/4 max-w-xl">
        {/* Buttons */}
        <div className="flex items-center gap-4">
          <button
            onClick={toggleShuffle}
            title="Enable shuffle"
            className={cn(
              'p-1 transition-colors cursor-pointer',
              isShuffle ? 'text-spotify-green' : 'text-spotify-muted hover:text-white'
            )}
          >
            <Shuffle className="w-4 h-4" />
          </button>

          <button
            onClick={prevTrack}
            title="Previous"
            className="p-1 text-spotify-muted hover:text-white transition-colors cursor-pointer"
          >
            <SkipBack className="w-5 h-5 fill-current" />
          </button>

          <button
            onClick={togglePlay}
            title={isPlaying ? 'Pause' : 'Play'}
            className="flex items-center justify-center w-8 h-8 rounded-full bg-white hover:scale-105 text-black transition-all active:scale-95 shadow-md cursor-pointer"
          >
            {isPlaying ? (
              <Pause className="w-4 h-4 fill-black text-black" />
            ) : (
              <Play className="w-4 h-4 fill-black text-black ml-0.5" />
            )}
          </button>

          <button
            onClick={nextTrack}
            title="Next"
            className="p-1 text-spotify-muted hover:text-white transition-colors cursor-pointer"
          >
            <SkipForward className="w-5 h-5 fill-current" />
          </button>

          <button
            onClick={toggleRepeat}
            title={`Repeat: ${repeatMode}`}
            className={cn(
              'p-1 transition-colors cursor-pointer',
              repeatMode !== 'off' ? 'text-spotify-green' : 'text-spotify-muted hover:text-white'
            )}
          >
            {repeatMode === 'one' ? (
              <Repeat1 className="w-4 h-4" />
            ) : (
              <Repeat className="w-4 h-4" />
            )}
          </button>
        </div>

        {/* Progress Bar & Timestamps */}
        <div className="flex items-center gap-2 w-full text-xs text-spotify-muted font-medium">
          <span className="w-10 text-right tabular-nums">{formatTime(currentTime)}</span>

          <div className="relative flex-1 flex items-center group py-2 cursor-pointer">
            {/* Background track */}
            <div className="w-full h-1 rounded-full bg-white/20 overflow-hidden">
              <div
                className="h-full bg-white group-hover:bg-spotify-green transition-colors rounded-full"
                style={{ width: `${progressPercent}%` }}
              />
            </div>

            {/* Slider input */}
            <input
              type="range"
              min={0}
              max={duration || 100}
              value={currentTime}
              onChange={(e) => seek(Number(e.target.value))}
              className="spotify-slider absolute inset-0 opacity-0 cursor-pointer"
            />
          </div>

          <span className="w-10 text-left tabular-nums">{formatTime(duration)}</span>
        </div>
      </div>

      {/* Right: Auxiliary Controls & Volume */}
      <div className="flex items-center justify-end gap-3 w-1/4 min-w-[180px]">
        <button
          title="Lyrics"
          className="p-1 text-spotify-muted hover:text-white transition-colors"
        >
          <Mic2 className="w-4 h-4" />
        </button>

        <button
          title="Queue"
          className="p-1 text-spotify-muted hover:text-white transition-colors"
        >
          <ListMusic className="w-4 h-4" />
        </button>

        <button
          title="Connect to a device"
          className="p-1 text-spotify-muted hover:text-white transition-colors"
        >
          <Laptop2 className="w-4 h-4" />
        </button>

        {/* Volume Slider */}
        <div className="flex items-center gap-2 group w-28">
          <button
            onClick={toggleMute}
            title={isMuted ? 'Unmute' : 'Mute'}
            className="text-spotify-muted hover:text-white transition-colors p-1"
          >
            {isMuted || volume === 0 ? (
              <VolumeX className="w-4 h-4" />
            ) : (
              <Volume2 className="w-4 h-4" />
            )}
          </button>

          <div className="relative flex-1 flex items-center py-2 cursor-pointer">
            <div className="w-full h-1 rounded-full bg-white/20 overflow-hidden">
              <div
                className="h-full bg-white group-hover:bg-spotify-green transition-colors rounded-full"
                style={{ width: `${volumePercent}%` }}
              />
            </div>
            <input
              type="range"
              min={0}
              max={1}
              step={0.01}
              value={isMuted ? 0 : volume}
              onChange={(e) => setVolume(Number(e.target.value))}
              className="spotify-slider absolute inset-0 opacity-0 cursor-pointer"
            />
          </div>
        </div>

        <button
          title="Full screen"
          className="p-1 text-spotify-muted hover:text-white transition-colors"
        >
          <Maximize2 className="w-4 h-4" />
        </button>
      </div>
    </footer>
  );
};
