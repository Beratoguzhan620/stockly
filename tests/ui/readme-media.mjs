// Regenerates the README images (docs/screenshots) from the demo data.
// Needs the app running with Seed:DemoContent=true. The walkthrough video is saved as
// output/media/walkthrough.webm; CI turns it into docs/screenshots/demo.gif with ffmpeg.
import { chromium } from "playwright";
import { mkdir, readdir, rename } from "node:fs/promises";

const baseUrl = process.env.BASE_URL ?? "http://localhost:5080";
const out = new URL("./output/media/", import.meta.url).pathname;
const pause = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

await mkdir(out, { recursive: true });
const browser = await chromium.launch();

async function shot(page, name) {
  await page.waitForLoadState("networkidle").catch(() => {});
  await page.screenshot({ path: `${out}${name}.png` });
  console.log(`saved ${name}.png`);
}

{
  const context = await browser.newContext({ viewport: { width: 1280, height: 800 }, locale: "tr-TR" });
  const page = await context.newPage();

  await page.goto(`${baseUrl}/`);
  await shot(page, "dashboard");
  await page.goto(`${baseUrl}/products`);
  await shot(page, "products");
  await page.goto(`${baseUrl}/products?stock=Critical`);
  await shot(page, "products-critical");
  await page.goto(`${baseUrl}/products/1`);
  await shot(page, "product");
  await page.goto(`${baseUrl}/movements`);
  await shot(page, "movements");
  await page.goto(`${baseUrl}/movements/new?type=Out`);
  await shot(page, "stock-entry");
  await page.goto(`${baseUrl}/suppliers`);
  await shot(page, "suppliers");

  await context.close();
}

{
  const context = await browser.newContext({ viewport: { width: 390, height: 844 }, deviceScaleFactor: 2, isMobile: true, hasTouch: true, locale: "tr-TR" });
  const page = await context.newPage();
  await page.goto(`${baseUrl}/`);
  await shot(page, "mobile-dashboard");
  await context.close();
}

// Walkthrough: dashboard → critical products → receive goods → movement history.
{
  const context = await browser.newContext({
    viewport: { width: 1280, height: 800 },
    locale: "tr-TR",
    recordVideo: { dir: out, size: { width: 1280, height: 800 } },
  });
  const page = await context.newPage();

  await page.goto(`${baseUrl}/`);
  await pause(1800);
  await page.click(".stat-card.attention");
  await page.waitForLoadState("networkidle").catch(() => {});
  await pause(1500);
  await page.click(".product-link >> nth=0");
  await page.waitForLoadState("networkidle").catch(() => {});
  await pause(1500);
  await page.fill("#inQuantity", "");
  await page.type("#inQuantity", "60", { delay: 120 });
  await page.type("#inNote", "İrsaliye 2026/204", { delay: 30 });
  await pause(500);
  await Promise.all([page.waitForNavigation(), page.click("#entryIn button[type=submit]")]);
  await pause(2200);
  await page.goto(`${baseUrl}/movements`);
  await pause(1800);

  await context.close();

  const video = (await readdir(out)).find((name) => name.endsWith(".webm"));
  if (video) await rename(`${out}${video}`, `${out}walkthrough.webm`);
}

await browser.close();
