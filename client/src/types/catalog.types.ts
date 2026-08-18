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
