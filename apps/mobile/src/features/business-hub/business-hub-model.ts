import type { BusinessHubContextSummary, BusinessHubMedia, BusinessHubMenuSummary, BusinessMenuItem } from '@/api/atlas-client';

export type HeroPresentation =
  | { kind: 'image'; uri: string; altText: string | null }
  | { kind: 'brand-fallback' };

export type MenuPresentation = {
  title: string;
  priceRange: string | null;
  actionLabel: string | null;
  sourceLabel: string | null;
};

export type ContextPresentation = {
  title: string;
  copy: string;
  actionLabel: string;
};

export type MenuGroup = {
  section: string;
  items: BusinessMenuItem[];
};

export function getHeroPresentation(media: Array<Pick<BusinessHubMedia, 'remoteUrl' | 'altText'>>): HeroPresentation {
  const first = media.find(item => isHttpsUrl(item.remoteUrl));
  return first
    ? { kind: 'image', uri: first.remoteUrl, altText: first.altText ?? null }
    : { kind: 'brand-fallback' };
}

export function getMenuPresentation(menu: BusinessHubMenuSummary): MenuPresentation {
  if (menu.itemCount === 0) {
    return {
      title: 'No menu observed yet',
      priceRange: null,
      actionLabel: null,
      sourceLabel: null,
    };
  }

  const itemLabel = menu.itemCount === 1 ? 'menu item' : 'menu items';
  const sectionLabel = menu.sectionCount === 1 ? 'section' : 'sections';
  const title = menu.sectionCount > 0
    ? `${menu.itemCount} ${itemLabel} across ${menu.sectionCount} ${sectionLabel}`
    : `${menu.itemCount} ${itemLabel} observed`;

  return {
    title,
    priceRange: formatPriceRange(menu.minPrice, menu.maxPrice, menu.currency),
    actionLabel: 'View full menu',
    sourceLabel: menu.source ? `Observed from ${providerLabel(menu.source)}` : null,
  };
}

export function getContextPresentation(context: BusinessHubContextSummary): ContextPresentation {
  if (context.status === 'strong') {
    return {
      title: 'Atlas has a strong operating picture',
      copy: `${context.entryCount} business context details are available${context.ownerConfirmedCount > 0 ? `, including ${context.ownerConfirmedCount} owner-confirmed` : ''}.`,
      actionLabel: 'Review business context',
    };
  }

  if (context.status === 'partial') {
    return {
      title: 'Atlas has a useful operating picture',
      copy: 'A little more context can make future recommendations more specific.',
      actionLabel: 'Review business context',
    };
  }

  return {
    title: 'Atlas is still learning your business',
    copy: 'Review the operating details that matter most so Atlas can give better guidance.',
    actionLabel: 'Review business context',
  };
}

export function groupMenuItems(items: BusinessMenuItem[]): MenuGroup[] {
  const groups = new Map<string, BusinessMenuItem[]>();
  for (const item of items) {
    const section = item.section?.trim() || 'Other';
    const existing = groups.get(section) ?? [];
    existing.push(item);
    groups.set(section, existing);
  }

  return [...groups.entries()]
    .sort(([left], [right]) => {
      if (left === 'Other') return right === 'Other' ? 0 : 1;
      if (right === 'Other') return -1;
      return left.localeCompare(right);
    })
    .map(([section, groupedItems]) => ({
      section,
      items: [...groupedItems].sort((left, right) => left.name.localeCompare(right.name)),
    }));
}

export function formatMenuItemPrice(price: number | null, currency: string | null): string | null {
  if (price === null || !currency) return null;
  return formatCurrency(price, currency);
}

function formatPriceRange(minPrice: number | null, maxPrice: number | null, currency: string | null): string | null {
  if (minPrice === null || maxPrice === null || !currency) return null;
  return `${formatCurrency(minPrice, currency)}–${formatCurrency(maxPrice, currency)}`;
}

function formatCurrency(value: number, currency: string): string {
  try {
    return new Intl.NumberFormat('en', {
      style: 'currency',
      currency: currency.toUpperCase(),
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  } catch {
    return `${currency.toUpperCase()} ${value.toFixed(2)}`;
  }
}

function providerLabel(source: string): string {
  return source
    .split(/[-_\s]+/)
    .filter(Boolean)
    .map(part => part.length <= 3 ? part.toUpperCase() : `${part[0].toUpperCase()}${part.slice(1)}`)
    .join(' ');
}

function isHttpsUrl(value: string): boolean {
  try {
    return new URL(value).protocol === 'https:';
  } catch {
    return false;
  }
}
