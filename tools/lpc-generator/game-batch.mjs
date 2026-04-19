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
  // Warrior: male, amber skin, CHESTNUT theme. Loose messy mop hair for a
  // rugged martial silhouette (Mop — PO pick, visually distinct from
  // Village Chief's Natural long hair).
  warrior: {
    hash: {
      sex: "male",
      body: "Body_Color_amber",
      head: "Human_Male_amber",
      hair: "Mop_chestnut",              // mop — loose messy warrior cut (PO pick)
      beard: "Basic_Beard_chestnut",     // match hair
      armour: "Plate_bronze",            // metal palette = bronze (warm metallic)
      legs: "Long_Pants_leather",        // file variant — "leather" reads as warm brown
      shoes_toe: "Thick_Plated_Toe_bronze",
      weapon: "Longsword_longsword",
      shield: "Round_Shield_silver",     // round shield, silver (full Name is "Round Shield" per sheet def)
    },
    output: "assets/characters/player/warrior/warrior_full_sheet.png",
  },
  // Ranger: female, light skin, GREEN theme with HOOD. Tunic + hood (hat
  // category) for the archer scout silhouette. Hair visible inside the
  // hood — use a bob or shoulder-length style (Lob) so the shape fills
  // the hood naturally instead of clipping.
  ranger: {
    hash: {
      sex: "female",
      body: "Body_Color_light",
      head: "Human_Female_light",
      hair: "Lob_green",                // shoulder-length bob, green to match theme
      clothes: "Tunic_green",           // bright green tunic (file variant, female-only)
      hat: "Hood_green",                // hood accessory, green (PO direction)
      gloves: "Gloves_leather",         // leather gloves (PO direction)
      legs: "Long_Pants_leather",       // warm brown pants for contrast
      shoes: "Revised_Shoes_brown",
      weapon: "Normal_iron",            // bow, name="Normal", variant=iron
    },
    output: "assets/characters/player/ranger/ranger_full_sheet.png",
  },
  // Mage: male, BROWN-skin (Black character per PO direction 2026-04-19).
  // Robe is female-only in LPC, and Iverness only has a black variant —
  // switched to Tabard (male-supported, blue variant exists) for the
  // scholar-mage silhouette. Bedhead hair = neutral "distracted scholar"
  // style, intentionally not picking ethnically-coded styles (afro /
  // twists / dreadlocks / cornrows) just because the character is Black.
  mage: {
    hash: {
      sex: "male",
      body: "Body_Color_brown",
      head: "Human_Male_brown",
      hair: "Bedhead_black",             // neutral bookish/scholar style
      jacket: "Tabard_blue",             // blue tabard (male-supported)
      legs: "Long_Pants_blue",
      shoes: "Revised_Shoes_bluegray",
      weapon: "Gnarled_staff_iron",
    },
    output: "assets/characters/player/mage/mage_full_sheet.png",
  },
  // Blacksmith: MUSCULAR body + SHIRTLESS (PO direction 2026-04-19).
  // Bald (Balding hair style) + full beard for the classic shirtless
  // smith-at-the-forge silhouette. No jacket/torso layer — bare torso
  // shows the muscular body definition. Pants + shoes kept.
  blacksmith: {
    hash: {
      sex: "muscular",                  // muscular body type (not male)
      body: "Body_Color_amber",
      head: "Human_Male_amber",
      hair: "Balding_black",            // bald / heavy receded hairline
      beard: "Basic_Beard_black",       // type=beard, name="Basic Beard", palette=hair
      legs: "Pants_black",              // pants (not Long_Pants/pants2 — pants2 has no muscular variant; pants does)
      shoes: "Revised_Shoes_black",
      weapon: "Mace_mace",              // name="Mace", variant=mace (only one)
    },
    output: "assets/characters/npcs/blacksmith/blacksmith_full_sheet.png",
  },
  // Guild Maid: female, GOLD theme with PONYTAIL (PO direction — classic
  // service-uniform bun/ponytail silhouette replaces the original flat
  // Natural style). Loud yellow blouse + white apron overlay + yellow
  // pants. Gold hair in a ponytail reads as "on-duty professional."
  guild_maid: {
    hash: {
      sex: "female",
      body: "Body_Color_light",
      head: "Human_Female_light",
      hair: "Ponytail_gold",            // ponytail, gold palette
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
