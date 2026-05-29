export type VerificationStatus = 'Pending' | 'InAnalysis' | 'Confirmed' | 'FalsePositive' | 'Ignored';
export type DivergenceType = 'ScoreMismatch' | 'GoalScorerMismatch' | 'MissingGoalEvent' | 'YellowCardMismatch' | 'RedCardMismatch' | 'MatchStatusMismatch';
export type Severity = 'Critical' | 'High' | 'Medium' | 'Low';

export interface Divergence {
  id: string;
  matchId: string;
  homeTeam: string;
  awayTeam: string;
  type: DivergenceType;
  severity: Severity;
  sourceA: string;
  sourceAValue: string;
  sourceB: string;
  sourceBValue: string;
  officialSourceValue: string | null;
  detectedAt: string;
  verificationStatus: VerificationStatus;
  replayLink: string | null;
  analystNotes: string | null;
}

export interface GoogleSearchResult {
  title: string;
  snippet: string;
  url: string;
}

export interface GoogleSearchSnapshot {
  matchId: string;
  homeTeam: string;
  awayTeam: string;
  query: string;
  results: GoogleSearchResult[];
  fetchedAt: string;
}

export interface MatchEvent {
  type: string;
  minute: number;
  playerName: string;
  team: string;
}

export interface MatchSnapshot {
  matchId: string;
  homeTeam: string;
  awayTeam: string;
  competition: string;
  kickOff: string;
  homeScore: number;
  awayScore: number;
  status: string;
  events: MatchEvent[];
  source: string;
  collectedAt: string;
}

export type LiveMatchGroup = MatchSnapshot[];

export interface VerificationUpdate {
  status: VerificationStatus;
  replayLink: string | null;
  analystNotes: string | null;
  manualActionStatus: string | null;
}
