export type ConnectorState = 'disconnected' | 'connected' | 'syncing' | 'reauthorization-required' | 'error';
export type OperationalSchedule = 'daily' | 'every-6-hours' | 'manual';
export type OperationalFreshness = 'fresh' | 'stale' | 'historical' | 'unknown';

export type OperationalConnector = {
  state: ConnectorState;
  folderName?: string | null;
  schedule?: OperationalSchedule;
  latestBusinessDate?: string | null;
  lastSuccessfulSyncAt?: string | null;
  message?: string | null;
};

export const operationalScheduleChoices = [
  { value: 'daily', label: 'Daily' },
  { value: 'every-6-hours', label: 'Every 6 hours' },
  { value: 'manual', label: 'Manual only' },
] as const satisfies readonly { value: OperationalSchedule; label: string }[];

export function presentConnector(connector: Pick<OperationalConnector, 'state' | 'folderName'>) {
  switch (connector.state) {
    case 'connected': return { title: connector.folderName ?? 'Google Drive connected', primaryAction: 'Sync now', tone: 'positive' as const };
    case 'syncing': return { title: connector.folderName ?? 'Google Drive connected', primaryAction: 'Syncing…', tone: 'neutral' as const };
    case 'reauthorization-required': return { title: 'Folder access needs attention', primaryAction: 'Reconnect folder', tone: 'warning' as const };
    case 'error': return { title: 'The latest sync did not finish', primaryAction: 'Try sync again', tone: 'warning' as const };
    default: return { title: 'Bring fresh business data into Atlas', primaryAction: 'Connect Google Drive', tone: 'neutral' as const };
  }
}

export function classifyOperationalFreshness(latestBusinessDate: string | null | undefined, now = new Date()): OperationalFreshness {
  if (!latestBusinessDate) return 'unknown';
  const sourceDate = new Date(`${latestBusinessDate}T00:00:00.000Z`);
  if (Number.isNaN(sourceDate.getTime())) return 'unknown';
  const ageDays = Math.floor((now.getTime() - sourceDate.getTime()) / 86_400_000);
  if (ageDays <= 7) return 'fresh';
  if (ageDays <= 30) return 'stale';
  return 'historical';
}
