import { env } from '@/lib/env';
import type { OperationalConnector, OperationalSchedule } from './operational-data-model';

type OperationalConnectorWire = {
  id: string;
  folderName: string;
  status: OperationalConnector['state'];
  schedule: 'daily' | 'every-six-hours' | 'manual';
  lastAttemptAt?: string | null;
  lastSuccessAt?: string | null;
  errorCode?: string | null;
};

export type OperationalSyncResult = {
  state: 'completed' | 'busy' | 'not-connected' | 'reauthorization-required';
  processedFiles: number;
  unchangedFiles: number;
};

async function request<T>(token: string, path: string, init?: RequestInit, allowNotFound = false): Promise<T | null> {
  const response = await fetch(`${env.apiUrl}${path}`, { ...init, headers: { Accept: 'application/json', 'Content-Type': 'application/json', Authorization: `Bearer ${token}`, ...init?.headers } });
  if (allowNotFound && response.status === 404) return null;
  if (!response.ok) throw new Error('Business data is temporarily unavailable.');
  if (response.status === 204) return null;
  return response.json() as Promise<T>;
}

const path = (businessId: string) => `/api/v1/businesses/${businessId}/operational-connector`;
const toWireSchedule = (schedule: OperationalSchedule) => schedule === 'every-6-hours' ? 'every-six-hours' : schedule;
const fromWireSchedule = (schedule: OperationalConnectorWire['schedule']): OperationalSchedule => schedule === 'every-six-hours' ? 'every-6-hours' : schedule;
const mapConnector = (value: OperationalConnectorWire): OperationalConnector => ({
  state: value.status,
  folderName: value.folderName,
  schedule: fromWireSchedule(value.schedule),
  lastSuccessfulSyncAt: value.lastSuccessAt,
  message: value.errorCode ?? null,
});

export async function getOperationalConnector(token: string, businessId: string): Promise<OperationalConnector> {
  const value = await request<OperationalConnectorWire>(token, path(businessId), undefined, true);
  return value ? mapConnector(value) : { state: 'disconnected', schedule: 'daily' };
}

export async function connectOperationalFolder(token: string, businessId: string, folderId: string, schedule: OperationalSchedule): Promise<OperationalConnector> {
  const value = await request<OperationalConnectorWire>(token, path(businessId), { method: 'POST', body: JSON.stringify({ folderId, schedule: toWireSchedule(schedule) }) });
  if (!value) throw new Error('Business data is temporarily unavailable.');
  return mapConnector(value);
}

export async function syncOperationalFolder(token: string, businessId: string): Promise<OperationalSyncResult> {
  const value = await request<OperationalSyncResult>(token, `${path(businessId)}/sync`, { method: 'POST' });
  if (!value) throw new Error('Business data is temporarily unavailable.');
  return value;
}

export async function setOperationalSchedule(token: string, businessId: string, schedule: OperationalSchedule): Promise<OperationalConnector> {
  const value = await request<OperationalConnectorWire>(token, `${path(businessId)}/schedule`, { method: 'PUT', body: JSON.stringify({ schedule: toWireSchedule(schedule) }) });
  if (!value) throw new Error('Business data is temporarily unavailable.');
  return mapConnector(value);
}

export const disconnectOperationalFolder = (token: string, businessId: string) => request<void>(token, path(businessId), { method: 'DELETE' });
