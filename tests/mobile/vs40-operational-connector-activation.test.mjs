import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const root = process.cwd();
const read = value => fs.readFileSync(path.join(root, value), 'utf8');
const modelUrl = new URL('../../apps/mobile/src/features/operational-data/operational-data-model.ts', import.meta.url);

test('VS-40 extracts a Drive folder id from the private folder link', async () => {
  assert.equal(fs.existsSync(fileURLToPath(modelUrl)), true);
  const { extractGoogleDriveFolderId } = await import(modelUrl.href);
  assert.equal(extractGoogleDriveFolderId('https://drive.google.com/drive/folders/1AbC_def-234?usp=drive_link'), '1AbC_def-234');
  assert.equal(extractGoogleDriveFolderId('https://drive.google.com/open?id=1AbC_def-234'), '1AbC_def-234');
  assert.equal(extractGoogleDriveFolderId('not-a-drive-folder'), null);
});

test('VS-40 mobile API follows the authoritative connector contract', () => {
  const api = read('apps/mobile/src/features/operational-data/operational-data-api.ts');
  assert.match(api, /method: 'POST'/);
  assert.match(api, /folderId/);
  assert.match(api, /schedule/);
  assert.match(api, /response\.status === 404/);
  assert.match(api, /status/);
  assert.match(api, /lastSuccessAt/);
});

test('VS-40 connector screen connects before sync and reloads authoritative state after sync', () => {
  const screen = read('apps/mobile/src/features/operational-data/OperationalDataScreen.tsx');
  assert.match(screen, /connectOperationalFolder/);
  assert.match(screen, /extractGoogleDriveFolderId/);
  assert.match(screen, /await syncOperationalFolder/);
  assert.match(screen, /await load\(\)/);
  assert.match(screen, /operationalScheduleChoices/);
  assert.match(screen, /setOperationalSchedule/);
});
