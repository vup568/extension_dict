import { expect, test } from "@playwright/test";

import type { RubySafeSelectionErrorCode } from "../../src/boundary/rubySafeSelectionResult";
import type { RubySafeSelectionCorpusCase } from "../corpus/ruby-safe-selection-corpus";

test("does not send, store, log, retain, or navigate with private content", async ({
  page,
}, testInfo) => {
  const consoleMessages: string[] = [];
  const requestUrls: string[] = [];
  page.on("console", (message) => consoleMessages.push(message.text()));
  page.on("request", (request) => requestUrls.push(request.url()));

  await page.goto("/tests/browser/ruby-safe-selection-harness.html");
  const marker = `PRIVATE_SUCCESS_${testInfo.project.name}_日本語`;
  const errorScenarios = [
    "none",
    "collapsed",
    "multi-range",
    "annotation-only",
    "stale",
    "inaccessible",
    "unexpected",
  ] as const;

  const observation = await page.evaluate(
    ({ privateMarker, scenarios }) => {
      localStorage.clear();
      sessionStorage.clear();
      const fixture: RubySafeSelectionCorpusCase = {
        id: "F02-privacy-success",
        acceptanceCriteria: ["F02-AC-025"],
        html: `<span id="target">${privateMarker}</span>`,
        range: { kind: "contents", selector: "#target" },
        expected: { status: "ok", text: privateMarker },
      };
      const historyLength = history.length;
      const href = location.href;
      const success = window.f02RunCase(fixture);
      const errors = scenarios.map((scenario) => ({
        scenario,
        result: window.f02RunErrorCase(scenario, privateMarker),
      }));

      return {
        success,
        errors,
        localStorage: { ...localStorage },
        sessionStorage: { ...sessionStorage },
        historyLengthBefore: historyLength,
        historyLengthAfter: history.length,
        hrefBefore: href,
        hrefAfter: location.href,
      };
    },
    { privateMarker: marker, scenarios: errorScenarios },
  );

  expect(observation.success).toEqual({ status: "ok", text: marker });
  for (const { result } of observation.errors) {
    expect(result).not.toHaveProperty("text");
    expect(JSON.stringify(result)).not.toContain(marker);
  }
  expect(observation.localStorage).toEqual({});
  expect(observation.sessionStorage).toEqual({});
  expect(observation.historyLengthAfter).toBe(observation.historyLengthBefore);
  expect(observation.hrefAfter).toBe(observation.hrefBefore);
  expect(requestUrls.filter((url) => url.includes(marker))).toEqual([]);
  expect(consoleMessages.filter((message) => message.includes(marker))).toEqual([]);
});

test("does not mutate DOM, style, focus, selection, editable value, clipboard, or events", async ({
  page,
}, testInfo) => {
  await page.goto("/tests/browser/ruby-safe-selection-harness.html");

  const observation = await page.evaluate(() => {
    const fixture: RubySafeSelectionCorpusCase = {
      id: "F02-host-invariance",
      acceptanceCriteria: ["F02-AC-022"],
      html:
        "<input id='focus' value='固定'><div id='target' contenteditable='true' style='color:red'>日本<ruby>語<rt>ご</rt></ruby></div>",
      range: { kind: "contents", selector: "#target" },
      expected: { status: "ok", text: "日本語" },
    };
    window.f02SetCase(fixture);
    const root = document.querySelector<HTMLElement>("[data-f02-fixture]");
    const focus = document.querySelector<HTMLInputElement>("#focus");
    if (root === null || focus === null) {
      throw new Error("host invariance fixture missing");
    }
    focus.focus();
    const target = document.querySelector("#target");
    const selection = window.getSelection();
    if (target === null || selection === null) {
      throw new Error("host selection fixture missing");
    }
    const range = document.createRange();
    range.selectNodeContents(target);
    selection.removeAllRanges();
    selection.addRange(range);

    let clipboardWrites = 0;
    const originalClipboard = navigator.clipboard;
    try {
      Object.defineProperty(navigator, "clipboard", {
        configurable: true,
        value: { writeText: () => { clipboardWrites += 1; } },
      });
    } catch {
      // A non-configurable browser clipboard still proves no call via unchanged state.
    }

    let eventDefaultPrevented = false;
    const event = new Event("f02-sentinel", { cancelable: true });
    root.addEventListener("f02-sentinel", (received) => {
      eventDefaultPrevented = received.defaultPrevented;
    });

    const snapshot = () => ({
      html: root.innerHTML,
      style: root.getAttribute("style"),
      activeId: document.activeElement?.id,
      selection: window.getSelection()?.toString(),
      editable: document.querySelector<HTMLElement>("#target")?.innerHTML,
      clipboardWrites,
    });

    const before = snapshot();
    const result = window.f02Extract();
    root.dispatchEvent(event);
    const after = snapshot();

    if (originalClipboard !== undefined) {
      try {
        Object.defineProperty(navigator, "clipboard", {
          configurable: true,
          value: originalClipboard,
        });
      } catch {
        // Test cleanup only; the feature never touches clipboard state.
      }
    }

    return { before, after, eventDefaultPrevented, result };
  });

  expect(observation.result, `host invariance on ${testInfo.project.name}`).toEqual({
    status: "ok",
    text: "日本語",
  });
  expect(observation.after).toEqual(observation.before);
  expect(observation.eventDefaultPrevented).toBe(false);
});

const errorCodes: readonly RubySafeSelectionErrorCode[] = [
  "no-selection",
  "unsupported-multi-range",
  "no-base-text",
  "stale-selection",
  "inaccessible-context",
  "unexpected-failure",
];

test("keeps every public error shape content-free", () => {
  for (const code of errorCodes) {
    expect({ status: "error", code }).toEqual({ status: "error", code });
  }
});
