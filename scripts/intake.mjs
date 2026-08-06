import {read, headings, fail} from './lib.mjs';

const specs = [
  ['product/PRD.md', [
    ['product objective', 'product definition'],
    ['target users', 'primary customer and problem'],
    ['user roles', 'primary customer and problem'],
    ['core journeys', 'hero journey'],
    ['functional requirements'],
    ['business rules', 'product principles', 'opportunity eligibility'],
    ['out of scope', 'explicitly out of scope'],
    ['release scope', 'approved slice mapping'],
    ['open decisions', 'final product decision']
  ]],
  ['product/TRD.md', [
    ['architecture'],
    ['technology stack', 'technology direction'],
    ['modules and data ownership', 'module boundaries'],
    ['authentication', 'authentication and authorisation'],
    ['authorization', 'authentication and authorisation'],
    ['persistence and migrations', 'persistence'],
    ['external integrations', 'ai orchestration and openrouter boundary'],
    ['deployment', 'testing and ci/cd'],
    ['observability'],
    ['security', 'security and privacy'],
    ['testing strategy', 'testing and ci/cd'],
    ['open decisions', 'final technical decision']
  ]]
];

const errors = [];
for (const [file, requiredGroups] of specs) {
  const doc = read(file);
  const found = new Set(headings(doc));
  for (const alternatives of requiredGroups) {
    if (!alternatives.some((heading) => found.has(heading))) {
      errors.push(`${file}: missing heading "${alternatives.join('" or "')}"`);
    }
  }
  if (/Status:\s*Draft/i.test(doc)) errors.push(`${file}: status remains Draft`);
}

if (errors.length) fail(`INTAKE: BLOCKED\n- ${errors.join('\n- ')}`);
console.log('INTAKE: PASS');
