import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const screen = readFileSync('apps/mobile/src/features/today-focus/TodayFocusScreen.tsx', 'utf8');

function readySegment() {
  const marker = 'const opportunity = focus.opportunity';
  const start = screen.indexOf(marker);
  assert.ok(start >= 0, 'Today ready-state marker is missing');
  return screen.slice(start);
}

function styleBody(styleName) {
  const pattern = new RegExp(`${styleName}:\\s*\\{([^}]*)\\}`);
  const match = screen.match(pattern);
  assert.ok(match, `${styleName} style is missing`);
  return match[1];
}

test('VS-34 makes the ready state read as one obvious daily task', () => {
  const ready = readySegment();

  assert.match(ready, />1 thing worth doing today</);
  assert.match(ready, />BEST MOVE</);
  assert.match(ready, />I’ll do this</);
  assert.match(ready, />Why this\?</);
  assert.match(ready, /accessibilityLabel="More actions"/);
  assert.match(ready, />Later</);
  assert.match(ready, />Not relevant</);

  assert.doesNotMatch(ready, /Want the reasoning\?/);
  assert.doesNotMatch(ready, /<Metric\b/);
});

test('VS-34 separates editorial page typography from task typography', () => {
  assert.match(styleBody('pageTitle'), /fontFamily:\s*'Georgia'/);
  assert.doesNotMatch(styleBody('bestMoveTitle'), /fontFamily:\s*'Georgia'/);
  assert.match(styleBody('bestMoveTitle'), /fontWeight:\s*'8|fontWeight:\s*'9/);
});

test('VS-34 presents existing evidence strength in one compact row without invented metrics', () => {
  const ready = readySegment();

  assert.match(
    ready,
    /\{opportunity\.expectedImpact\}\s*impact\s*·\s*\{opportunity\.effort\}\s*effort\s*·\s*\{opportunity\.confidence\}\s*confidence/,
  );
  assert.doesNotMatch(ready, /% confidence/);
});

test('VS-34 keeps existing owner decision semantics behind the simplified controls', () => {
  const ready = readySegment();

  assert.match(ready, /decide\('apply'\)/);
  assert.match(ready, /decide\('skip'\)/);
  assert.match(ready, /decide\('not-relevant'\)/);
  assert.match(ready, /router\.push\(`\/opportunities\/\$\{opportunity\.id\}`\)/);
});
