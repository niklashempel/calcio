export interface VenueDto {
  id: number;
  address?: string;
  latitude?: number;
  longitude?: number;
}

export interface TeamDto {
  id: number;
  name?: string;
  clubId?: number;
}

export interface AgeGroupDto {
  id: number;
  name?: string;
}

export interface CompetitionDto {
  id: number;
  name?: string;
}

export interface MatchDto {
  id: number;
  url?: string;
  time?: string;
  homeTeam?: TeamDto;
  awayTeam?: TeamDto;
  venue?: VenueDto;
  ageGroup?: AgeGroupDto;
  competition?: CompetitionDto;
}

export interface GetMatchesRequest {
  minLat?: number;
  maxLat?: number;
  minLng?: number;
  maxLng?: number;
}

export interface GroupedMatches {
  venueId: number;
  venue?: VenueDto;
  count: number;
  today: MatchDto[];
  upcoming: MatchDto[];
  past: MatchDto[];
}
