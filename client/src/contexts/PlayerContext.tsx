import React, { createContext, useContext, useEffect, useRef, useState } from 'react';
import { TrackDto } from '../types/catalog.types';
import { PlaybackState, RepeatMode } from '../types/player.types';

interface PlayerContextType extends PlaybackState {
  playTrack: (track: TrackDto, queue?: TrackDto[]) => void;
  togglePlay: () => void;
  seek: (seconds: number) => void;
  setVolume: (volume: number) => void;
  toggleMute: () => void;
  toggleShuffle: () => void;
  toggleRepeat: () => void;
  nextTrack: () => void;
  prevTrack: () => void;
  toggleLikeTrack: (trackId: string) => void;
  isLiked: (trackId: string) => boolean;
  addToQueue: (track: TrackDto) => void;
}

const PlayerContext = createContext<PlayerContextType | undefined>(undefined);

// Initial demo tracks so the user sees a rich Spotify UI immediately upon launching
const DEMO_TRACKS: TrackDto[] = [
  {
    id: 'track-1',
    title: 'Midnight Echoes',
    artistId: 'artist-1',
    artistName: 'Luna Waves',
    albumId: 'album-1',
    albumTitle: 'Neon Horizon',
    coverImageUrl: 'https://images.unsplash.com/photo-1614613535308-eb5fbd3d2c17?w=400&q=80',
    durationSeconds: 214,
    playCount: 1420500,
    isExplicit: false,
  },
  {
    id: 'track-2',
    title: 'Electric Dreams',
    artistId: 'artist-2',
    artistName: 'Solar Pulse',
    albumId: 'album-2',
    albumTitle: 'Cybernetic Symphony',
    coverImageUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=400&q=80',
    durationSeconds: 198,
    playCount: 980200,
    isExplicit: true,
  },
  {
    id: 'track-3',
    title: 'Velvet Sky',
    artistId: 'artist-3',
    artistName: 'Astral Drift',
    albumId: 'album-3',
    albumTitle: 'Starlight Memories',
    coverImageUrl: 'https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=400&q=80',
    durationSeconds: 245,
    playCount: 2310000,
    isExplicit: false,
  },
];

