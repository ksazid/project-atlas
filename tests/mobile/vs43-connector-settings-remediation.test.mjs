import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import test from 'node:test';
const read=p=>fs.readFileSync(path.join(process.cwd(),p),'utf8');
test('VS-43 keeps schedule failures distinct from sync failures',()=>{const model=read('apps/mobile/src/features/operational-data/operational-data-model.ts');assert.match(model,/message\?\.includes\('sync schedule'\)/);assert.match(model,/Google Drive connected/);assert.match(model,/primaryAction: 'Sync now'/);assert.match(model,/The latest sync did not finish/);});
test('VS-43 settings uses one Atlas hierarchy instead of a duplicate native title',()=>{const route=read('apps/mobile/app/settings.tsx');const screen=read('apps/mobile/src/features/settings/SettingsScreen.tsx');assert.match(route,/headerShown: false/);assert.match(screen,/Back to Profile/);assert.match(screen,/BrandMark/);assert.match(screen,/SETTINGS/);assert.match(screen,/Control how Atlas works for you/);assert.match(screen,/tokens\.radius\.lg/);});
