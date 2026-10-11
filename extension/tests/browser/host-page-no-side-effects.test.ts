import { expect, test } from "@playwright/test";

test("does not mutate DOM, style, focus, selection, or input", async ({
  page,
}) => {
  await page.goto("/tests/browser/harness.html");

  const result = await page.evaluate(() => {
    const focusTarget = document.querySelector<HTMLInputElement>("#focus-target");
    const selectionTarget = document.querySelector("#selection-target");
    if (
      focusTarget === null ||
      selectionTarget === null ||
      selectionTarget.firstChild === null
    ) {
      throw new Error("Harness sentinel missing");
    }

    focusTarget.focus();
    const range = document.createRange();
    range.selectNodeContents(selectionTarget);
    const selection = window.getSelection();
    selection?.removeAllRanges();
    selection?.addRange(range);

    const input = "SYNTHETIC_PRIVATE_MARKER_日本語";
    const before = {
      html: document.body.innerHTML,
      style: document.body.getAttribute("style"),
      activeId: document.activeElement?.id,
      selection: selection?.toString(),
      input,
    };
    const classification = window.f01Classify(input);
    const after = {
      html: document.body.innerHTML,
      style: document.body.getAttribute("style"),
      activeId: document.activeElement?.id,
      selection: window.getSelection()?.toString(),
      input,
    };

    return { before, after, classification };
  });

  expect(result.classification).toEqual({
    status: "ok",
    containsJapaneseText: true,
  });
  expect(result.after).toEqual(result.before);
});
