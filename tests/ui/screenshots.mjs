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

  await page.goto(`${baseUrl}/products`);
  await shot(page, `${device}-products`);
  await page.goto(`${baseUrl}/products?stock=Critical`);
  await shot(page, `${device}-products-critical`);
  await page.goto(`${baseUrl}/products?search=defter`);
  await shot(page, `${device}-products-search`);

  await page.goto(`${baseUrl}/products/1`);
  await shot(page, `${device}-product`);

  await page.goto(`${baseUrl}/products/new`);
  await shot(page, `${device}-product-new`);

  await page.goto(`${baseUrl}/movements`);
  await shot(page, `${device}-movements`);
  await page.goto(`${baseUrl}/movements/new`);
  await shot(page, `${device}-movement-new`);

  await page.goto(`${baseUrl}/suppliers`);
  await shot(page, `${device}-suppliers`);

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

  if (device === "desktop") {
    // New product: a duplicate SKU is refused, then the product is created with opening stock.
    await page.goto(`${baseUrl}/products/new`);
    await page.click("form button[type=submit]");
    await page.waitForTimeout(300);
    await shot(page, `${device}-product-validation`);

    await page.fill("#Sku", "kag-001");
    await page.fill("#Name", "Kraft zarf A4, 25'li");
    await page.selectOption("#CategoryId", { label: "Kâğıt ürünleri" });
    await page.fill("#PurchasePrice", "48");
    await page.fill("#SalePrice", "79.90");
    await page.fill("#CriticalLevel", "5");
    await page.fill("#OpeningStock", "12");
    await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
    await shot(page, `${device}-product-duplicate-sku`);

    await page.fill("#Sku", "kag-031");
    await page.fill("#Barcode", "8690000000001");
    await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
    await shot(page, `${device}-product-bad-barcode`);

    await page.fill("#Barcode", "");
    await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
    await shot(page, `${device}-product-created`);

    // Stock out, more than in stock, then a count.
    await page.click("button[data-bs-target='#entryOut']");
    await page.fill("#outQuantity", "50");
    await page.$eval("#outQuantity", (el) => el.removeAttribute("max"));
    await Promise.all([page.waitForNavigation(), page.click("#entryOut button[type=submit]")]);
    await shot(page, `${device}-stock-out-too-many`);

    await page.goto(`${baseUrl}/products`);
    await page.click("text=Kraft zarf");
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.click("button[data-bs-target='#entryOut']");
    await page.fill("#outQuantity", "9");
    await Promise.all([page.waitForNavigation(), page.click("#entryOut button[type=submit]")]);
    await shot(page, `${device}-stock-out-critical`);

    await page.click("button[data-bs-target='#entryCount']");
    await page.fill("#countQuantity", "2");
    await Promise.all([page.waitForNavigation(), page.click("#entryCount button[type=submit]")]);
    await shot(page, `${device}-stock-count`);

    await page.click("button[data-bs-target='#entryIn']");
    await page.fill("#inQuantity", "25");
    await page.fill("#inNote", "İrsaliye 2026/118");
    await Promise.all([page.waitForNavigation(), page.click("#entryIn button[type=submit]")]);
    await shot(page, `${device}-stock-in`);

    // Supplier: add one, then pause it.
    await page.goto(`${baseUrl}/suppliers/new`);
    await page.fill("#Name", "Deniz Ambalaj");
    await page.fill("#ContactName", "Cem Arı");
    await page.fill("#Phone", "0232 555 77 10");
    await page.fill("#Email", "Satis@DenizAmbalaj.example");
    await Promise.all([page.waitForNavigation(), page.click("form button[type=submit]")]);
    await shot(page, `${device}-supplier-created`);
    await Promise.all([page.waitForNavigation(), page.click("tr:has-text('Deniz Ambalaj') button:has-text('Pasife al')")]);
    await shot(page, `${device}-supplier-paused`);

    await page.goto(`${baseUrl}/movements?type=Count`);
    await shot(page, `${device}-movements-count`);

    // Exports keep the current filters.
    await page.goto(`${baseUrl}/products?stock=Critical`);
    await page.click("button:has-text('Dışa aktar')");
    await page.waitForTimeout(300);
    await shot(page, `${device}-export-menu`);
    for (const format of ["xlsx", "csv"]) {
      const [download] = await Promise.all([page.waitForEvent("download"), page.click(`a[href*='format=${format}']`)]);
      const name = download.suggestedFilename();
      console.log(`downloaded ${name}`);
      if (!name.endsWith(`.${format}`)) problems.push(`export: expected a .${format} file, got ${name}`);
      if (format === "xlsx") await page.click("button:has-text('Dışa aktar')");
    }
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
