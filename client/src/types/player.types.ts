import { TrackDto } from './catalog.types';

export type RepeatMode = 'off' | 'all' | 'one';

export interface PlaybackState {
  currentTrack: TrackDto | null;
  isPlaying: boolean;
  isBuffering: boolean;
  volume: number; // 0.0 to 1.0
  isMuted: boolean;
  currentTime: number; // in seconds
  duration: number; // in seconds
  repeatMode: RepeatMode;
  isShuffle: boolean;
  queue: TrackDto[];
  queueIndex: number;
}
