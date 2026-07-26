/**
 * Copies the kuromoji IPADIC dictionary (~96 MB of .dat files) from
 * node_modules into public/kuromoji so Vite packages it into the extension
 * and the service worker can load it fully offline.
 * (The @aiktb/kuromoji fork serves uncompressed .dat files: slightly bigger
 * on disk, but no decompression cost on every service-worker cold start.)
 */
import { copyFileSync, mkdirSync, readdirSync, rmSync, statSync } from 'node:fs'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'

const ROOT = fileURLToPath(new URL('..', import.meta.url))
const SRC = join(ROOT, 'node_modules', '@aiktb', 'kuromoji', 'dict')
const OUT = join(ROOT, 'public', 'kuromoji')

const files = readdirSync(SRC).filter((name) => name.endsWith('.dat'))
if (files.length === 0) throw new Error(`No .dat files found in ${SRC} — did npm install run?`)

rmSync(OUT, { recursive: true, force: true })
mkdirSync(OUT, { recursive: true })
let total = 0
for (const name of files) {
  copyFileSync(join(SRC, name), join(OUT, name))
  total += statSync(join(OUT, name)).size
}
console.log(`Copied ${files.length} kuromoji dict files to public/kuromoji (${(total / 1024 / 1024).toFixed(1)} MB)`)