export const PlayerProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [currentTrack, setCurrentTrack] = useState<TrackDto | null>(DEMO_TRACKS[0]);
  const [isPlaying, setIsPlaying] = useState<boolean>(false);
  const [isBuffering] = useState<boolean>(false);
  const [volume, setVolumeState] = useState<number>(0.75);
  const [prevVolume, setPrevVolume] = useState<number>(0.75);
  const [isMuted, setIsMuted] = useState<boolean>(false);
  const [currentTime, setCurrentTime] = useState<number>(45);
  const [duration, setDuration] = useState<number>(214);
  const [repeatMode, setRepeatMode] = useState<RepeatMode>('off');
  const [isShuffle, setIsShuffle] = useState<boolean>(false);
  const [queue, setQueue] = useState<TrackDto[]>(DEMO_TRACKS);
  const [queueIndex, setQueueIndex] = useState<number>(0);
  const [likedTrackIds, setLikedTrackIds] = useState<string[]>(['track-1']);

  const timerRef = useRef<NodeJS.Timeout | null>(null);

  // Simulated playback timer for smooth progress UI in the player
  useEffect(() => {
    if (isPlaying) {
      timerRef.current = setInterval(() => {
        setCurrentTime((prev) => {
          if (prev >= duration) {
            if (repeatMode === 'one') {
              return 0;
            } else {
              nextTrack();
              return 0;
            }
          }
          return prev + 1;
        });
      }, 1000);
    } else {
      if (timerRef.current) clearInterval(timerRef.current);
    }

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [isPlaying, duration, repeatMode]);

  const playTrack = (track: TrackDto, newQueue?: TrackDto[]) => {
    setCurrentTrack(track);
    setDuration(track.durationSeconds || 180);
    setCurrentTime(0);
    setIsPlaying(true);

    if (newQueue && newQueue.length > 0) {
      setQueue(newQueue);
      const index = newQueue.findIndex((t) => t.id === track.id);
      setQueueIndex(index !== -1 ? index : 0);
    } else {
      const existingIdx = queue.findIndex((t) => t.id === track.id);
      if (existingIdx !== -1) {
        setQueueIndex(existingIdx);
      } else {
        setQueue((prev) => [...prev, track]);
        setQueueIndex(queue.length);
      }
    }
  };

  const togglePlay = () => {
    setIsPlaying((prev) => !prev);
  };

  const seek = (seconds: number) => {
    const clamped = Math.max(0, Math.min(seconds, duration));
    setCurrentTime(clamped);
  };

  const setVolume = (val: number) => {
    const clamped = Math.max(0, Math.min(val, 1));
    setVolumeState(clamped);
    if (clamped > 0 && isMuted) {
      setIsMuted(false);
    }
  };

  const toggleMute = () => {
    if (isMuted) {
      setIsMuted(false);
      setVolumeState(prevVolume > 0 ? prevVolume : 0.5);
    } else {
      setPrevVolume(volume);
      setIsMuted(true);
      setVolumeState(0);
    }
  };

  const toggleShuffle = () => {
    setIsShuffle((prev) => !prev);
  };

  const toggleRepeat = () => {
    setRepeatMode((prev) => {
      if (prev === 'off') return 'all';
      if (prev === 'all') return 'one';
      return 'off';
    });
  };

  const nextTrack = () => {
    if (queue.length === 0) return;

    if (isShuffle) {
      const randomIndex = Math.floor(Math.random() * queue.length);
      setQueueIndex(randomIndex);
      setCurrentTrack(queue[randomIndex]);
      setDuration(queue[randomIndex].durationSeconds || 180);
      setCurrentTime(0);
      return;
    }

    const nextIndex = queueIndex + 1;
    if (nextIndex < queue.length) {
      setQueueIndex(nextIndex);
      setCurrentTrack(queue[nextIndex]);
      setDuration(queue[nextIndex].durationSeconds || 180);
      setCurrentTime(0);
    } else if (repeatMode === 'all') {
      setQueueIndex(0);
      setCurrentTrack(queue[0]);
      setDuration(queue[0].durationSeconds || 180);
      setCurrentTime(0);
    } else {
      setIsPlaying(false);
      setCurrentTime(0);
    }
  };

  const prevTrack = () => {
    if (currentTime > 3) {
      setCurrentTime(0);
      return;
    }

    if (queueIndex > 0) {
      const prevIndex = queueIndex - 1;
      setQueueIndex(prevIndex);
      setCurrentTrack(queue[prevIndex]);
      setDuration(queue[prevIndex].durationSeconds || 180);
      setCurrentTime(0);
    } else {
      setCurrentTime(0);
    }
  };

  const toggleLikeTrack = (trackId: string) => {
    setLikedTrackIds((prev) =>
      prev.includes(trackId) ? prev.filter((id) => id !== trackId) : [...prev, trackId]
    );
  };

  const isLiked = (trackId: string) => likedTrackIds.includes(trackId);

  const addToQueue = (track: TrackDto) => {
    setQueue((prev) => [...prev, track]);
  };

  return (
    <PlayerContext.Provider
      value={{
        currentTrack,
        isPlaying,
        isBuffering,
        volume,
        isMuted,
        currentTime,
        duration,
        repeatMode,
        isShuffle,
        queue,
        queueIndex,
        playTrack,
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
        addToQueue,
      }}
    >
      {children}
    </PlayerContext.Provider>
  );
};

export const usePlayer = (): PlayerContextType => {
  const context = useContext(PlayerContext);
  if (!context) {
    throw new Error('usePlayer must be used within a PlayerProvider');
  }
  return context;
};
