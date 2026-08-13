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

export type OperationalUploadAsset = { uri: string; name: string; file?: Blob | null };
export type OperationalUploadPreview = {
  previewFingerprint: string;
  rowCount: number;
  orderCount: number;
  earliestBusinessDate: string;
  latestBusinessDate: string;
  recognizedColumns: string[];
  ignoredSensitiveColumns: string[];
  metricKeys: string[];
};
export type OperationalUploadResult = {
  state: 'imported' | 'duplicate' | 'overlap-conflict';
  createdSignals: number;
  createdChanges: number;
  freshness: 'fresh' | 'stale' | 'historical';
};

async function request<T>(token: string, path: string, init?: RequestInit, allowNotFound = false): Promise<T | null> {
  const response = await fetch(`${env.apiUrl}${path}`, { ...init, headers: { Accept: 'application/json', 'Content-Type': 'application/json', Authorization: `Bearer ${token}`, ...init?.headers } });
  if (allowNotFound && response.status === 404) return null;
  if (!response.ok) throw new Error('Business data is temporarily unavailable.');
  if (response.status === 204) return null;
  return response.json() as Promise<T>;
}

async function multipartRequest<T>(token: string, requestPath: string, form: FormData): Promise<T> {
  const response = await fetch(`${env.apiUrl}${requestPath}`, { method: 'POST', headers: { Accept: 'application/json', Authorization: `Bearer ${token}` }, body: form });
  if (!response.ok) throw new Error('That CSV could not be processed. Check the file and try again.');
  return response.json() as Promise<T>;
}

const path = (businessId: string) => `/api/v1/businesses/${businessId}/operational-connector`;
const uploadPath = (businessId: string, action: 'preview' | 'confirm') => `/api/v1/businesses/${businessId}/operational-upload/${action}`;
const toWireSchedule = (schedule: OperationalSchedule) => schedule === 'every-6-hours' ? 'every-six-hours' : schedule;
const fromWireSchedule = (schedule: OperationalConnectorWire['schedule']): OperationalSchedule => schedule === 'every-six-hours' ? 'every-6-hours' : schedule;
const mapConnector = (value: OperationalConnectorWire): OperationalConnector => ({
  state: value.status,
  folderName: value.folderName,
  schedule: fromWireSchedule(value.schedule),
  lastSuccessfulSyncAt: value.lastSuccessAt,
  message: value.errorCode ?? null,
});

function appendCsv(form: FormData, asset: OperationalUploadAsset) {
  if (asset.file) form.append('file', asset.file, asset.name);
  else form.append('file', { uri: asset.uri, name: asset.name, type: 'text/csv' } as unknown as Blob);
}

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

export async function previewOperationalUpload(token: string, businessId: string, asset: OperationalUploadAsset): Promise<OperationalUploadPreview> {
  const form = new FormData();
  appendCsv(form, asset);
  return multipartRequest<OperationalUploadPreview>(token, uploadPath(businessId, 'preview'), form);
}

export async function confirmOperationalUpload(token: string, businessId: string, asset: OperationalUploadAsset, previewFingerprint: string): Promise<OperationalUploadResult> {
  const form = new FormData();
  appendCsv(form, asset);
  form.append('PreviewFingerprint', previewFingerprint);
  return multipartRequest<OperationalUploadResult>(token, uploadPath(businessId, 'confirm'), form);
}

export const disconnectOperationalFolder = (token: string, businessId: string) => request<void>(token, path(businessId), { method: 'DELETE' });
