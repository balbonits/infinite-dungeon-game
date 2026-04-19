// Game-specific batch runner for Infinite Dungeon Game MVP characters.
// Drives the local Vite dev server (http://localhost:5173) via Playwright,
// loads a character recipe via URL hash, captures the Download PNG button,
// saves to the target path in the game repo.
//
// Usage:  npm run dev   # keep dev server running in another terminal
//         node game-batch.mjs              # runs all recipes
//         node game-batch.mjs warrior      # runs just one
//
// All recipes are expressed as URL hash params: sex=<bodyType>&<type>=<Name>_<variant>.
// Name parts use underscores (e.g. "Body Color" → "Body_Color").

import { chromium } from "@playwright/test";
import { mkdirSync } from "node:fs";
import { dirname, resolve, basename } from "node:path";

const REPO_ROOT = resolve(import.meta.dirname, "../..");
const SERVER_URL = process.env.LPC_SERVER_URL ?? "http://localhost:5173";

// Character recipes — keys match the keys in RECIPES below.
// Each entry: [hashParams object, output path relative to repo root]
const RECIPES = {
  // VALIDATION: fixture's known-good URL, minimal known-good layers.
  validation: {
    hash: {
      sex: "male",
      body: "Body_Color_light",
      head: "Human_Male_light",
      hair: "Natural_violet",
      jacket: "Iverness_cloak_black",
      legs: "Long_Pants_orange",
      shoes: "Revised_Shoes_bluegray",
      weapon: "Longsword_longsword",
    },
    output: "assets/characters/_lpc-validation/validation_full_sheet.png",
  },
  // Actual MVP characters — layer Name values may need tuning after
  // validation confirms the pipeline works. These are initial guesses
  // built from item-metadata.js categories; the validation run above
  // confirms the hash format is correct.
  // Warrior: male, amber skin, CHESTNUT theme (hair palette) with warm
  // bronze plate + leather pants for color harmony. Chestnut itself is a
  // hair palette key only, so gear uses metal-palette colors chosen to sit
  // in the same warm-brown family.
  warrior: {
    hash: {
      sex: "male",
      body: "Body_Color_amber",
      head: "Human_Male_amber",
      hair: "Natural_chestnut",          // hair palette = chestnut (warm brown)
      beard: "Basic_Beard_chestnut",     // match hair
      armour: "Plate_bronze",            // metal palette = bronze (warm metallic)
      legs: "Long_Pants_leather",        // file variant — "leather" reads as warm brown
      shoes_toe: "Thick_Plated_Toe_bronze",
      weapon: "Longsword_longsword",
    },
    output: "assets/characters/player/warrior/warrior_full_sheet.png",
  },
  // Ranger: female, light skin, GREEN theme. Switched from Robe (which only
  // offers muted forest_green) to Tunic_green (bright green file variant,
  // female-only) + leather pants for classic ranger silhouette + bow. The
  // tunic carries the loud green; pants stay brown so nothing muddies the
  // green read. Hair green to match theme (loud — like Mage's blue hair).
  ranger: {
    hash: {
      sex: "female",
      body: "Body_Color_light",
      head: "Human_Female_light",
      hair: "Natural_green",            // green hair to match green theme
      clothes: "Tunic_green",           // bright green tunic (file variant, female-only)
      legs: "Long_Pants_leather",       // warm brown pants for contrast
      shoes: "Revised_Shoes_brown",
      weapon: "Normal_iron",            // bow, name="Normal", variant=iron
    },
    output: "assets/characters/player/ranger/ranger_full_sheet.png",
  },
  // Mage: female (robe requires female), BLUE theme. Blue hair + blue robe
  // + blue pants + iron staff. Strong blue silhouette end-to-end.
  mage: {
    hash: {
      sex: "female",
      body: "Body_Color_light",
      head: "Human_Female_light",
      hair: "Natural_blue",             // hair palette = blue
      clothes: "Robe_blue",             // blue file variant (robe)
      legs: "Long_Pants_blue",          // blue file variant (pants2)
      shoes: "Revised_Shoes_bluegray",
      weapon: "Gnarled_staff_iron",
    },
    output: "assets/characters/player/mage/mage_full_sheet.png",
  },
  // Blacksmith: male, BLACK theme. Black cloak + black beard + black pants.
  blacksmith: {
    hash: {
      sex: "male",
      body: "Body_Color_amber",
      head: "Human_Male_amber",
      hair: "Natural_black",
      beard: "Basic_Beard_black",       // type=beard, name="Basic Beard", palette=hair
      jacket: "Iverness_cloak_black",   // file variant black
      legs: "Long_Pants_black",
      shoes: "Revised_Shoes_black",
      weapon: "Mace_mace",              // name="Mace", variant=mace (only one)
    },
    output: "assets/characters/npcs/blacksmith/blacksmith_full_sheet.png",
  },
  // Guild Maid: female, GOLD theme. Previous white-robe + yellow-pants read
  // as low-contrast (light-on-light). Now: loud yellow blouse torso +
  // contrasting white apron_half overlay (service uniform read) + yellow
  // pants. Gold hair (more saturated than blonde). High-chroma gold with
  // white apron as classic service silhouette.
  guild_maid: {
    hash: {
      sex: "female",
      body: "Body_Color_light",
      head: "Human_Female_light",
      hair: "Natural_gold",             // gold (palette) — more saturated than blonde
      clothes: "Blouse_yellow",         // yellow blouse (file variant, female-only)
      apron: "Apron_half_white",        // white half-apron overlay (female-only)
      legs: "Long_Pants_yellow",        // yellow pants (file variant)
      shoes: "Revised_Shoes_brown",
    },
    output: "assets/characters/npcs/guild_maid/guild_maid_full_sheet.png",
  },
  // Village Chief: MALE (Frock coat is male-only and carries white variant,
  // so we can finally get a male elder in white). Long white Winter Beard
  // as the key identity marker + white hair + white frock coat + white
  // pants. Walking staff (iron gnarled staff for now).
  village_chief: {
    hash: {
      sex: "male",
      body: "Body_Color_light",
      head: "Human_Male_light",
      hair: "Natural_white",
      beard: "Winter_Beard_white",      // long white beard — male elder read
      jacket: "Frock_coat_white",       // white frock coat (file variant, male-only)
      legs: "Long_Pants_white",
      shoes: "Revised_Shoes_brown",
      weapon: "Gnarled_staff_iron",
    },
    output: "assets/characters/npcs/village_chief/village_chief_full_sheet.png",
  },
};

