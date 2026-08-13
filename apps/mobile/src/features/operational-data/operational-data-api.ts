import { env } from '@/lib/env';
import type { OperationalConnector, OperationalSchedule } from './operational-data-model';

async function request<T>(token: string, path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${env.apiUrl}${path}`, { ...init, headers: { Accept: 'application/json', 'Content-Type': 'application/json', Authorization: `Bearer ${token}`, ...init?.headers } });
  if (!response.ok) throw new Error('Business data is temporarily unavailable.');
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

const path = (businessId: string) => `/api/v1/businesses/${businessId}/operational-connector`;
export const getOperationalConnector = (token: string, businessId: string) => request<OperationalConnector>(token, path(businessId));
export const connectOperationalFolder = (token: string, businessId: string, folderUrl: string) => request<OperationalConnector>(token, path(businessId), { method: 'PUT', body: JSON.stringify({ folderUrl }) });
export const syncOperationalFolder = (token: string, businessId: string) => request<OperationalConnector>(token, `${path(businessId)}/sync`, { method: 'POST' });
export const setOperationalSchedule = (token: string, businessId: string, schedule: OperationalSchedule) => request<OperationalConnector>(token, `${path(businessId)}/schedule`, { method: 'PUT', body: JSON.stringify({ schedule }) });
export const disconnectOperationalFolder = (token: string, businessId: string) => request<void>(token, path(businessId), { method: 'DELETE' });
