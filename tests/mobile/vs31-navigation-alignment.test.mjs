import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import test from 'node:test';

function file(path) {
  const url = new URL(path, import.meta.url);
  return existsSync(url) ? readFileSync(url, 'utf8') : '';
}

const tabHistory = '../../apps/mobile/app/(tabs)/history.tsx';
const rootHistory = '../../apps/mobile/app/history.tsx';
const rootContext = '../../apps/mobile/app/context.tsx';
const tabContext = '../../apps/mobile/app/(tabs)/context.tsx';
const rootSettings = '../../apps/mobile/app/settings.tsx';
const tabSettings = '../../apps/mobile/app/(tabs)/settings.tsx';

test('VS-31 route placement preserves public paths while changing navigation roles', () => {
  assert.equal(existsSync(new URL(tabHistory, import.meta.url)), true);
  assert.equal(existsSync(new URL(rootHistory, import.meta.url)), false);
  assert.equal(existsSync(new URL(rootContext, import.meta.url)), true);
  assert.equal(existsSync(new URL(tabContext, import.meta.url)), false);
  assert.equal(existsSync(new URL(rootSettings, import.meta.url)), true);
  assert.equal(existsSync(new URL(tabSettings, import.meta.url)), false);
  assert.match(file(tabHistory), /HistoryScreen/);
});

test('History is a tab root without a misleading Back action', () => {
  const history = file('../../apps/mobile/src/features/history/HistoryScreen.tsx');
  assert.match(history, /<AtlasScreen\s+hasTabBar\b/);
  assert.doesNotMatch(history, /onPress=\{\(\) => router\.back\(\)\}/);
  assert.match(history, /Weekly review/);
});

test('Profile retains Business Hub and links to Context and Settings details', () => {
  const profile = file('../../apps/mobile/src/features/business-hub/BusinessHubScreen.tsx');
  assert.match(profile, />PROFILE</);
  assert.match(profile, /router\.push\('\/context'\)/);
  assert.match(profile, /router\.push\('\/settings'\)/);
  assert.match(profile, /Edit business details/);
  assert.match(profile, /MenuIntelligenceCard/);
});

test('Context and Settings use pushed Stack route shells with accessible Profile fallback', () => {
  const contextRoute = file(rootContext);
  const settingsRoute = file(rootSettings);

  for (const source of [contextRoute, settingsRoute]) {
    assert.match(source, /<Stack\.Screen/);
    assert.match(source, /headerShown:\s*true/);
    assert.match(source, /accessibilityLabel="Back to Profile"/);
    assert.match(source, /router\.canGoBack\(\)/);
    assert.match(source, /router\.replace\('\/(?:\(tabs\)\/)?profile'\)/);
    assert.doesNotMatch(source, /<AtlasScreen\s+hasTabBar\b/);
  }

  assert.match(contextRoute, /ContextScreen/);
  assert.match(settingsRoute, /SettingsScreen/);

  const contextScreen = file('../../apps/mobile/src/features/context/ContextScreen.tsx');
  const settingsScreen = file('../../apps/mobile/src/features/settings/SettingsScreen.tsx');
  assert.match(contextScreen, /getContext/);
  assert.match(contextScreen, /saveContext/);
  assert.match(settingsScreen, /Notifications/);
  assert.match(settingsScreen, /BusinessMemoryPanel/);
  assert.match(settingsScreen, /resetExpoDemoBusiness/);
});

test('AtlasScreen reserves tab spacing only for routes that are actually inside the native tab group', () => {
  const atlasScreen = file('../../apps/mobile/src/components/AtlasScreen.tsx');
  assert.match(atlasScreen, /useSegments/);
  assert.match(atlasScreen, /segments\.includes\('\(tabs\)'\)/);
  assert.match(atlasScreen, /hasTabBar:\s*hasTabBar\s*&&\s*isPersistentTabRoute/);
  assert.match(atlasScreen, /segments\[0\]\s*===\s*'context'/);
  assert.match(atlasScreen, /segments\[0\]\s*===\s*'settings'/);
});
