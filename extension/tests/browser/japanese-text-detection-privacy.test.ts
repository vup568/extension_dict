import { expect, test } from "@playwright/test";

const marker = "SYNTHETIC_PRIVATE_MARKER_日本語";

test("does not request, persist, or log classification input", async ({
  page,
}) => {
  const consoleMessages: string[] = [];
  const requests: string[] = [];
  page.on("console", (message) => consoleMessages.push(message.text()));
  page.on("request", (request) => requests.push(request.url()));

  await page.goto("/tests/browser/harness.html");
  requests.length = 0;

  const beforeStorage = await page.evaluate(() => ({
    local: { ...localStorage },
    session: { ...sessionStorage },
  }));
  const result = await page.evaluate(
    (value) => window.f01Classify(value),
    marker,
  );
  const afterStorage = await page.evaluate(() => ({
    local: { ...localStorage },
    session: { ...sessionStorage },
  }));

  expect(result).toEqual({ status: "ok", containsJapaneseText: true });
  expect(requests).toEqual([]);
  expect(afterStorage).toEqual(beforeStorage);
  expect(consoleMessages.join("\n")).not.toContain(marker);
});

test("does not expose marker content on invalid or unexpected failure results", async ({
  page,
}) => {
  await page.goto("/tests/browser/harness.html");

  const invalidResult = await page.evaluate(
    (value) => window.f01Classify({ marker: value }),
    marker,
  );
  const failureResult = await page.evaluate(
    (value) => window.f01ClassifyUnexpectedFailure(value),
    marker,
  );

  expect(invalidResult).toEqual({ status: "error", code: "invalid-input" });
  expect(failureResult).toEqual({
    status: "error",
    code: "unexpected-failure",
  });
  expect(JSON.stringify([invalidResult, failureResult])).not.toContain(marker);
});
