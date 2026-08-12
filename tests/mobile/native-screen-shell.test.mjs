import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const files = [
  '../../apps/mobile/src/features/today-focus/TodayFocusScreen.tsx',
  '../../apps/mobile/src/features/business-hub/BusinessHubScreen.tsx',
  '../../apps/mobile/app/(tabs)/goals.tsx',
  '../../apps/mobile/src/features/context/ContextScreen.tsx',
  '../../apps/mobile/src/features/settings/SettingsScreen.tsx',
  '../../apps/mobile/app/create-business.tsx',
  '../../apps/mobile/app/welcome.tsx',
  '../../apps/mobile/app/sign-in.tsx',
  '../../apps/mobile/app/progressive-questions.tsx',
  '../../apps/mobile/app/edit-business.tsx',
  '../../apps/mobile/src/features/business-hub/BusinessMenuScreen.tsx',
  '../../apps/mobile/src/features/opportunity-detail/OpportunityDetailScreen.tsx',
  '../../apps/mobile/src/features/execution-kit/ExecutionKitScreen.tsx',
  '../../apps/mobile/src/features/history/HistoryScreen.tsx',
  '../../apps/mobile/src/features/weekly-review/WeeklyReviewScreen.tsx',
  '../../apps/mobile/src/features/notifications/NotificationCenterScreen.tsx',
];

const sources = files.map(path => [path, readFileSync(new URL(path, import.meta.url), 'utf8')]);
const todaySource = readFileSync(new URL('../../apps/mobile/src/features/today-focus/TodayFocusScreen.tsx', import.meta.url), 'utf8');
const atlasScreenSource = readFileSync(new URL('../../apps/mobile/src/components/AtlasScreen.tsx', import.meta.url), 'utf8');

test('every current first-party screen uses AtlasScreen', () => {
  for (const [path, source] of sources) {
    assert.match(source, /AtlasScreen/, `${path} must use AtlasScreen`);
  }
});

test('migrated screens do not retain known one-device page offsets', () => {
  for (const [path, source] of sources) {
    assert.doesNotMatch(source, /paddingTop:\s*(54|57|58)\b/, `${path} retains legacy page top padding`);
  }
});

test('Today non-ready states do not vertically center the entire tall-device viewport', () => {
  assert.match(todaySource, /stateContainer:\s*\{[^}]*justifyContent:\s*'flex-start'/s);
  assert.doesNotMatch(todaySource, /stateContainer:\s*\{[^}]*justifyContent:\s*'center'/s);
});

test('scrolling screens keep the top safe area outside scrollable content', () => {
  assert.match(atlasScreenSource, /const fixedSafeAreaStyle:[^{]*\{[^}]*paddingTop:\s*metrics\.paddingTop[^}]*\}/s);
  assert.match(atlasScreenSource, /const scrollContentSafeAreaStyle:[^{]*\{[^}]*paddingBottom:\s*metrics\.paddingBottom[^}]*paddingHorizontal:\s*metrics\.paddingHorizontal[^}]*\}/s);
  assert.match(atlasScreenSource, /<View style=\{\[\{ flex: 1 \}, fixedSafeAreaStyle\]\}>[\s\S]*<ScrollView/);
  assert.doesNotMatch(atlasScreenSource, /const scrollContentSafeAreaStyle:[^{]*\{[^}]*paddingTop:/s);
});