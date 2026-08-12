export type AtlasMaterialMode = 'glass' | 'solid';
export type AtlasMotionMode = 'full' | 'reduced';

export function resolveMaterialMode({
  platform,
  glassAvailable,
  reduceTransparency,
}: {
  platform: string;
  glassAvailable: boolean;
  reduceTransparency: boolean;
}): AtlasMaterialMode {
  return platform === 'ios' && glassAvailable && !reduceTransparency ? 'glass' : 'solid';
}

export function resolveMotionMode(reduceMotion: boolean): AtlasMotionMode {
  return reduceMotion ? 'reduced' : 'full';
}
