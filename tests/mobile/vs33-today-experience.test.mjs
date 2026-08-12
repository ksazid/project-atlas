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

test('VS-33 presents one concise Best move with one- or two-tap owner actions', () => {
  const ready = readySegment();

  assert.match(ready, />Best move</);
  assert.match(ready, /accessibilityLabel="Apply best move"/);
  assert.match(ready, />Apply</);
  assert.match(ready, /accessibilityLabel="Why this move"/);
  assert.match(ready, />Why\?</);
  assert.match(ready, />Later</);
  assert.match(ready, />Not relevant</);
  assert.match(ready, /decide\('skip'\)/, 'Later must keep the existing Skip decision semantics');
  assert.match(ready, /decide\('not-relevant'\)/);

  assert.doesNotMatch(ready, /RECOMMENDED MOVE/);
  assert.doesNotMatch(ready, /One action\. Clear reason\. Measurable outcome\./);
  assert.doesNotMatch(ready, /Apply this move/);
  assert.doesNotMatch(ready, /Skip for now/);
});

test('VS-33 keeps evidence and deep reasoning progressive on Opportunity Detail', () => {
  const ready = readySegment();

  assert.doesNotMatch(ready, />Why now</);
  assert.doesNotMatch(ready, />Evidence</);
  assert.doesNotMatch(ready, /Atlas interpretation/);
  assert.doesNotMatch(ready, /knowledgePackKey/);
  assert.doesNotMatch(ready, /Expires \{/);
  assert.match(ready, /router\.push\(`\/opportunities\/\$\{opportunity\.id\}`\)/);
});

test('VS-33 makes successful freshness and native pull-to-refresh explicit', () => {
  assert.match(screen, /RefreshControl/);
  assert.match(screen, /lastUpdatedAt/);
  assert.match(screen, /Updated just now/);
  assert.match(screen, /setLastUpdatedAt\(new Date\(\)\)/);
  assert.ok((screen.match(/refreshControl=\{refreshControl\}/g) ?? []).length >= 4, 'Ready and safe non-ready Today states should support pull-to-refresh');
});

test('VS-33 preserves safe displayed content when a manual refresh fails', () => {
  assert.match(screen, /if \(manual\) \{\s*setRefreshFailed\(true\);\s*\} else \{\s*setState\('error'\);\s*\}/s);
  assert.match(screen, /Couldn’t refresh · showing previous result/);
});

test('VS-33 uses concise truthful recovery language without fabricated BI data', () => {
  assert.match(screen, /Nothing strong enough to recommend yet/);
  assert.match(screen, /Today couldn[’']t refresh safely/);
  assert.match(screen, /Choose a goal to get your first Best move|Choose your first goal/);

  assert.doesNotMatch(screen, /Business Pulse|What Changed|Menu Intelligence|Competitor|Benchmark|Forecast|Ask Atlas/);
  assert.doesNotMatch(screen, /Bolt Food|Wolt|Google Places|places\.googleapis/i);
});
