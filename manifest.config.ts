import { defineManifest } from '@crxjs/vite-plugin'

/**
 * Manifest V3. Permissions are intentionally minimal:
 * - content_scripts on <all_urls>: the product is "select text on any page",
 *   so the selection listener must run everywhere. all_frames covers iframes.
 * - storage: install-state/version flag for the dictionary import.
 * No tabs / activeTab / scripting / host_permissions beyond the above.
 */
export default defineManifest({
  manifest_version: 3,
  name: 'Japanese Dictionary Popup',
  version: '0.1.0',
  description: 'Select Japanese text on any page to see its reading and meaning. Works fully offline.',
  minimum_chrome_version: '120',
  background: {
    // NOTE: entry file names must be unique across entries (service-worker.ts
    // vs content-script.ts) — with two entries both named index.ts, CRXJS
    // emits colliding chunk names and wires the SW loader to the wrong chunk.
    service_worker: 'src/background/service-worker.ts',
    type: 'module',
  },
  // No popup page — the action badge shows dictionary-import progress.
  action: {
    default_title: 'Japanese Dictionary Popup',
  },
  content_scripts: [
    {
      matches: ['<all_urls>'],
      js: ['src/content/content-script.ts'],
      all_frames: true,
      run_at: 'document_idle',
    },
  ],
  permissions: ['storage'],
})
