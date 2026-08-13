import assert from 'node:assert/strict';
import fs from 'node:fs';
import test from 'node:test';

const read = path => fs.readFileSync(path, 'utf8');

test('VS-41 selects one CSV with the built-in Expo file picker', () => {
  const screen = read('apps/mobile/src/features/operational-data/OperationalDataScreen.tsx');
  assert.match(screen, /expo-file-system/);
  assert.match(screen, /File\.pickFileAsync/);
  assert.match(screen, /text\/csv/);
  assert.match(screen, /10 \* 1024 \* 1024/);
});

test('VS-41 reuses preview and confirm upload endpoints', () => {
  const api = read('apps/mobile/src/features/operational-data/operational-data-api.ts');
  assert.match(api, /operational-upload\/preview/);
  assert.match(api, /operational-upload\/confirm/);
  assert.match(api, /FormData/);
  assert.match(api, /PreviewFingerprint/);
});

test('VS-41 previews before confirmation while Drive stays primary', () => {
  const screen = read('apps/mobile/src/features/operational-data/OperationalDataScreen.tsx');
  assert.match(screen, /GOOGLE DRIVE · PRIMARY/);
  assert.match(screen, /FALLBACK/);
  assert.match(screen, /previewOperationalUpload/);
  assert.match(screen, /confirmOperationalUpload/);
  assert.match(screen, /Preview before importing/);
  assert.match(screen, /Confirm import/);
  assert.match(screen, /Ignored sensitive columns/);
  assert.match(screen, /Choose another CSV/);
});
