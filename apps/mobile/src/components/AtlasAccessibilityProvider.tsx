import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { AccessibilityInfo, Platform } from 'react-native';

export type AtlasAccessibilityPreferences = {
  reduceMotion: boolean;
  reduceTransparency: boolean;
  ready: boolean;
};

const conservativePreferences: AtlasAccessibilityPreferences = {
  reduceMotion: true,
  reduceTransparency: true,
  ready: false,
};

const AtlasAccessibilityContext = createContext<AtlasAccessibilityPreferences>(conservativePreferences);

export function AtlasAccessibilityProvider({ children }: { children: ReactNode }) {
  const [preferences, setPreferences] = useState<AtlasAccessibilityPreferences>(conservativePreferences);

  useEffect(() => {
    let active = true;

    const reduceMotionSubscription = AccessibilityInfo.addEventListener('reduceMotionChanged', reduceMotion => {
      setPreferences(current => ({ ...current, reduceMotion, ready: true }));
    });

    const reduceTransparencySubscription = Platform.OS === 'ios'
      ? AccessibilityInfo.addEventListener('reduceTransparencyChanged', reduceTransparency => {
        setPreferences(current => ({ ...current, reduceTransparency, ready: true }));
      })
      : null;

    void Promise.all([
      AccessibilityInfo.isReduceMotionEnabled(),
      Platform.OS === 'ios' ? AccessibilityInfo.isReduceTransparencyEnabled() : Promise.resolve(false),
    ]).then(([reduceMotion, reduceTransparency]) => {
      if (!active) return;
      setPreferences({ reduceMotion, reduceTransparency, ready: true });
    }).catch(() => {
      if (!active) return;
      setPreferences(current => ({ ...current, ready: true }));
    });

    return () => {
      active = false;
      reduceMotionSubscription.remove();
      reduceTransparencySubscription?.remove();
    };
  }, []);

  return (
    <AtlasAccessibilityContext.Provider value={preferences}>
      {children}
    </AtlasAccessibilityContext.Provider>
  );
}

export function useAtlasAccessibility(): AtlasAccessibilityPreferences {
  return useContext(AtlasAccessibilityContext);
}
