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
    service_worker: 'src/background/index.ts',
    type: 'module',
  },
  // No popup page — the action badge shows dictionary-import progress.
  action: {
    default_title: 'Japanese Dictionary Popup',
  },
  content_scripts: [
    {
      matches: ['<all_urls>'],
      js: ['src/content/index.ts'],
      all_frames: true,
      run_at: 'document_idle',
    },
  ],
  permissions: ['storage'],
})