function buildHashUrl(hash) {
  const params = Object.entries(hash)
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
    .join("&");
  return `${SERVER_URL}/#${params}`;
}

async function generate(recipeKey, recipe) {
  const url = buildHashUrl(recipe.hash);
  const outPath = resolve(REPO_ROOT, recipe.output);
  mkdirSync(dirname(outPath), { recursive: true });

  console.log(`\n[${recipeKey}]`);
  console.log(`  URL:    ${url}`);
  console.log(`  Output: ${recipe.output}`);

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ acceptDownloads: true });
  const page = await context.newPage();

  const logs = [];
  page.on("console", (msg) => logs.push(`  [console.${msg.type()}] ${msg.text()}`));
  page.on("pageerror", (err) => logs.push(`  [pageerror] ${err.message}`));

  try {
    await page.goto(url, { waitUntil: "networkidle", timeout: 60000 });
    // Wait for the Download buttons to mount (mithril SPA — rendered after hydration).
    await page.waitForSelector("#download-buttons button", { timeout: 30000 });
    // Give the layer compositor + WebGL palette recolor a moment to finish.
    await page.waitForTimeout(3000);

    // Click "Spritesheet (PNG)" — confirmed button text from Download.js:75.
    const downloadPromise = page.waitForEvent("download", { timeout: 30000 });
    await page.getByRole("button", { name: "Spritesheet (PNG)" }).click();
    const download = await downloadPromise;
    await download.saveAs(outPath);
    console.log(`  ✓ PNG saved`);

    // Click "Credits (TXT)" — captures the per-layer attribution list.
    const creditsPath = outPath.replace(/_full_sheet\.png$/, "_credits.txt");
    const creditsPromise = page.waitForEvent("download", { timeout: 30000 });
    await page.getByRole("button", { name: "Credits (TXT)" }).click();
    const creditsDownload = await creditsPromise;
    await creditsDownload.saveAs(creditsPath);
    console.log(`  ✓ Credits TXT saved`);

    await browser.close();
    return true;
  } catch (err) {
    console.log(`  FAIL: ${err.message}`);
    console.log(logs.slice(-10).join("\n"));
    await browser.close();
    return false;
  }
}

async function main() {
  const only = process.argv[2];
  const keys = only ? [only] : Object.keys(RECIPES);
  if (only && !RECIPES[only]) {
    console.error(`Unknown recipe: ${only}. Options: ${Object.keys(RECIPES).join(", ")}`);
    process.exit(1);
  }

  let successes = 0;
  let failures = 0;
  for (const key of keys) {
    const ok = await generate(key, RECIPES[key]);
    if (ok) successes++;
    else failures++;
  }
  console.log(`\nDone. ${successes} ok, ${failures} failed.`);
  process.exit(failures > 0 ? 1 : 0);
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
