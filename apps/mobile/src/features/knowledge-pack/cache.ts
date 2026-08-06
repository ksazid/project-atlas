import { secureStorage } from '@/lib/secure-storage';
import type { KnowledgePack } from '@/api/atlas-client';

const key = (businessId: string) => `atlas.knowledge-pack.${businessId}`;

export type CachedKnowledgePack = { pack: KnowledgePack; cachedAt: string };

export async function loadCachedKnowledgePack(businessId: string): Promise<CachedKnowledgePack | null> {
  const value = await secureStorage.get(key(businessId));
  if (!value) return null;
  try { return JSON.parse(value) as CachedKnowledgePack; } catch { return null; }
}

export async function saveCachedKnowledgePack(businessId: string, pack: KnowledgePack): Promise<void> {
  await secureStorage.set(key(businessId), JSON.stringify({ pack, cachedAt: new Date().toISOString() } satisfies CachedKnowledgePack));
}
