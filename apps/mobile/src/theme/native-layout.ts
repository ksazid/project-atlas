export type AtlasTabBarMetricsInput = {
  width: number;
  bottomInset: number;
  fontScale: number;
};

export type AtlasTabBarMetrics = {
  mode: 'floating' | 'edge';
  horizontalInset: number;
  bottomOffset: number;
  frameHeight: number;
  paddingBottom: number;
  borderRadius: number;
  obstructionHeight: number;
};

export type AtlasScreenMetricsInput = {
  width: number;
  topInset: number;
  bottomInset: number;
  fontScale: number;
  hasTabBar: boolean;
};

export type AtlasScreenMetrics = {
  paddingTop: number;
  paddingBottom: number;
  paddingHorizontal: number;
};

const COMPACT_WIDTH = 390;
const LARGE_TEXT_SCALE = 1.2;

export function getAtlasTabBarMetrics({ width, bottomInset, fontScale }: AtlasTabBarMetricsInput): AtlasTabBarMetrics {
  const rowHeight = fontScale > LARGE_TEXT_SCALE ? 64 : 58;
  if (width < COMPACT_WIDTH) {
    const frameHeight = rowHeight + bottomInset;
    return {
      mode: 'edge',
      horizontalInset: 0,
      bottomOffset: 0,
      frameHeight,
      paddingBottom: bottomInset,
      borderRadius: 0,
      obstructionHeight: frameHeight,
    };
  }

  const horizontalInset = width >= 430 ? 16 : 12;
  const bottomOffset = Math.max(8, bottomInset - 8);
  return {
    mode: 'floating',
    horizontalInset,
    bottomOffset,
    frameHeight: rowHeight,
    paddingBottom: 0,
    borderRadius: 24,
    obstructionHeight: rowHeight + bottomOffset,
  };
}

export function getAtlasScreenMetrics({
  width,
  topInset,
  bottomInset,
  fontScale,
  hasTabBar,
}: AtlasScreenMetricsInput): AtlasScreenMetrics {
  const paddingHorizontal = width < 360 ? 20 : width < 430 ? 24 : 28;
  const topGap = width < 360 ? 8 : 12;
  const tab = getAtlasTabBarMetrics({ width, bottomInset, fontScale });

  return {
    paddingTop: topInset + topGap,
    paddingBottom: hasTabBar ? tab.obstructionHeight + 16 : bottomInset + 24,
    paddingHorizontal,
  };
}
