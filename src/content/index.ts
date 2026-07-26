/**
 * Content script composition root: wires selection detection to the popup
 * controller. This is the ONLY place Japanese-specific knowledge enters the
 * content layer (as the containsJapanese predicate); everything else here is
 * language-agnostic.
 */
import { containsJapanese } from '../core/language/japanese/detect'
import { PopupController } from './popup-controller'
import { watchSelection } from './selection'
import type { DictionaryEntry } from '../shared/types'

/**
 * PHASE 2 MOCK — replaced by real lookups over chrome.runtime messaging in
 * Phase 4. Two entries so entry-cycling is testable; five senses on the
 * first so the "show more" cap is testable.
 */
function mockEntries(selectedText: string): readonly DictionaryEntry[] {
  const shown = selectedText.length <= 12 ? selectedText : `${selectedText.slice(0, 12)}…`
  return [
    {
      id: 'mock-1',
      expression: shown,
      reading: 'もっく',
      senses: [
        { partsOfSpeech: ['n'], glosses: ['mock entry rendered for the current selection'] },
        { partsOfSpeech: ['n'], glosses: ['placeholder sense', 'demo gloss'] },
        { partsOfSpeech: ['adj-na'], glosses: ['third sense to fill space'] },
        { partsOfSpeech: ['n'], glosses: ['fourth sense — last one shown before the cap'] },
        { partsOfSpeech: ['exp'], glosses: ['fifth sense hidden behind "show more"'] },
      ],
    },
    {
      id: 'mock-2',
      expression: '学生',
      reading: 'がくせい',
      senses: [{ partsOfSpeech: ['n'], glosses: ['student'] }],
    },
  ]
}

const controller = new PopupController()

watchSelection(containsJapanese, {
  onSelect: (text, range) => controller.show(mockEntries(text), range),
  onClear: () => controller.handleSelectionCleared(),
})
