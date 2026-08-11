export type BusinessUrlInputResult = {
  value: string;
  complete: boolean;
  error: string | null;
};

type SourceKind = 'website' | 'bolt-food' | 'wolt' | 'google-maps';

const MAX_URL_CHARACTERS = 2000;
const TRACKING_KEYS = new Set([
  'gclid',
  'dclid',
  'fbclid',
  'msclkid',
  'g_st',
  'mc_cid',
  'mc_eid',
  'ref',
  'referrer',
  'referral',
  'share',
  'share_id',
  'share_source',
  'source',
]);
const GOOGLE_RETAINED_KEYS = new Set(['query_place_id', 'query', 'q', 'cid', 'ftid']);
const GOOGLE_STRONG_IDENTITY_KEYS = new Set(['query_place_id', 'cid', 'ftid']);
const BLOCKED_HOST_SUFFIXES = ['.localhost', '.local', '.internal', '.home', '.lan', '.test'];
const HTTPS_URL = /https:\/\/[^\s<>"']+/gi;

export function canonicalizeBusinessUrlInput(rawValue: string): BusinessUrlInputResult {
  const original = rawValue;
  const trimmed = rawValue.replace(/[\u0000-\u001F\u007F]/g, '').trim();

  if (!trimmed) return { value: original, complete: false, error: null };

  if (trimmed.length > 8192) {
    return invalid(original, 'Business page URL is too long.');
  }

  if (/^http:\/\//i.test(trimmed)) {
    return invalid(original, 'Use a secure HTTPS business page URL.');
  }

  const matches = [...trimmed.matchAll(HTTPS_URL)].map(match => trimSharePunctuation(match[0]));
  if (matches.length > 1) {
    return invalid(original, 'Share one business page URL in each field.');
  }

  if (matches.length === 0) {
    if (looksLikeIncompleteUrl(trimmed)) return { value: original, complete: false, error: null };
    return invalid(original, 'Use a valid HTTPS business page URL.');
  }

  const candidate = matches[0];
  if (candidate.length > MAX_URL_CHARACTERS) {
    return invalid(original, 'Business page URL is too long.');
  }

  let url: URL;
  try {
    url = new URL(candidate);
  } catch {
    return invalid(original, 'Use a valid HTTPS business page URL.');
  }

  if (url.protocol !== 'https:') return invalid(original, 'Use a secure HTTPS business page URL.');
  if (url.username || url.password) return invalid(original, 'Business page URLs cannot include credentials.');
  if (url.port && url.port !== '443') return invalid(original, 'Use the standard HTTPS port.');

  const host = url.hostname.replace(/\.$/, '').toLowerCase();
  if (!host || isBlockedHost(host) || isIpLiteral(host)) {
    return invalid(original, 'Use a public business hostname.');
  }

  const kind = sourceKind(host);
  const route = validateProviderRoute(kind, host, url);
  if (route === 'incomplete') return { value: original, complete: false, error: null };
  if (route) return invalid(original, route);

  url.protocol = 'https:';
  url.hostname = host;
  url.port = '';
  url.hash = '';
  if (url.pathname.length > 1) url.pathname = url.pathname.replace(/\/+$/, '');
  canonicalizeQuery(url, kind, host);

  const value = url.pathname === '/' && !url.search
    ? `https://${url.host}`
    : url.toString();
  if (value.length > MAX_URL_CHARACTERS) return invalid(original, 'Business page URL is too long.');

  return { value, complete: true, error: null };
}

export function canonicalBusinessUrlKey(rawValue: string): string | null {
  const result = canonicalizeBusinessUrlInput(rawValue);
  return result.complete && !result.error ? result.value : null;
}

function invalid(value: string, error: string): BusinessUrlInputResult {
  return { value, complete: true, error };
}

function looksLikeIncompleteUrl(value: string) {
  return /^(h|ht|htt|http|https|https:|https:\/|https:\/\/[^\s]*)$/i.test(value);
}

function trimSharePunctuation(value: string) {
  return value.replace(/[.,;:!\)\]\}]+$/, '');
}

function sourceKind(host: string): SourceKind {
  if (host === 'food.bolt.eu') return 'bolt-food';
  if (host === 'wolt.com' || host.endsWith('.wolt.com')) return 'wolt';
  if (['maps.app.goo.gl', 'maps.google.com', 'google.com', 'www.google.com'].includes(host)) return 'google-maps';
  return 'website';
}

function validateProviderRoute(kind: SourceKind, host: string, url: URL): string | 'incomplete' | null {
  const segments = url.pathname.split('/').filter(Boolean);

  if (kind === 'bolt-food') {
    if (segments.length === 0) return 'incomplete';
    const pageIndex = segments.findIndex(segment => segment.toLowerCase() === 'p');
    if (pageIndex < 0 || pageIndex >= segments.length - 1 || segments[pageIndex + 1].length < 2) {
      return 'Share the specific Bolt Food business page, not a marketplace browsing page.';
    }
    return null;
  }

  if (kind === 'wolt') {
    if (segments.length === 0) return 'incomplete';
    const businessIndex = segments.findIndex(segment => ['restaurant', 'venue', 'store'].includes(segment.toLowerCase()));
    if (businessIndex < 0 || businessIndex >= segments.length - 1 || segments[businessIndex + 1].length < 2) {
      return 'Share the specific Wolt business page, not a marketplace browsing page.';
    }
    return null;
  }

  if (kind === 'google-maps') {
    if (host === 'maps.app.goo.gl') {
      if (segments.length === 0) return 'incomplete';
      return segments.length === 1 && segments[0].length >= 4
        ? null
        : 'Share a specific Google Maps business location link.';
    }

    const path = url.pathname.toLowerCase();
    if (path.includes('/search') && !hasStrongGooglePlaceIdentifier(url)) {
      return 'Google Search links are not a specific business location. Share the Google Maps business profile/location instead.';
    }
    if (path.includes('/maps/place/') || hasStrongGooglePlaceIdentifier(url)) return null;
    return 'Share a Google Maps link that identifies one business location.';
  }

  return null;
}

function canonicalizeQuery(url: URL, kind: SourceKind, host: string) {
  if (kind === 'bolt-food' || kind === 'wolt' || (kind === 'google-maps' && host === 'maps.app.goo.gl')) {
    url.search = '';
    return;
  }

  const retained = new URLSearchParams();
  for (const [key, value] of url.searchParams.entries()) {
    const normalizedKey = key.toLowerCase();
    if (normalizedKey.startsWith('utm_') || TRACKING_KEYS.has(normalizedKey)) continue;
    if (kind === 'google-maps' && !GOOGLE_RETAINED_KEYS.has(normalizedKey)) continue;
    retained.append(key, value);
  }
  url.search = retained.toString();
}

function hasStrongGooglePlaceIdentifier(url: URL) {
  for (const [key, value] of url.searchParams.entries()) {
    if (GOOGLE_STRONG_IDENTITY_KEYS.has(key.toLowerCase()) && value.trim()) return true;
  }
  return false;
}

function isBlockedHost(host: string) {
  return host === 'localhost' || host === 'localhost.localdomain' || BLOCKED_HOST_SUFFIXES.some(suffix => host.endsWith(suffix));
}

function isIpLiteral(host: string) {
  if (/^\d{1,3}(?:\.\d{1,3}){3}$/.test(host)) return true;
  return host.includes(':');
}
