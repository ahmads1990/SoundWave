export interface GenreDto {
  id: string;
  name: string;
  description?: string;
  colorHex?: string;
  imageUrl?: string;
}

export interface ArtistProfileDto {
  id: string;
  userId: string;
  stageName: string;
  bio?: string;
  profilePicUrl?: string;
  coverImageUrl?: string;
  monthlyListeners: number;
  followerCount: number;
  isVerified: boolean;
  topTracks: TrackDto[];
  albums: AlbumDto[];
}

export interface AlbumDto {
  id: string;
  title: string;
  artistId: string;
  artistName: string;
  releaseDate: string;
  coverImageUrl?: string;
  albumType: 'Album' | 'Single' | 'EP';
  trackCount: number;
  totalDurationSeconds?: number;
  tracks?: TrackDto[];
}

export interface TrackDto {
  id: string;
  title: string;
  artistId: string;
  artistName: string;
  albumId?: string;
  albumTitle?: string;
  coverImageUrl?: string;
  durationSeconds: number;
  playCount: number;
  isExplicit: boolean;
  audioUrl?: string;
  hlsPlaylistUrl?: string;
  orderNumber?: number;
}

export interface PlaylistDto {
  id: string;
  title: string;
  description?: string;
  coverImageUrl?: string;
  ownerId: string;
  ownerName: string;
  isPublic: boolean;
  trackCount: number;
  totalDurationSeconds: number;
  tracks?: TrackDto[];
}

/** Mirrors SharedKernel FileStatus — the track's audio lifecycle. */
export type FileStatus = 'PendingUpload' | 'PendingValidation' | 'Processing' | 'Available' | 'Failed';

export interface GetTrackUploadUrlRequest {
  fileName: string;
  fileSizeBytes: number;
  contentType?: string;
}

export interface TrackUploadUrlResponse {
  trackId: string;
  uploadUrl: string;
  storageKey: string;
  expiresAt: string;
}

// Backwards-compatibility aliases
export type InitiateTrackUploadRequest = GetTrackUploadUrlRequest;
export type InitiateTrackUploadResponse = TrackUploadUrlResponse;

export interface TrackStatusResponse {
  trackId: string;
  /** null when no audio has been uploaded yet */
  status: FileStatus | null;
  failureReason?: string;
  processingStartedAt?: string;
  hlsPlaylistPath?: string;
  previewPlaylistPath?: string;
}
