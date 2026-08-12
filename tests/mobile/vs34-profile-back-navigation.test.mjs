import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';

const routes = [
  ['Settings', 'apps/mobile/app/settings.tsx'],
  ['Context', 'apps/mobile/app/context.tsx'],
  ['Feedback', 'apps/mobile/app/feedback.tsx'],
];

for (const [name, path] of routes) {
  test(`VS-34 ${name} uses native/light Profile back navigation`, () => {
    const source = readFileSync(path, 'utf8');

    assert.match(source, /headerBackTitle:\s*'Profile'/);
    assert.match(source, /headerTintColor:\s*tokens\.color\.green/);
    assert.match(source, /router\.canGoBack\(\)/);
    assert.match(source, /router\.replace\('\/(?:\(tabs\)\/)?profile'\)/);
    assert.match(source, />‹<\/Text>/);

    assert.doesNotMatch(
      source,
      /headerLeft:\s*\(\)\s*=>\s*\([\s\S]*?<Text[\s\S]*?>Profile<\/Text>/,
      'Profile must not render as the old custom text pill',
    );
  });
}
