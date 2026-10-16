// Visual smoke test: visits the main screens with the demo data, saves desktop and 360px-wide
// screenshots and fails when a page is wider than the viewport.
import { chromium } from "playwright";
import { mkdir } from "node:fs/promises";

const baseUrl = process.env.BASE_URL ?? "http://localhost:5080";
const out = new URL("./output/", import.meta.url).pathname;

const viewports = {
  desktop: { viewport: { width: 1440, height: 900 } },
  mobile: { viewport: { width: 360, height: 780 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true },
};

await mkdir(out, { recursive: true });
const browser = await chromium.launch();
const problems = [];

async function shot(page, name) {
  await page.waitForLoadState("networkidle").catch(() => {});
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  if (overflow > 1) problems.push(`${name}: page is ${overflow}px wider than the viewport`);
  await page.screenshot({ path: `${out}${name}.png`, fullPage: true });
  console.log(`saved ${name}.png`);
}

for (const [device, options] of Object.entries(viewports)) {
  const context = await browser.newContext({ ...options, locale: "tr-TR" });
  const page = await context.newPage();

  await page.goto(`${baseUrl}/`);
  await shot(page, `${device}-dashboard`);

  await page.goto(`${baseUrl}/categories`);
  await shot(page, `${device}-categories`);

  await page.goto(`${baseUrl}/categories/new`);
  await shot(page, `${device}-category-new`);

  if (device === "desktop") {
    // Duplicate names are refused regardless of case.
    await page.fill("#Name", "KÂĞIT ÜRÜNLERİ");
    await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
    await shot(page, `${device}-category-duplicate`);

    await page.fill("#Name", "Ambalaj");
    await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
    await shot(page, `${device}-category-created`);

    // A category with products cannot be deleted.
    await page.click("tr:has-text('Dosyalama') button:has-text('Sil')");
    await page.waitForTimeout(400);
    await shot(page, `${device}-category-confirm`);
    await Promise.all([page.waitForNavigation(), page.click("[data-confirm-accept]")]);
    await shot(page, `${device}-category-delete-blocked`);

    // The empty one can.
    await page.click("tr:has-text('Ambalaj') button:has-text('Sil')");
    await page.waitForTimeout(400);
    await Promise.all([page.waitForNavigation(), page.click("[data-confirm-accept]")]);
    await shot(page, `${device}-category-deleted`);
  }

  if (device === "mobile") {
    await page.goto(`${baseUrl}/`);
    await page.click("button[data-bs-target='#appSidebar']");
    await page.waitForTimeout(400);
    await page.screenshot({ path: `${out}${device}-menu.png` });
    console.log(`saved ${device}-menu.png`);
  }

  await page.goto(`${baseUrl}/does-not-exist`);
  await shot(page, `${device}-not-found`);

  await context.close();
}

await browser.close();

if (problems.length > 0) {
  console.error("Layout problems:\n" + problems.join("\n"));
  process.exit(1);
}
