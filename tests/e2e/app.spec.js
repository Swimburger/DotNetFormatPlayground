import { expect, test } from "@playwright/test";

async function openPlayground(page) {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: ".NET Format String Tester", exact: true })).toBeVisible({ timeout: 60_000 });
  await expect(page.getByRole("button", { name: "Format with .NET" })).toBeVisible();
  await expect(page.getByRole("heading", { name: ".NET Format String Tester" })).toHaveCSS("outline-style", "none");
}

test("compares culture and invariant standard formats", async ({ page }) => {
  await openPlayground(page);
  const table = page.getByRole("table", { name: "Standard formats for Decimal" });
  await expect(table).toBeVisible();
  await expect(table.getByRole("columnheader", { name: "en-US" })).toBeVisible();
  await expect(table.getByRole("columnheader", { name: "Invariant" })).toBeVisible();
  await expect(table.getByRole("row", { name: /Currency/ }).getByText("$1,234,567.89", { exact: true }).first()).toBeVisible();
  await expect(page.getByRole("status").getByText("1234567.890123456789", { exact: true })).toBeVisible();
});

test("preserves a BigInteger beyond JavaScript precision", async ({ page }) => {
  await openPlayground(page);
  await page.getByLabel("Try an example").selectOption("big");
  await expect(page.locator(".result-status")).toContainText("1234567890123456789012345678901234567890");
  await expect(page.getByText("1.234.567.890.123.456.789.012.345.678.901.234.567.890 units", { exact: true })).toBeVisible();
  await page.getByText("C# for this result").click();
  await expect(page.getByText("BigInteger.Parse", { exact: false })).toBeVisible();
});

test("updates the custom preview and generated code", async ({ page }) => {
  await openPlayground(page);
  const format = page.getByRole("textbox", { name: "Custom format", exact: true });
  await format.fill("0000000000.0000");
  await expect(page.locator(".preview-card code")).toHaveText("0001234567.8901");
  await page.getByText("C# for this result").click();
  await page.getByRole("button", { name: "string.Format" }).click();
  await expect(page.locator(".code-panel pre")).toContainText("string.Format(culture");
});

test("validates only after interaction and links to the DateTime lab", async ({ page }) => {
  await openPlayground(page);
  const input = page.getByLabel("Typed value");
  await input.fill("not decimal");
  await expect(input).toHaveAttribute("aria-invalid", "false");
  await input.blur();
  await expect(input).toHaveAttribute("aria-invalid", "true");
  await expect(page.locator(".result-status")).toContainText("cannot be parsed");
  await page.getByText("Formatting documentation").click();
  await expect(page.getByRole("link", { name: ".NET Date and Time Tester" })).toHaveAttribute("href", "https://datetime.swimburger.net");
});

test("is semantic, responsive, and served with production headers", async ({ page, request }) => {
  await page.setViewportSize({ width: 375, height: 812 });
  await openPlayground(page);
  await expect(page.locator("main")).toHaveCount(1);
  await expect(page.locator("table caption")).toHaveCount(1);
  expect(await page.evaluate(() => document.documentElement.scrollWidth)).toBeLessThanOrEqual(375);
  await expect(page.locator(".site-footer")).toContainText("processed locally");

  const root = await request.get("/");
  expect(root.status()).toBe(200);
  expect(root.headers()["cache-control"]).toContain("must-revalidate");
  expect(root.headers()["x-content-type-options"]).toBe("nosniff");
  const deepLink = await request.get("/reference/custom-numeric", { headers: { Accept: "text/html" } });
  expect(deepLink.status()).toBe(200);
});
