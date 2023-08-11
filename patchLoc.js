// Fall back to English instead of the translation key
// Weblate outputs "" instead of no key
const fs = require("fs");
const path = require("path");

const loc = path.join(__dirname, "Data", "loc");
const files = fs.readdirSync(loc);

for (const file of files) {
  const filePath = path.join(loc, file);
  const data = fs.readFileSync(filePath, "utf-8");
  const json = JSON.parse(data);

  for (const [key, value] of Object.entries(json)) {
    if (value.message === "") {
      delete json[key];
    }
  }

  fs.writeFileSync(filePath, JSON.stringify(json, null, 2));
}
