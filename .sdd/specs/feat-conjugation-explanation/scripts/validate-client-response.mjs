// Test-only interoperability: validate actual HTTP output with the production client parser.
import { dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
const feature = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const { parseAnalysisResponse } = await import(pathToFileURL(resolve(feature, '../../../extension/src/api/analysisContract.ts')).href);
let input = '';
for await (const chunk of process.stdin) input += chunk;
const response = parseAnalysisResponse(JSON.parse(input), process.argv[2]);
process.stdout.write(JSON.stringify({ valid: true, occurrences: response.data.conjugations.length }));
