import { parseTemplate } from '@angular/compiler';
import fs from 'node:fs';
import path from 'node:path';
import process from 'node:process';

const appRoot = path.resolve('src/app');
const i18nRoot = path.resolve('src/assets/i18n');
const languages = ['en', 'bg', 'de'];
const dynamicTranslationKeys = new Map([
  ['homeV3.matchReason.perfectFit', 'backend recommendations match reason'],
  ['homeV3.matchReason.efficient', 'backend recommendations match reason'],
  ['homeV3.matchReason.powerful', 'backend recommendations match reason'],
  ['homeV3.matchReason.quietBedroom', 'backend recommendations match reason'],
  ['homeV3.matchReason.coldClimate', 'backend recommendations match reason'],
  ['homeV3.matchReason.budgetFriendly', 'backend recommendations match reason'],
  ['homeV3.matchReason.fallback', 'backend recommendations match reason']
]);

const keyPatterns = [
  /['"]([A-Za-z0-9_.-]+)['"]\s*\|\s*translate/g,
  /(?:this\.)?(?:translate|translateService|languageService)\s*\.\s*(?:instant|get)\(\s*['"]([A-Za-z0-9_.-]+)['"]/g,
  /translationKey\s*:\s*['"]([A-Za-z0-9_.-]+)['"]/g,
  /(?:apiErrorToTranslationKey|toTranslationKey)\([\s\S]*?,\s*['"]([A-Za-z0-9_.-]+)['"]\s*\)/g
];

const staticAttributeNames = new Set(['placeholder', 'aria-label', 'title', 'alt']);
const visibleTextAllowlist = new Set([
  'A', 'A+', 'A++', 'A+++', 'AMEX',
  'B', 'BG', 'BGN', 'BGN (лв)', 'BTU/h',
  'C', 'CDL', 'ClimaSite',
  'D', 'DE',
  'EN', 'EUR', 'EUR (€)',
  'FAQ', 'Facebook',
  'ID', 'Instagram',
  'LinkedIn',
  'Pal', 'Pay',
  'Q&A',
  'SEO', 'SKU',
  'URL', 'USD', 'USD ($)', 'VAT',
  'X', 'YouTube',
  'dB', 'kW', 'kg', 'm2', 'm²'
]);

function walk(directory) {
  const entries = fs.readdirSync(directory, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...walk(entryPath));
    } else {
      files.push(entryPath);
    }
  }

  return files;
}

function getTranslationValue(translations, key) {
  return key.split('.').reduce((value, segment) => {
    if (value && Object.prototype.hasOwnProperty.call(value, segment)) {
      return value[segment];
    }

    return undefined;
  }, translations);
}

function getProductionFiles() {
  return walk(appRoot).filter(file =>
    /\.(ts|html)$/.test(file) &&
    !file.endsWith('.spec.ts')
  );
}

function collectStaticTranslationKeys(files) {
  const keys = new Map();

  for (const file of files) {
    const source = fs.readFileSync(file, 'utf8');
    for (const pattern of keyPatterns) {
      let match;
      while ((match = pattern.exec(source))) {
        const key = match[1];
        if (!key.includes('.') || key.startsWith('.')) {
          continue;
        }

        if (!keys.has(key)) {
          keys.set(key, new Set());
        }
        keys.get(key).add(path.relative(process.cwd(), file));
      }
    }
  }

  return keys;
}

function extractTemplates(file, source) {
  if (file.endsWith('.html')) {
    return [{ template: source, file }];
  }

  const templates = [];
  const inlineTemplatePattern = /template\s*:\s*`([\s\S]*?)`/g;
  let match;

  while ((match = inlineTemplatePattern.exec(source))) {
    templates.push({ template: match[1], file });
  }

  return templates;
}

function normalizeText(text) {
  return text.replace(/\s+/g, ' ').trim();
}

function isLikelyVisibleCopy(text) {
  const normalized = normalizeText(text);

  if (!normalized || visibleTextAllowlist.has(normalized)) {
    return false;
  }

  if (!/[A-Za-zА-Яа-я]/.test(normalized)) {
    return false;
  }

  if (/^https?:/.test(normalized) || /^mailto:/.test(normalized) || /^tel:/.test(normalized)) {
    return false;
  }

  if (/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(normalized)) {
    return false;
  }

  if (/^[-+]?\d/.test(normalized)) {
    return false;
  }

  return !(/^[A-Z0-9+\/°%., -]+$/.test(normalized) && normalized.length <= 20);
}

function visitTemplateNode(node, file, findings) {
  if (node.constructor.name === 'Text' && isLikelyVisibleCopy(node.value)) {
    findings.push({
      file,
      type: 'text',
      value: normalizeText(node.value)
    });
  }

  if (node.constructor.name === 'Element') {
    for (const attribute of node.attributes || []) {
      if (staticAttributeNames.has(attribute.name) && isLikelyVisibleCopy(attribute.value)) {
        findings.push({
          file,
          type: 'attribute',
          value: `${attribute.name}="${attribute.value}"`
        });
      }
    }
  }

  for (const childKey of ['children', 'branches', 'cases']) {
    const children = node[childKey];
    if (!Array.isArray(children)) {
      continue;
    }

    for (const child of children) {
      visitTemplateNode(child, file, findings);
    }
  }
}

function collectHardcodedTemplateText(files) {
  const findings = [];

  for (const file of files) {
    const source = fs.readFileSync(file, 'utf8');
    for (const { template } of extractTemplates(file, source)) {
      const parsed = parseTemplate(template, path.relative(process.cwd(), file), {
        preserveWhitespaces: false
      });

      if (parsed.errors?.length) {
        for (const error of parsed.errors) {
          findings.push({
            file: path.relative(process.cwd(), file),
            type: 'parse-error',
            value: error.msg
          });
        }
        continue;
      }

      for (const node of parsed.nodes) {
        visitTemplateNode(node, path.relative(process.cwd(), file), findings);
      }
    }
  }

  return findings;
}

const userFacingSignalSetters = [
  'error',
  '_error',
  'errorMessage',
  'questionError',
  'answerError'
];

function isTranslationKey(value) {
  return /^[A-Za-z][A-Za-z0-9]*(?:[._-][A-Za-z0-9]+)+$/.test(value);
}

function collectSignalErrorUsage(files) {
  const findings = [];
  const keys = new Map();
  const setterPattern = new RegExp(
    `(?:this\\.)?(?:${userFacingSignalSetters.map(name => name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('|')})\\.set\\(\\s*['"]([^'"]+)['"]`,
    'g'
  );
  const setErrorPattern = /\.setError\(\s*['"]([^'"]+)['"]/g;
  const fallbackSetterPattern = new RegExp(
    `(?:this\\.)?(?:${userFacingSignalSetters.map(name => name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('|')})\\.set\\([\\s\\S]*?\\|\\|\\s*['"]([^'"]+)['"]\\s*\\)`,
    'g'
  );
  const fallbackSetErrorPattern = /\.setError\([\s\S]*?\|\|\s*['"]([^'"]+)['"]\s*\)/g;

  for (const file of files.filter(candidate => candidate.endsWith('.ts'))) {
    const source = fs.readFileSync(file, 'utf8');
    for (const pattern of [setterPattern, setErrorPattern, fallbackSetterPattern, fallbackSetErrorPattern]) {
      let match;
      while ((match = pattern.exec(source))) {
        const value = match[1];
        if (isTranslationKey(value)) {
          if (!keys.has(value)) {
            keys.set(value, new Set());
          }
          keys.get(value).add(path.relative(process.cwd(), file));
        } else if (isLikelyVisibleCopy(value)) {
          findings.push({
            file: path.relative(process.cwd(), file),
            type: 'signal-error',
            value
          });
        }
      }
    }
  }

  return { findings, keys };
}

function main() {
  const files = getProductionFiles();
  const dictionaries = Object.fromEntries(
    languages.map(language => [
      language,
      JSON.parse(fs.readFileSync(path.join(i18nRoot, `${language}.json`), 'utf8'))
    ])
  );

  const staticKeys = collectStaticTranslationKeys(files);
  const signalErrorUsage = collectSignalErrorUsage(files);
  for (const [key, locations] of signalErrorUsage.keys) {
    if (!staticKeys.has(key)) {
      staticKeys.set(key, new Set());
    }

    for (const location of locations) {
      staticKeys.get(key).add(location);
    }
  }

  for (const [key, source] of dynamicTranslationKeys) {
    if (!staticKeys.has(key)) {
      staticKeys.set(key, new Set([source]));
    }
  }
  const missingKeys = [];

  for (const [key, locations] of staticKeys) {
    for (const language of languages) {
      if (getTranslationValue(dictionaries[language], key) === undefined) {
        missingKeys.push({
          language,
          key,
          file: [...locations][0]
        });
      }
    }
  }

  const hardcodedText = collectHardcodedTemplateText(files);
  const hardcodedSignalErrors = signalErrorUsage.findings;

  if (missingKeys.length === 0 && hardcodedText.length === 0 && hardcodedSignalErrors.length === 0) {
    console.log(`i18n check passed: ${staticKeys.size} static keys verified across ${languages.join(', ')}`);
    return;
  }

  if (missingKeys.length > 0) {
    console.error('\nMissing translation keys:');
    for (const finding of missingKeys) {
      console.error(`- ${finding.language}: ${finding.key} (${finding.file})`);
    }
  }

  if (hardcodedText.length > 0) {
    console.error('\nHardcoded visible template text:');
    for (const finding of hardcodedText) {
      console.error(`- ${finding.type}: ${finding.file}: ${finding.value}`);
    }
  }

  if (hardcodedSignalErrors.length > 0) {
    console.error('\nHardcoded user-facing signal error text:');
    for (const finding of hardcodedSignalErrors) {
      console.error(`- ${finding.type}: ${finding.file}: ${finding.value}`);
    }
  }

  process.exitCode = 1;
}

main();
